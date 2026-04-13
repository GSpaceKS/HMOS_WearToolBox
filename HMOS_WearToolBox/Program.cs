using System;
using System.Windows.Forms;
using HMOS_WearToolBox.Helper;

namespace HMOS_WearToolBox
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // 加载所有嵌入字体
            FontHelper.InitializeEmbeddedFonts();

            // 从设置中读取用户选择的全局字体和大小（如果尚未保存，则使用默认值）
            string globalFontName = Properties.Settings.Default.GlobalFontName;
            float globalFontSize = Properties.Settings.Default.GlobalFontSize;

            // 获取全局字体（若用户选择的不存在，则回退到 HarmonyOS 或系统默认）
            Font globalFont;
            if (globalFontName == "HarmonyOS Sans SC Medium" || string.IsNullOrEmpty(globalFontName))
            {
                globalFont = FontHelper.GetHarmonyOSFont(globalFontSize);
            }
            else
            {
                globalFont = FontHelper.GetFont(globalFontName, globalFontSize);
            }

            // 设置应用程序全局默认字体
            Application.SetDefaultFont(globalFont);

            Application.Run(new MainForm());
        }
    }
}