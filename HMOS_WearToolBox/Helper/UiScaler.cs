using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace HMOS_WearToolBox.Helper
{
    /// <summary>
    /// 等比缩放：以控件第一次参与缩放时的几何/字体为基准，整体乘以 factor。
    ///
    /// 为什么不用 Control.Scale() —— 它是累乘的，窗口来回拖动会因为四舍五入越缩越歪，
    /// 而且同一个控件缩两次就回不到原尺寸。这里每次都从快照重算，缩多少次都不会漂。
    /// </summary>
    internal static class UiScaler
    {
        private sealed class Snapshot
        {
            public Rectangle Bounds;
            public Font Font = SystemFonts.DefaultFont;
            public Padding Padding;
            public Padding Margin;
            public Size MinimumSize;
            public bool IsTableLayout;
            public (SizeType Type, float Value)[] ColumnStyles = Array.Empty<(SizeType, float)>();
            public (SizeType Type, float Value)[] RowStyles = Array.Empty<(SizeType, float)>();
            public bool IsSplitContainer;
            public int SplitterDistance;
        }

        private static readonly ConditionalWeakTable<Control, Snapshot> Cache =
            new ConditionalWeakTable<Control, Snapshot>();

        private static readonly Dictionary<string, Font> FontCache = new Dictionary<string, Font>();

        private static readonly Dictionary<Control, float> Factors = new Dictionary<Control, float>();

        /// <summary>某个控件当前所在页面的缩放系数（没缩放过就是 1）。供需要按比例算像素宽度的地方用。</summary>
        public static float FactorOf(Control control)
        {
            for (Control? c = control; c != null; c = c.Parent)
            {
                if (Factors.TryGetValue(c, out float factor)) return factor;
            }
            return 1f;
        }

        /// <summary>把页面的设计尺寸记为基准（不在缩放前调用也行，Apply 会自己补）。</summary>
        public static void Capture(Control root)
        {
            foreach (Control c in Flatten(root)) Get(c);
        }

        /// <summary>按 factor 重排整棵控件树。factor=1 就是还原设计尺寸。</summary>
        public static void Apply(Control root, float factor)
        {
            if (factor <= 0f) return;
            Factors[root] = factor;
            List<Control> all = Flatten(root);

            // 1) 几何：父先子后，子控件的坐标是相对父控件的，所以父的尺寸先定
            foreach (Control c in all)
            {
                Snapshot s = Get(c);
                c.Bounds = new Rectangle(
                    (int)Math.Round(s.Bounds.X * factor),
                    (int)Math.Round(s.Bounds.Y * factor),
                    Math.Max(1, (int)Math.Round(s.Bounds.Width * factor)),
                    Math.Max(1, (int)Math.Round(s.Bounds.Height * factor)));
                // 按钮宽度在设计时大多是「正好卡住字数」给的，字号缩到 0.7 倍后宽度也跟着缩，
                // 就会出现「重新连」「添加设」这种被硬截断的字。这里给它们一个不小于文字所需宽度的下限；
                // Dock 过的按钮（比如整行宽的保存按钮）宽度由布局决定，不去动它。
                if (c is ButtonBase b && !b.AutoSize && b.Dock == DockStyle.None && !string.IsNullOrEmpty(b.Text))
                {
                    Font bf = ScaledFont(s.Font, factor);
                    Size need = TextRenderer.MeasureText(b.Text, bf);
                    int minWidth = need.Width + 10;
                    if (c.Width < minWidth) c.Width = minWidth;
                    if (c.Height < need.Height + 2) c.Height = need.Height + 2;
                }

                c.Padding = ScalePadding(s.Padding, factor);
                c.Margin = ScalePadding(s.Margin, factor);
                c.MinimumSize = new Size(
                    (int)Math.Round(s.MinimumSize.Width * factor),
                    (int)Math.Round(s.MinimumSize.Height * factor));

                if (s.IsTableLayout && c is TableLayoutPanel tlp)
                {
                    // 表格的列宽行高如果是绝对值（像素），不会跟着表格大小变，必须单独缩
                    for (int i = 0; i < tlp.ColumnStyles.Count && i < s.ColumnStyles.Length; i++)
                    {
                        (SizeType type, float value) = s.ColumnStyles[i];
                        if (type == SizeType.Absolute)
                        {
                            tlp.ColumnStyles[i].Width = Math.Max(1f, value * factor);
                        }
                    }
                    for (int i = 0; i < tlp.RowStyles.Count && i < s.RowStyles.Length; i++)
                    {
                        (SizeType type, float value) = s.RowStyles[i];
                        if (type == SizeType.Absolute)
                        {
                            tlp.RowStyles[i].Height = Math.Max(1f, value * factor);
                        }
                    }
                }

                if (s.IsSplitContainer && c is SplitContainer sc && s.SplitterDistance > 0)
                {
                    try
                    {
                        sc.SplitterDistance = Math.Max(1, (int)Math.Round(s.SplitterDistance * factor));
                    }
                    catch (InvalidOperationException)
                    {
                        // 面板太小时 WinForms 会拒绝，忽略即可
                    }
                }
            }

            // 2) 字体：父先子后。给父控件设置 Font 会把字体“继承”给子控件，
            //    所以必须父的先设、子的后设，子控件自己那份字号才不会被父控件冲掉。
            foreach (Control c in all)
            {
                Snapshot s = Get(c);
                c.Font = ScaledFont(s.Font, factor);
            }
        }

        private static Font ScaledFont(Font original, float factor)
        {
            float size = original.Size * factor;
            // 0.5pt 一档，避免拖动窗口时生成成百上千个 Font 对象
            size = (float)Math.Round(size * 2f) / 2f;
            if (size < 4f) size = 4f;
            string key = original.FontFamily.Name + "|" + (int)original.Style + "|" + size.ToString("0.0");
            if (FontCache.TryGetValue(key, out Font? cached) && cached != null) return cached;

            Font created;
            try
            {
                created = new Font(original.FontFamily, size, original.Style, GraphicsUnit.Point);
            }
            catch (ArgumentException)
            {
                created = new Font(original.FontFamily.Name, size, original.Style, GraphicsUnit.Point);
            }
            FontCache[key] = created;
            return created;
        }

        private static Padding ScalePadding(Padding p, float factor)
        {
            return new Padding(
                (int)Math.Round(p.Left * factor),
                (int)Math.Round(p.Top * factor),
                (int)Math.Round(p.Right * factor),
                (int)Math.Round(p.Bottom * factor));
        }

        private static List<Control> Flatten(Control root)
        {
            var list = new List<Control>();
            Walk(root, list);
            return list;
        }

        private static void Walk(Control parent, List<Control> list)
        {
            foreach (Control child in parent.Controls)
            {
                list.Add(child);
                Walk(child, list);
            }
        }

        private static Snapshot Get(Control c)
        {
            return Cache.GetValue(c, key =>
            {
                var s = new Snapshot
                {
                    Bounds = key.Bounds,
                    Font = key.Font,
                    Padding = key.Padding,
                    Margin = key.Margin,
                    MinimumSize = key.MinimumSize
                };

                if (key is TableLayoutPanel tlp)
                {
                    s.IsTableLayout = true;
                    var cols = new List<(SizeType, float)>();
                    foreach (ColumnStyle style in tlp.ColumnStyles) cols.Add((style.SizeType, style.Width));
                    s.ColumnStyles = cols.ToArray();

                    var rows = new List<(SizeType, float)>();
                    foreach (RowStyle style in tlp.RowStyles) rows.Add((style.SizeType, style.Height));
                    s.RowStyles = rows.ToArray();
                }

                if (key is SplitContainer sc)
                {
                    s.IsSplitContainer = true;
                    s.SplitterDistance = sc.SplitterDistance;
                }

                return s;
            });
        }
    }
}
