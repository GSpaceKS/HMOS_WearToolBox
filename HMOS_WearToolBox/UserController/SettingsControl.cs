#nullable disable
using HMOS_WearToolBox.Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace HMOS_WearToolBox.UserController
{
    /// <summary>
    /// 设置控件，提供应用程序设置界面，包括自动更新、终端字体和颜色、软件全局字体等配置。
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        // 自动更新功能的启用状态
        private bool autoUpdateEnabled;
        // 记录软件字体加载时的原始值（用于判断是否改变）
        private string _originalGlobalFont;

        /// <summary>
        /// 初始化设置控件，加载系统字体列表、当前设置，并绑定事件。
        /// </summary>
        public SettingsControl()
        {
            InitializeComponent();

            // 加载系统字体列表（包含嵌入字体分组）—— 用于终端字体
            LoadSystemFonts();
            // 加载软件字体下拉框
            LoadGlobalFontComboBox();

            // 加载当前设置
            LoadSettings();

            // 注册事件
            btnAutoUpdateControl.Click += BtnAutoUpdateControl_Click;
            buttonChooseFontColor.Click += ButtonChooseFontColor_Click;
            buttonChooseTerminalBackgroundColor.Click += ButtonChooseTerminalBackgroundColor_Click;
            btnSave.Click += BtnSave_Click;
            btnRestoreDefault.Click += BtnRestoreDefault_Click;
            btnGlobalFontRestoreDefault.Click += BtnGlobalFontRestoreDefault_Click;
        }

        /// <summary>
        /// 用于下拉框的项，支持分隔符/分组标题。
        /// </summary>
        public class ComboBoxItem
        {
            public string Text { get; set; }
            public bool IsSeparator { get; set; }
            public override string ToString() => Text;
        }

        /// <summary>
        /// 加载系统字体列表，并添加嵌入字体分组（用于终端）。
        /// </summary>
        private void LoadSystemFonts()
        {
            comboBoxFont.Items.Clear();

            // 添加分组标题
            comboBoxFont.Items.Add(new ComboBoxItem { Text = "--- 软件内嵌字体 ---", IsSeparator = true });

            // 添加已加载的嵌入字体名称
            var embeddedFonts = FontHelper.GetEmbeddedFontNames();
            foreach (var fontName in embeddedFonts.OrderBy(n => n))
            {
                comboBoxFont.Items.Add(new ComboBoxItem { Text = fontName, IsSeparator = false });
            }

            // 添加分组标题
            comboBoxFont.Items.Add(new ComboBoxItem { Text = "--- 系统字体 ---", IsSeparator = true });

            // 添加系统字体
            using (var fonts = new System.Drawing.Text.InstalledFontCollection())
            {
                var systemFonts = fonts.Families.Select(f => f.Name).Distinct().OrderBy(n => n).ToList();
                foreach (var fontName in systemFonts)
                {
                    comboBoxFont.Items.Add(new ComboBoxItem { Text = fontName, IsSeparator = false });
                }
            }

            // 设置自定义绘制，使分隔线灰色斜体显示，且不可选
            comboBoxFont.DrawMode = DrawMode.OwnerDrawFixed;
            comboBoxFont.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxFont.DrawItem += ComboBoxFont_DrawItem;
            comboBoxFont.MeasureItem += ComboBoxFont_MeasureItem;
            comboBoxFont.SelectedIndexChanged += ComboBoxFont_SelectedIndexChanged;

            // 设置当前选中项（根据保存的设置）
            string currentFont = Properties.Settings.Default.TerminalFontName;
            for (int i = 0; i < comboBoxFont.Items.Count; i++)
            {
                var item = comboBoxFont.Items[i] as ComboBoxItem;
                if (item != null && !item.IsSeparator && item.Text == currentFont)
                {
                    comboBoxFont.SelectedIndex = i;
                    break;
                }
            }
            if (comboBoxFont.SelectedIndex == -1 && comboBoxFont.Items.Count > 0)
                comboBoxFont.SelectedIndex = 0; // 默认选中第一个有效项
        }

        /// <summary>
        /// 自定义绘制下拉框项（用于分隔线样式）。
        /// </summary>
        private void ComboBoxFont_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            var combo = sender as ComboBox;
            if (combo == null) return;

            var item = combo.Items[e.Index] as ComboBoxItem;
            if (item == null) return;

            e.DrawBackground();
            try
            {
                // 确保绘制区域有效
                if (e.Bounds.Width <= 0 || e.Bounds.Height <= 0) return;

                Font drawFont;
                if (item.IsSeparator)
                {
                    // 分隔符使用斜体样式（临时创建）
                    drawFont = new Font(combo.Font, FontStyle.Italic);
                }
                else
                {
                    // 普通项使用组合框的字体（无需释放）
                    drawFont = combo.Font;
                }

                // 创建画笔（需要释放）
                using (var brush = new SolidBrush(item.IsSeparator ? Color.Gray : e.ForeColor))
                {
                    e.Graphics.DrawString(item.Text, drawFont, brush, e.Bounds, StringFormat.GenericDefault);
                }

                // 如果分隔符字体是临时创建的，需要释放
                if (item.IsSeparator && drawFont != combo.Font)
                {
                    drawFont.Dispose();
                }
            }
            catch (Exception ex)
            {
                // 忽略绘制异常，避免崩溃（调试时可输出）
                System.Diagnostics.Debug.WriteLine($"DrawItem error: {ex.Message}");
            }
            e.DrawFocusRectangle();
        }

        /// <summary>
        /// 设置下拉框项的高度。
        /// </summary>
        private void ComboBoxFont_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = (int)(comboBoxFont.Font.Height * 1.2);
        }

        /// <summary>
        /// 防止选中分隔线（分组标题），自动回退到上一个有效项。
        /// </summary>
        private void ComboBoxFont_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxFont.SelectedIndex >= 0)
            {
                var selectedItem = comboBoxFont.Items[comboBoxFont.SelectedIndex] as ComboBoxItem;
                if (selectedItem != null && selectedItem.IsSeparator)
                {
                    // 回退到上一个有效项
                    int previousValid = comboBoxFont.SelectedIndex - 1;
                    while (previousValid >= 0)
                    {
                        var prev = comboBoxFont.Items[previousValid] as ComboBoxItem;
                        if (prev != null && !prev.IsSeparator)
                        {
                            comboBoxFont.SelectedIndex = previousValid;
                            break;
                        }
                        previousValid--;
                    }
                    if (comboBoxFont.SelectedIndex == -1 && comboBoxFont.Items.Count > 0)
                        comboBoxFont.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// 加载软件字体下拉框（分组显示）。
        /// </summary>
        private void LoadGlobalFontComboBox()
        {
            comboBoxGlobalFont.Items.Clear();

            // 添加分组标题
            comboBoxGlobalFont.Items.Add(new ComboBoxItem { Text = "--- 软件内嵌字体 ---", IsSeparator = true });
            var embeddedFonts = FontHelper.GetEmbeddedFontNames();
            foreach (var fontName in embeddedFonts.OrderBy(n => n))
            {
                comboBoxGlobalFont.Items.Add(new ComboBoxItem { Text = fontName, IsSeparator = false });
            }

            // 添加分组标题
            comboBoxGlobalFont.Items.Add(new ComboBoxItem { Text = "--- 系统字体 ---", IsSeparator = true });
            using (var fonts = new System.Drawing.Text.InstalledFontCollection())
            {
                var systemFonts = fonts.Families.Select(f => f.Name).Distinct().OrderBy(n => n).ToList();
                foreach (var fontName in systemFonts)
                {
                    comboBoxGlobalFont.Items.Add(new ComboBoxItem { Text = fontName, IsSeparator = false });
                }
            }

            // 设置自定义绘制（复用终端字体的绘制事件）
            comboBoxGlobalFont.DrawMode = DrawMode.OwnerDrawFixed;
            comboBoxGlobalFont.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxGlobalFont.DrawItem += ComboBoxFont_DrawItem; // 复用同一个绘制方法
            comboBoxGlobalFont.MeasureItem += ComboBoxFont_MeasureItem;
            comboBoxGlobalFont.SelectedIndexChanged += ComboBoxGlobalFont_SelectedIndexChanged;
        }

        /// <summary>
        /// 防止选中分隔线（分组标题），自动回退到上一个有效项。
        /// </summary>
        private void ComboBoxGlobalFont_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxGlobalFont.SelectedIndex >= 0)
            {
                var selectedItem = comboBoxGlobalFont.Items[comboBoxGlobalFont.SelectedIndex] as ComboBoxItem;
                if (selectedItem != null && selectedItem.IsSeparator)
                {
                    int previousValid = comboBoxGlobalFont.SelectedIndex - 1;
                    while (previousValid >= 0)
                    {
                        var prev = comboBoxGlobalFont.Items[previousValid] as ComboBoxItem;
                        if (prev != null && !prev.IsSeparator)
                        {
                            comboBoxGlobalFont.SelectedIndex = previousValid;
                            break;
                        }
                        previousValid--;
                    }
                    if (comboBoxGlobalFont.SelectedIndex == -1 && comboBoxGlobalFont.Items.Count > 0)
                        comboBoxGlobalFont.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// 从应用程序设置中加载已保存的配置，并更新界面控件状态。
        /// </summary>
        private void LoadSettings()
        {
            // 自动更新相关
            autoUpdateEnabled = Properties.Settings.Default.AutoUpdateEnabled;
            UpdateAutoUpdateButtonUI();

            numericUpDownAutoUpdateTime.Value = Properties.Settings.Default.AutoRefreshInterval;

            // 终端字体（由 LoadSystemFonts 中已设置选中项，这里只确保界面同步）
            // 颜色预览
            panelShowFontColor.BackColor = Properties.Settings.Default.TerminalForeColor;
            panelShowTerminalBackgroundColor.BackColor = Properties.Settings.Default.TerminalBackColor;
            numericUpDownFontSize.Value = (decimal)Properties.Settings.Default.TerminalFontSize;

            // 软件字体设置
            string globalFontName = Properties.Settings.Default.GlobalFontName;
            _originalGlobalFont = globalFontName; // 记录原始值
            for (int i = 0; i < comboBoxGlobalFont.Items.Count; i++)
            {
                var item = comboBoxGlobalFont.Items[i] as ComboBoxItem;
                if (item != null && !item.IsSeparator && item.Text == globalFontName)
                {
                    comboBoxGlobalFont.SelectedIndex = i;
                    break;
                }
            }
            if (comboBoxGlobalFont.SelectedIndex == -1 && comboBoxGlobalFont.Items.Count > 0)
                comboBoxGlobalFont.SelectedIndex = 0;
        }

        /// <summary>
        /// 根据自动更新启用状态更新按钮的显示文本和颜色，并控制时间设置控件的可用性。
        /// </summary>
        private void UpdateAutoUpdateButtonUI()
        {
            if (autoUpdateEnabled)
            {
                btnAutoUpdateControl.Text = "开";
                btnAutoUpdateControl.ForeColor = Color.Green;
                numericUpDownAutoUpdateTime.Enabled = true;
            }
            else
            {
                btnAutoUpdateControl.Text = "关";
                btnAutoUpdateControl.ForeColor = Color.Red;
                numericUpDownAutoUpdateTime.Enabled = false;
            }
        }

        /// <summary>
        /// 保存按钮的点击事件：将所有设置保存到应用程序设置文件，并通知主窗体应用新配置。
        /// </summary>
        private void BtnSave_Click(object sender, EventArgs e)
        {
            // 保存自动更新设置
            Properties.Settings.Default.AutoRefreshInterval = (int)numericUpDownAutoUpdateTime.Value;
            Properties.Settings.Default.AutoUpdateEnabled = autoUpdateEnabled;

            // 保存终端设置
            if (comboBoxFont.SelectedItem != null)
            {
                var selected = comboBoxFont.SelectedItem as ComboBoxItem;
                if (selected != null && !selected.IsSeparator)
                    Properties.Settings.Default.TerminalFontName = selected.Text;
            }
            Properties.Settings.Default.TerminalFontSize = (float)numericUpDownFontSize.Value;
            Properties.Settings.Default.TerminalForeColor = panelShowFontColor.BackColor;
            Properties.Settings.Default.TerminalBackColor = panelShowTerminalBackgroundColor.BackColor;

            // 保存软件字体设置
            string newGlobalFont = "HarmonyOS Sans SC"; // 默认值
            if (comboBoxGlobalFont.SelectedItem is ComboBoxItem selectedFont && !selectedFont.IsSeparator)
                newGlobalFont = selectedFont.Text;
            Properties.Settings.Default.GlobalFontName = newGlobalFont;

            Properties.Settings.Default.Save();

            // 通知主窗体更新
            if (ParentForm is MainForm mainForm)
            {
                mainForm.UpdateRefreshInterval((int)numericUpDownAutoUpdateTime.Value);
                if (autoUpdateEnabled)
                    mainForm.SetAutoUpdateEnabled(true);
                mainForm.UpdateTerminalStyles();
            }

            // 检查软件字体是否改变
            bool globalFontChanged = (_originalGlobalFont != newGlobalFont);
            if (globalFontChanged)
            {
                DialogResult result = MessageBox.Show(
                    "软件字体已更改，需要重启软件才能完全生效。是否立即重启？",
                    "提示",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    Application.Restart();
                    Environment.Exit(0);
                }
                else
                {
                    MessageBox.Show("设置已保存，字体将在下次启动时应用。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// 自动更新按钮的点击事件处理：切换自动更新状态，保存设置，并通知主窗体。
        /// </summary>
        private void BtnAutoUpdateControl_Click(object sender, EventArgs e)
        {
            autoUpdateEnabled = !autoUpdateEnabled;
            Properties.Settings.Default.AutoUpdateEnabled = autoUpdateEnabled;
            Properties.Settings.Default.Save();

            UpdateAutoUpdateButtonUI();

            if (ParentForm is MainForm mainForm)
            {
                mainForm.SetAutoUpdateEnabled(autoUpdateEnabled);
            }
        }

        /// <summary>
        /// 选择字体颜色按钮的点击事件：打开颜色对话框，设置终端字体颜色。
        /// </summary>
        private void ButtonChooseFontColor_Click(object sender, EventArgs e)
        {
            using (ColorDialog dlg = new ColorDialog())
            {
                dlg.Color = panelShowFontColor.BackColor;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    panelShowFontColor.BackColor = dlg.Color;
                }
            }
        }

        /// <summary>
        /// 选择终端背景色按钮的点击事件：打开颜色对话框，设置终端背景颜色。
        /// </summary>
        private void ButtonChooseTerminalBackgroundColor_Click(object sender, EventArgs e)
        {
            using (ColorDialog dlg = new ColorDialog())
            {
                dlg.Color = panelShowTerminalBackgroundColor.BackColor;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    panelShowTerminalBackgroundColor.BackColor = dlg.Color;
                }
            }
        }

        /// <summary>
        /// 恢复终端设置为默认值（不影响全局设置）。
        /// </summary>
        private void BtnRestoreDefault_Click(object sender, EventArgs e)
        {
            // 默认终端设置
            string defaultFontName = "Cascadia Mono";
            float defaultFontSize = 12f;
            Color defaultForeColor = Color.White;
            Color defaultBackColor = Color.Black;

            // 更新界面控件
            int targetIndex = -1;
            for (int i = 0; i < comboBoxFont.Items.Count; i++)
            {
                var item = comboBoxFont.Items[i] as ComboBoxItem;
                if (item != null && !item.IsSeparator && item.Text == defaultFontName)
                {
                    targetIndex = i;
                    break;
                }
            }
            if (targetIndex != -1)
                comboBoxFont.SelectedIndex = targetIndex;
            else if (comboBoxFont.Items.Count > 0)
                comboBoxFont.SelectedIndex = 0;

            numericUpDownFontSize.Value = (decimal)defaultFontSize;
            panelShowFontColor.BackColor = defaultForeColor;
            panelShowTerminalBackgroundColor.BackColor = defaultBackColor;

            // 立即保存默认值并刷新终端
            Properties.Settings.Default.TerminalFontName = defaultFontName;
            Properties.Settings.Default.TerminalFontSize = defaultFontSize;
            Properties.Settings.Default.TerminalForeColor = defaultForeColor;
            Properties.Settings.Default.TerminalBackColor = defaultBackColor;
            Properties.Settings.Default.Save();

            if (ParentForm is MainForm mainForm)
                mainForm.UpdateTerminalStyles();

            MessageBox.Show("终端设置已恢复为默认值", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 软件字体恢复默认按钮点击事件：恢复默认字体并提示是否重启。
        /// </summary>
        private void BtnGlobalFontRestoreDefault_Click(object sender, EventArgs e)
        {
            string defaultFont = "HarmonyOS Sans SC Medium";

            // 在下拉框中选中默认字体
            for (int i = 0; i < comboBoxGlobalFont.Items.Count; i++)
            {
                if (comboBoxGlobalFont.Items[i] is ComboBoxItem item && !item.IsSeparator && item.Text == defaultFont)
                {
                    comboBoxGlobalFont.SelectedIndex = i;
                    break;
                }
            }

            // 更新原始值记录（以便保存时判断是否改变）
            _originalGlobalFont = defaultFont;

            // 立即保存到设置文件
            Properties.Settings.Default.GlobalFontName = defaultFont;
            Properties.Settings.Default.Save();

            // 提示用户是否重启
            DialogResult result = MessageBox.Show(
                "软件字体已恢复为默认值，需要重启软件才能生效。是否立即重启？",
                "提示",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                Application.Restart();
                Environment.Exit(0);
            }
            else
            {
                MessageBox.Show("字体已恢复，将在下次启动时应用。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}