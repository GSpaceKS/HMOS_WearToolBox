using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;

namespace HMOS_WearToolBox.Helper
{
    /// <summary>
    /// 字体辅助类，支持从嵌入资源加载字体，并提供字体获取的统一接口。
    /// </summary>
    public static class FontHelper
    {
        // 私有字体集合，用于存储从资源加载的字体
        private static PrivateFontCollection _privateFonts = new PrivateFontCollection();

        /// <summary>
        /// 从嵌入资源加载字体文件，并将其添加到私有字体集合中。
        /// </summary>
        public static string LoadEmbeddedFont(string resourcePath)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream(resourcePath))
                {
                    if (stream == null) return null;
                    byte[] fontData = new byte[stream.Length];
                    stream.Read(fontData, 0, fontData.Length);

                    // 使用 unsafe 代码将字体数据添加到私有集合
                    unsafe
                    {
                        fixed (byte* ptr = fontData)
                        {
                            _privateFonts.AddMemoryFont((IntPtr)ptr, fontData.Length);
                        }
                    }
                }

                // 返回第一个加载的字体家族名称（通常一个文件对应一个家族）
                return _privateFonts.Families.Length > 0 ? _privateFonts.Families[_privateFonts.Families.Length - 1].Name : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 初始化所有预定义的嵌入字体（在程序启动时调用）。
        /// </summary>
        public static void InitializeEmbeddedFonts()
        {
            // 列出所有需要嵌入的字体资源路径
            string[] embeddedResources = new[]
            {
                "HMOS_WearToolBox.Resources.Fonts.CascadiaCode.CascadiaCode.ttf",
                "HMOS_WearToolBox.Resources.Fonts.CascadiaMono.CascadiaMono.ttf",
                "HMOS_WearToolBox.Resources.Fonts.Consolas.consola.ttf",
                "HMOS_WearToolBox.Resources.Fonts.Consolas.consolab.ttf",
                "HMOS_WearToolBox.Resources.Fonts.Consolas.consolai.ttf",
                "HMOS_WearToolBox.Resources.Fonts.Consolas.consolaz.ttf",
                "HMOS_WearToolBox.Resources.Fonts.HarmonyOS_Sans_SC.HarmonyOS_Sans_SC_Black.ttf",
                "HMOS_WearToolBox.Resources.Fonts.HarmonyOS_Sans_SC.HarmonyOS_Sans_SC_Bold.ttf",
                "HMOS_WearToolBox.Resources.Fonts.HarmonyOS_Sans_SC.HarmonyOS_Sans_SC_Medium.ttf",
                "HMOS_WearToolBox.Resources.Fonts.Microsoft_YaHei_UI.msyh.ttc",
                "HMOS_WearToolBox.Resources.Fonts.Microsoft_YaHei_UI.msyhbd.ttc",
                "HMOS_WearToolBox.Resources.Fonts.Microsoft_YaHei_UI.msyhl.ttc"
            };

            foreach (var res in embeddedResources)
            {
                LoadEmbeddedFont(res);
            }
        }

        /// <summary>
        /// 获取所有已加载的嵌入字体名称列表。
        /// </summary>
        public static List<string> GetEmbeddedFontNames()
        {
            var names = new List<string>();
            foreach (FontFamily family in _privateFonts.Families)
            {
                names.Add(family.Name);
            }
            return names;
        }

        /// <summary>
        /// 根据字体名称获取字体实例（优先系统字体，其次嵌入字体，最后回退到微软雅黑UI）。
        /// </summary>
        public static Font GetFont(string fontName, float fontSize)
        {
            // 1. 尝试从系统字体中获取
            try
            {
                Font systemFont = new Font(fontName, fontSize);
                // 如果系统字体实际名称与请求一致，则有效
                if (systemFont.Name == fontName)
                    return systemFont;
                systemFont.Dispose();
            }
            catch { }

            // 2. 尝试从嵌入字体中查找
            foreach (FontFamily family in _privateFonts.Families)
            {
                if (family.Name == fontName)
                    return new Font(family, fontSize);
            }

            // 3. 回退到微软雅黑UI（若系统中不存在则使用系统默认字体）
            try
            {
                return new Font("微软雅黑UI", fontSize);
            }
            catch
            {
                // 最后的保底方案
                return new Font("Tahoma", fontSize);
            }
        }

        /// <summary>
        /// 获取 HarmonyOS 字体（用于软件界面全局字体）。
        /// </summary>
        public static Font GetHarmonyOSFont(float fontSize)
        {
            // 先尝试从嵌入字体集合中查找 "HarmonyOS Sans SC Medium"
            foreach (FontFamily family in _privateFonts.Families)
            {
                if (family.Name.Contains("HarmonyOS") && family.Name.Contains("Medium"))
                    return new Font(family, fontSize);
            }
            // 再尝试其他 HarmonyOS 变体
            foreach (FontFamily family in _privateFonts.Families)
            {
                if (family.Name.Contains("HarmonyOS"))
                    return new Font(family, fontSize);
            }
            // 回退到系统字体
            try
            {
                return new Font("微软雅黑UI", fontSize);
            }
            catch
            {
                return new Font("Tahoma", fontSize);
            }
        }
    }
}