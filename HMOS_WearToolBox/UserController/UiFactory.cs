using System.Drawing;
using System.Windows.Forms;

namespace HMOS_WearToolBox.UserController
{
    internal static class UiFactory
    {
        // 按钮按文字自适应、只用 MinimumSize 保底：写死宽度在高 DPI 下会被中文撑破。
        public static Button MakeButton(string text)
        {
            return MakeButton(text, 112, 34, new Padding(3));
        }

        public static Button MakeButton(string text, int minWidth, int minHeight)
        {
            return MakeButton(text, minWidth, minHeight, new Padding(3));
        }

        public static Button MakeButton(string text, int minWidth, int minHeight, Padding margin)
        {
            return new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10, 0, 10, 0),
                Margin = margin,
                MinimumSize = new Size(minWidth, minHeight),
                UseVisualStyleBackColor = true
            };
        }
    }
}
