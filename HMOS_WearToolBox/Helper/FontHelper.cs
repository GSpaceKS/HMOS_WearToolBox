using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace HMOS_WearToolBox.Helper
{
    /// <summary>
    /// 字体辅助类，支持从嵌入资源加载字体，并提供字体获取的统一接口。
    /// </summary>
    public static class FontHelper
    {
        // 私有字体集合，用于存储从资源加载的字体
        private static readonly PrivateFontCollection _privateFonts = new PrivateFontCollection();

        /// <summary>系统 UI 字体的候选列表（按优先级）。</summary>
        private static readonly string[] SystemUiFontCandidates =
        {
            "Microsoft YaHei UI",
            "Microsoft YaHei",
            "微软雅黑",
            "Segoe UI",
            "Tahoma"
        };

        /// <summary>从嵌入资源加载字体，失败时返回 null。</summary>
        public static string? LoadEmbeddedFont(string resourcePath)
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

        public static void InitializeEmbeddedFonts()
        {
            // 只嵌入可以随程序再分发的字体：
            //   Cascadia Code / Mono —— SIL OFL 1.1
            //   HarmonyOS Sans SC   —— 华为授权
            // 微软雅黑（msyh*.ttc）和 Consolas 是微软专有字体，只授权在 Windows 上使用，
            // 不允许随应用再分发，因此没有打进仓库。它们在 Windows 上都是系统自带字体，
            // GetFont 会直接从系统字体里取到，观感不受影响。
            string[] embeddedResources = new[]
            {
                "HMOS_WearToolBox.Resources.Fonts.CascadiaCode.CascadiaCode.ttf",
                "HMOS_WearToolBox.Resources.Fonts.CascadiaMono.CascadiaMono.ttf",
                "HMOS_WearToolBox.Resources.Fonts.HarmonyOS_Sans_SC.HarmonyOS_Sans_SC_Black.ttf",
                "HMOS_WearToolBox.Resources.Fonts.HarmonyOS_Sans_SC.HarmonyOS_Sans_SC_Bold.ttf",
                "HMOS_WearToolBox.Resources.Fonts.HarmonyOS_Sans_SC.HarmonyOS_Sans_SC_Medium.ttf"
            };

            foreach (var res in embeddedResources)
            {
                LoadEmbeddedFont(res);
            }
        }

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
        /// 根据字体名称获取字体实例（优先系统字体，其次嵌入字体，最后回退到系统 UI 字体）。
        /// </summary>
        public static Font GetFont(string fontName, float fontSize)
        {
            // 1. 尝试从系统字体中获取
            Font? systemFont = TryCreateSystemFont(fontName, fontSize);
            if (systemFont != null)
                return systemFont;

            // 2. 尝试从嵌入字体中查找
            Font? embeddedFont = TryCreateEmbeddedFont(fontName, fontSize);
            if (embeddedFont != null)
                return embeddedFont;

            // 3. 回退到系统 UI 字体
            return CreateFirstAvailableFont(fontSize);
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
            // 回退到系统 UI 字体
            return CreateFirstAvailableFont(fontSize);
        }

        private static Font? TryCreateSystemFont(string fontName, float fontSize)
        {
            if (string.IsNullOrWhiteSpace(fontName))
                return null;

            // 先确认字体真的存在。不能用 new Font(name, size) 来判断：
            // 名字不存在时它既不报错也不抛异常，而是静默给你一个 Microsoft Sans Serif。
            try
            {
                using FontFamily probe = new FontFamily(fontName);
            }
            catch
            {
                return null;
            }

            // 也不能拿 font.Name 和传入的名字比字符串：GDI+ 会把 "Microsoft YaHei"
            // 解析成它的本地化名 "微软雅黑"，名字对不上但字体其实是对的。
            try
            {
                return new Font(fontName, fontSize);
            }
            catch
            {
                return null;
            }
        }

        private static Font? TryCreateEmbeddedFont(string fontName, float fontSize)
        {
            if (string.IsNullOrWhiteSpace(fontName))
                return null;

            foreach (FontFamily family in _privateFonts.Families)
            {
                if (string.Equals(family.Name, fontName, StringComparison.OrdinalIgnoreCase))
                    return new Font(family, fontSize);
            }

            return null;
        }

        /// <summary>
        /// 依次尝试候选列表，返回第一个系统里真实存在的字体。
        /// </summary>
        private static Font CreateFirstAvailableFont(float fontSize)
        {
            foreach (string name in SystemUiFontCandidates)
            {
                Font? font = TryCreateSystemFont(name, fontSize);
                if (font != null)
                    return font;
            }

            // 理论上到不了这里；真到了就用系统消息框字体，再不行用 GDI+ 的通用无衬线字体。
            Font? uiFont = SystemFonts.MessageBoxFont ?? SystemFonts.DefaultFont;
            if (uiFont != null)
                return (Font)uiFont.Clone();

            return new Font(FontFamily.GenericSansSerif, fontSize);
        }
    }
}
