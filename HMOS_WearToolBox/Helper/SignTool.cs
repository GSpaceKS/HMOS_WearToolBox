using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace HMOS_WearToolBox.Helper
{
    public sealed class SignToolResult
    {
        public bool Success { get; set; }
        public int ExitCode { get; set; }
        public string Output { get; set; } = string.Empty;
    }

    /// <summary>定位并调用 hap-sign-tool.jar（内嵌在程序集里，首次使用时解压到 LocalAppData）。</summary>
    public static class SignTool
    {
        private const string ToolResourceName = "HMOS_WearToolBox.Resources.Sign.sign-tool.zip";

        private static readonly object InitLock = new object();

        private static string? bundledRoot;

        private static string? javaPath;
        private static string? jarPath;
        private static string? rootPath;
        private static string lastError = string.Empty;

        /// <summary>
        /// 探测签名工具是否可用；失败时 message 说明缺什么。
        /// </summary>
        public static bool TryResolve(out string message)
        {
            if (javaPath != null && jarPath != null && File.Exists(javaPath) && File.Exists(jarPath))
            {
                message = string.Empty;
                return true;
            }

            lock (InitLock)
            {
                if (javaPath != null && jarPath != null && File.Exists(javaPath) && File.Exists(jarPath))
                {
                    message = string.Empty;
                    return true;
                }

                List<string> roots = new List<string>();
                string env = Environment.GetEnvironmentVariable("HMOS_SIGN_DIR") ?? string.Empty;
                if (env.Length > 0)
                    roots.Add(env);

                roots.Add(Path.Combine(AppContext.BaseDirectory, "Sign"));
                roots.Add(Path.Combine(AppContext.BaseDirectory, "Resources", "Sign"));

                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                roots.Add(Path.Combine(appData, "HMOS_WearToolBox", "sign"));

                // 随程序内嵌的签名工具链（首次使用时解压到 LocalAppData）
                string bundled = EnsureBundled(out string bundleError);
                if (bundled.Length > 0)
                    roots.Add(bundled);
                else if (bundleError.Length > 0)
                    lastError = bundleError;

                // 开发期的兜底目录：源码仓库里的工具链
                roots.Add(@"D:\GitHub Repositories\HMOSLC\!HMOS_WearToolBox\Sign");

                string[] javaCandidates = { "jre\\bin\\java.exe", "mini_jre\\bin\\java.exe", "java\\bin\\java.exe", "bin\\java.exe" };

                foreach (string root in roots)
                {
                    if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                        continue;

                    string jar = Path.Combine(root, "hap-sign-tool.jar");
                    if (!File.Exists(jar))
                        continue;

                    foreach (string relative in javaCandidates)
                    {
                        string java = Path.Combine(root, relative);
                        if (!File.Exists(java))
                            continue;

                        javaPath = java;
                        jarPath = jar;
                        rootPath = root;
                        message = string.Empty;
                        return true;
                    }
                }

                string why = lastError.Length > 0
                    ? lastError
                    : "没有找到签名工具链（hap-sign-tool.jar 与裁剪后的 java）。";
                why += " 也可以设环境变量 HMOS_SIGN_DIR 指向存放 hap-sign-tool.jar 与 jre\\bin\\java.exe 的目录。";
                lastError = why;
                message = why;
                return false;
            }
        }

        /// <summary>
        /// 执行一次签名工具命令。参数按列表传入，不经过字符串拼接，避免路径带空格被拆开。
        /// </summary>
        public static SignToolResult Run(string label, IReadOnlyList<string> args, string workDir, int timeoutMs = 600_000)
        {
            if (!TryResolve(out string message))
                throw new InvalidOperationException(message);

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = javaPath!,
                WorkingDirectory = workDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            // 裁剪版 JRE 把 java.beans 从 java.base 里摘了出去（见 beans.jar），
            // 必须靠 --patch-module 补回去，否则 log4j2 初始化会 NoClassDefFoundError。
            string beans = rootPath == null ? string.Empty : Path.Combine(rootPath, "beans.jar");
            if (beans.Length > 0 && File.Exists(beans))
            {
                startInfo.ArgumentList.Add("--patch-module");
                startInfo.ArgumentList.Add("java.base=" + beans);
                startInfo.ArgumentList.Add("--add-exports");
                startInfo.ArgumentList.Add("java.base/java.beans=ALL-UNNAMED");
            }

            startInfo.ArgumentList.Add("-jar");
            startInfo.ArgumentList.Add(jarPath!);
            foreach (string arg in args)
                startInfo.ArgumentList.Add(arg);

            using Process process = new Process { StartInfo = startInfo };
            process.Start();

            // 两个管道必须同时读：只读 stdout 时 stderr 写满缓冲区会让工具永久阻塞。
            // 这里读原始字节而不是文本：JVM 往管道写日志用的编码跟它的版本/控制台代码页有关
            // （17 是系统 ANSI，18 以后默认 UTF-8），按固定编码解会把证书里的中文名字解成乱码。
            Task<byte[]> stdoutTask = ReadAllBytesAsync(process.StandardOutput.BaseStream);
            Task<byte[]> stderrTask = ReadAllBytesAsync(process.StandardError.BaseStream);

            if (!process.WaitForExit(timeoutMs))
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                try { process.WaitForExit(3_000); } catch { }
                throw new TimeoutException("签名工具超过 " + timeoutMs + " ms 未返回：" + label);
            }

            string stdout = Decode(ReadSafely(stdoutTask));
            string stderr = Decode(ReadSafely(stderrTask));
            string output = string.IsNullOrWhiteSpace(stderr) ? stdout : stdout + Environment.NewLine + stderr;

            return new SignToolResult
            {
                Success = process.ExitCode == 0,
                ExitCode = process.ExitCode,
                Output = output.Trim()
            };
        }

        /// <summary>把内嵌的签名工具链解压到 LocalAppData；已经解压过就直接复用。</summary>
        public static string EnsureBundled(out string error)
        {
            error = string.Empty;
            if (!string.IsNullOrEmpty(bundledRoot))
                return bundledRoot!;

            Assembly assembly = typeof(SignTool).Assembly;
            using Stream? stream = assembly.GetManifestResourceStream(ToolResourceName);
            if (stream == null)
            {
                error = string.Empty;   // 没内嵌就退回目录探测，不算错误
                bundledRoot = string.Empty;
                return string.Empty;
            }

            try
            {
                string baseFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "HMOS_WearToolBox", "sign-tool");
                string folder = Path.Combine(baseFolder, stream.Length.ToString());
                string marker = Path.Combine(folder, ".ready");
                string jar = Path.Combine(folder, "hap-sign-tool.jar");

                if (!File.Exists(marker) || !File.Exists(jar))
                {
                    Directory.CreateDirectory(folder);
                    Unzip(stream, folder);
                    File.WriteAllText(marker, DateTime.Now.ToString("o"));
                }

                bundledRoot = folder;
                return folder;
            }
            catch (Exception ex)
            {
                error = "解压内嵌签名工具失败：" + ex.Message;
                bundledRoot = string.Empty;
                return string.Empty;
            }
        }

        private static void Unzip(Stream stream, string folder)
        {
            string root = Path.GetFullPath(folder);
            using ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read);
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                    continue;   // 目录条目，靠文件创建时自动建

                string target = Path.GetFullPath(Path.Combine(root, entry.FullName));
                if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    continue;   // 防御 zip slip

                string? parent = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(parent))
                    Directory.CreateDirectory(parent);

                entry.ExtractToFile(target, overwrite: true);
            }
        }

        private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
        {
            using MemoryStream buffer = new MemoryStream();
            await stream.CopyToAsync(buffer).ConfigureAwait(false);
            return buffer.ToArray();
        }

        private static byte[] ReadSafely(Task<byte[]> task)
        {
            try { return task.Wait(2_000) ? task.Result : Array.Empty<byte>(); }
            catch { return Array.Empty<byte>(); }
        }

        private static Encoding? ansi;

        // .NET Core 默认只带 UTF-8/UTF-16，GBK(936) 要显式注册代码页提供程序才认得。
        private static Encoding Ansi()
        {
            if (ansi != null)
                return ansi;

            try { Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); } catch { }
            try { ansi = Encoding.GetEncoding(936); }
            catch { ansi = Encoding.UTF8; }
            return ansi;
        }

        /// <summary>按 UTF-8 解码 hdc 日志，失败再退回 GBK。</summary>
        private static string Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return string.Empty;

            try
            {
                return new UTF8Encoding(false, true).GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                try { return Ansi().GetString(bytes); }
                catch { return Encoding.UTF8.GetString(bytes); }
            }
        }
    }
}
