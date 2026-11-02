using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HMOS_WearToolBox.Helper
{
    /// <summary>HDC 调用入口：释放内嵌的 hdc.exe，并按超时执行命令。</summary>
    public static class HdcHelper
    {
        public const int DefaultTimeoutMs = 60_000;

        public const int QuickTimeoutMs = 5_000;

        /// <summary>耗时命令（如 file send / install）的超时时间（毫秒）。</summary>
        public const int LongRunningTimeoutMs = 600_000;

        private const string HdcResourceName = "HMOS_WearToolBox.Resources.HDC.hdc.exe";
        private const string LibUsbResourceName = "HMOS_WearToolBox.Resources.HDC.libusb_shared.dll";

        private static readonly object InitLock = new object();
        private static string? hdcPath;
        private static string? libPath;

        /// <summary>
        /// 获取 hdc.exe 的路径（首次调用时从嵌入资源释放到临时目录）。
        /// </summary>
        public static string GetHdcPath()
        {
            if (hdcPath != null && File.Exists(hdcPath))
                return hdcPath;

            // 多个页面（首页定时器 / 终端定时器 / 软件管理）可能同时首次调用，这里必须串行化
            lock (InitLock)
            {
                if (hdcPath != null && File.Exists(hdcPath))
                    return hdcPath;

                string toolDir = Path.Combine(Path.GetTempPath(), "HMOS_WearToolBox");
                Directory.CreateDirectory(toolDir);

                string exePath = Path.Combine(toolDir, "hdc.exe");
                string dllPath = Path.Combine(toolDir, "libusb_shared.dll");

                ExtractResourceIfNeeded(HdcResourceName, exePath);
                ExtractResourceIfNeeded(LibUsbResourceName, dllPath);

                libPath = dllPath;
                hdcPath = exePath;
                return hdcPath;
            }
        }

        /// <summary>
        /// 从嵌入资源释放文件；若目标文件已存在且体积一致，则跳过。
        /// </summary>
        private static void ExtractResourceIfNeeded(string resourceName, string outputPath)
        {
            using Stream? resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (resource == null)
            {
                throw new FileNotFoundException(
                    $"未能找到嵌入资源 {resourceName}。" +
                    "请确认 Resources\\HDC\\ 目录下有 hdc.exe 与 libusb_shared.dll，" +
                    "并检查 .gitignore 是否为这两个文件保留了 ! 例外规则。");
            }

            // 只判断 File.Exists 是不够的：释放过程中断会在临时目录留下体积不完整的文件，
            // 之后每次启动都会认为“已经释放好了”，而 hdc 实际无法运行。
            FileInfo existing = new FileInfo(outputPath);
            if (existing.Exists && existing.Length == resource.Length)
                return;

            // 先写临时文件再替换，避免中途失败留下半截的 exe
            string tempPath = outputPath + ".tmp";
            try
            {
                using (FileStream fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    resource.CopyTo(fs);
                }
                File.Move(tempPath, outputPath, overwrite: true);
            }
            catch (Exception ex) when (File.Exists(outputPath))
            {
                // 上一次运行残留的 hdc 服务可能正占用这个 exe，导致替换失败。
                // 这种情况下退回使用现有文件，总好过让整个 HDC 功能不可用。
                Debug.WriteLine($"替换 {outputPath} 失败，继续使用现有文件: {ex.Message}");
            }
            finally
            {
                TryDelete(tempPath);
            }
        }

        public static string RunHdcCommand(string arguments) => RunHdcCommand(arguments, DefaultTimeoutMs);

        /// <summary>
        /// 运行 hdc 命令并返回输出。
        /// </summary>
        /// <param name="arguments">传给 hdc 的参数，如 "list targets"。</param>
        /// <param name="timeoutMs">超时毫秒数，超时后进程会被强制结束并抛出 <see cref="TimeoutException"/>。</param>
        public static string RunHdcCommand(string arguments, int timeoutMs)
        {
            string hdc = GetHdcPath();

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = hdc,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                // 设备端输出是 UTF-8（bm dump 里的中文机构名、应用名都是），不显式指定的话
                // .NET 会按系统 ANSI 代码页（中文系统是 GBK）解码：中文变乱码，而且 GBK 双字节
                // 会吞掉紧跟的 0x22 引号，把 JSON 结构吃坏，导致部分应用的 dump 解析失败。
                StandardOutputEncoding = new UTF8Encoding(false),
                StandardErrorEncoding = new UTF8Encoding(false),
                CreateNoWindow = true
            };

            using Process process = new Process { StartInfo = startInfo };
            process.Start();

            // 必须同时把两个管道读走：只读 stdout 时，若 stderr 写满缓冲区，hdc 会阻塞在写操作上，
            // 于是 ReadToEnd() 和 WaitForExit() 双双永久等待。
            Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
            Task<string> stderrTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(timeoutMs))
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                try { process.WaitForExit(QuickTimeoutMs); } catch { }
                throw new TimeoutException($"hdc 命令超过 {timeoutMs} ms 未返回：hdc {arguments}");
            }

            string stdout = ReadSafely(stdoutTask);
            string stderr = ReadSafely(stderrTask);

            // 保持原有行为：正常情况下只返回标准输出。
            // 只有标准输出为空时才回退到标准错误，免得把报错信息当成“命令没有输出”丢掉。
            if (string.IsNullOrWhiteSpace(stdout) && !string.IsNullOrWhiteSpace(stderr))
                return stderr;

            return stdout;
        }

        public static List<string> ListTargets(int timeoutMs = DefaultTimeoutMs)
        {
            string output = RunHdcCommand("list targets", timeoutMs);
            List<string> targets = new List<string>();

            foreach (string rawLine in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string line = rawLine.Trim();
                // "[Empty]" 表示当前没有设备；其他以 [ 开头的是 hdc 的状态提示，都不是设备地址。
                // （IPv6 地址会以 [ 开头，但手表场景用的是 IPv4，这里按 IPv4 处理。）
                if (line.Length == 0 || line.StartsWith("[", StringComparison.Ordinal))
                    continue;

                targets.Add(line);
            }

            return targets;
        }

        /// <summary>判断设备是否在线（只比主机部分，避免 192.168.1.1 误配 192.168.1.10）。</summary>
        public static bool IsTargetOnline(IEnumerable<string> targets, string? deviceAddress)
        {
            if (string.IsNullOrWhiteSpace(deviceAddress))
                return false;

            string wantHost = GetHostPart(deviceAddress);

            foreach (string target in targets)
            {
                if (string.Equals(target, deviceAddress, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (string.Equals(GetHostPart(target), wantHost, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static string GetHostPart(string address)
        {
            int index = address.LastIndexOf(':');
            return index > 0 ? address.Substring(0, index) : address.Trim();
        }

        /// <summary>
        /// 删除临时目录中的 hdc.exe / libusb_shared.dll（含上次异常退出残留的文件）。
        /// </summary>
        public static void Cleanup()
        {
            lock (InitLock)
            {
                string toolDir = Path.Combine(Path.GetTempPath(), "HMOS_WearToolBox");

                TryDelete(hdcPath ?? Path.Combine(toolDir, "hdc.exe"));
                TryDelete(libPath ?? Path.Combine(toolDir, "libusb_shared.dll"));
                TryDelete(Path.Combine(toolDir, "hdc.exe.tmp"));
                TryDelete(Path.Combine(toolDir, "libusb_shared.dll.tmp"));

                hdcPath = null;
                libPath = null;
            }
        }

        private static string ReadSafely(Task<string> task)
        {
            try { return task.GetAwaiter().GetResult(); }
            catch { return string.Empty; }
        }

        private static void TryDelete(string? path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return;

            try { File.Delete(path); } catch { }
        }
    }
}
