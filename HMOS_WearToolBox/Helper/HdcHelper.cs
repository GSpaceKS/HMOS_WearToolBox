using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace HMOS_WearToolBox.Helper
{
    public static class HdcHelper
    {
        private static string hdcPath = null;
        private static string libPath = null;

        /// <summary>
        /// 获取 hdc.exe 的路径（从嵌入资源释放到临时目录）
        /// </summary>
        public static string GetHdcPath()
        {
            if (hdcPath != null && File.Exists(hdcPath))
                return hdcPath;

            // 使用一个专用子目录，避免与其他工具冲突
            string tempDir = Path.GetTempPath();
            string toolDir = Path.Combine(tempDir, "HMOS_WearToolBox");
            if (!Directory.Exists(toolDir))
                Directory.CreateDirectory(toolDir);

            string exePath = Path.Combine(toolDir, "hdc.exe");
            string dllPath = Path.Combine(toolDir, "libusb_shared.dll");

            // 释放 hdc.exe
            if (!File.Exists(exePath))
                ExtractResource("HMOS_WearToolBox.Resources.hdc.exe", exePath);

            // 释放 libusb_shared.dll
            if (!File.Exists(dllPath))
                ExtractResource("HMOS_WearToolBox.Resources.libusb_shared.dll", dllPath);

            hdcPath = exePath;
            libPath = dllPath;
            return hdcPath;
        }

        /// <summary>
        /// 从嵌入资源释放文件
        /// </summary>
        private static void ExtractResource(string resourceName, string outputPath)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new Exception($"无法找到嵌入的资源：{resourceName}");
                using (FileStream fs = new FileStream(outputPath, FileMode.Create))
                {
                    stream.CopyTo(fs);
                }
            }
        }

        /// <summary>
        /// 运行 hdc 命令并返回标准输出
        /// </summary>
        public static string RunHdcCommand(string arguments)
        {
            string hdc = GetHdcPath();
            Process process = new Process();
            process.StartInfo.FileName = hdc;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }

        /// <summary>
        /// 清理临时目录中的 hdc.exe 和 libusb_shared.dll
        /// </summary>
        public static void Cleanup()
        {
            if (hdcPath != null && File.Exists(hdcPath))
            {
                try { File.Delete(hdcPath); } catch { }
            }
            if (libPath != null && File.Exists(libPath))
            {
                try { File.Delete(libPath); } catch { }
            }
        }
    }
}