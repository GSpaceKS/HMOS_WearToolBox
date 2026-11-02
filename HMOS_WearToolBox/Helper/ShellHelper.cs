using System;
using System.Diagnostics;

namespace HMOS_WearToolBox.Helper
{
    internal static class ShellHelper
    {
        // 成功返回 null，失败返回异常文本，提示文案由调用方决定。
        public static string? OpenUrl(string url)
        {
            return Start(url, null);
        }

        public static string? OpenFolder(string folder)
        {
            return Start("explorer.exe", "\"" + folder + "\"");
        }

        private static string? Start(string fileName, string? arguments)
        {
            try
            {
                ProcessStartInfo info = arguments == null
                    ? new ProcessStartInfo(fileName) { UseShellExecute = true }
                    : new ProcessStartInfo(fileName, arguments) { UseShellExecute = true };
                Process.Start(info);
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
