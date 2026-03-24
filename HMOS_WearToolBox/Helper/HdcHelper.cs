using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms; // 如果使用 Application.UserAppDataPath 需要

namespace HMOS_WearToolBox.Helper
{
    public static class HdcHelper
    {
        private static string hdcPath = null;

        /// <summary>
        /// 获取 hdc.exe 的路径（从嵌入资源释放到临时目录）
        /// </summary>
        public static string GetHdcPath()
        {
            if (hdcPath != null && File.Exists(hdcPath))
                return hdcPath;

            string tempDir = Path.GetTempPath();
            string exePath = Path.Combine(tempDir, "hdc.exe");
            if (!File.Exists(exePath))
            {
                // 从嵌入资源释放
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("HMOS_WearToolBox.Resources.hdc.exe"))
                {
                    if (stream == null)
                        throw new Exception("无法找到嵌入的资源：hdc.exe");
                    using (FileStream fs = new FileStream(exePath, FileMode.Create))
                    {
                        stream.CopyTo(fs);
                    }
                }
            }
            hdcPath = exePath;
            return hdcPath;
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
        /// 清理临时目录中的 hdc.exe
        /// </summary>
        public static void Cleanup()
        {
            if (hdcPath != null && File.Exists(hdcPath))
            {
                try
                {
                    File.Delete(hdcPath);
                }
                catch { /* 忽略删除失败 */ }
            }
        }


    }
}