using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace HMOS_WearToolBox.UserController
{
    /// <summary>
    /// 设置控件，提供应用程序设置界面，包括自动更新、终端字体和颜色等配置。
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        /// <summary>
        /// 自动更新功能的启用状态。
        /// </summary>
        private bool autoUpdateEnabled;

        /// <summary>
        /// 初始化设置控件，加载系统字体列表、当前设置，并绑定事件。
        /// </summary>
        public SettingsControl()
        {
            InitializeComponent();

            // 加载系统字体列表
            LoadSystemFonts();

            // 加载当前设置
            LoadSettings();

            // 注册事件（参数类型改为 object，符合 EventHandler 委托）
            btnAutoUpdateControl.Click += BtnAutoUpdateControl_Click;
            buttonChooseFontColor.Click += ButtonChooseFontColor_Click;
            buttonChooseTerminalBackgroundColor.Click += ButtonChooseTerminalBackgroundColor_Click;
            btnSave.Click += BtnSave_Click;
            btnRestoreDefault.Click += BtnRestoreDefault_Click;
        }

        /// <summary>
        /// 加载系统已安装的字体列表到字体下拉框中。
        /// </summary>
        private void LoadSystemFonts()
        {
            using (var fonts = new System.Drawing.Text.InstalledFontCollection())
            {
                foreach (var font in fonts.Families)
                {
                    comboBoxFont.Items.Add(font.Name);
                }
            }
            comboBoxFont.Sorted = true;
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

            // 终端字体
            string fontName = Properties.Settings.Default.TerminalFontName;
            if (comboBoxFont.Items.Contains(fontName))
                comboBoxFont.SelectedItem = fontName;
            else if (comboBoxFont.Items.Count > 0)
                comboBoxFont.SelectedIndex = 0;

            numericUpDownFontSize.Value = (decimal)Properties.Settings.Default.TerminalFontSize;

            // 终端颜色
            panelShowFontColor.BackColor = Properties.Settings.Default.TerminalForeColor;
            panelShowTerminalBackgroundColor.BackColor = Properties.Settings.Default.TerminalBackColor;
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
        /// 恢复终端设置为默认值
        /// </summary>
        private void BtnRestoreDefault_Click(object sender, EventArgs e)
        {
            // 默认终端设置
            string defaultFontName = "Cascadia Mono";
            float defaultFontSize = 12f;
            Color defaultForeColor = Color.White;
            Color defaultBackColor = Color.Black;

            // 更新界面控件
            if (comboBoxFont.Items.Contains(defaultFontName))
                comboBoxFont.SelectedItem = defaultFontName;
            else if (comboBoxFont.Items.Count > 0)
                comboBoxFont.SelectedIndex = 0;

            numericUpDownFontSize.Value = (decimal)defaultFontSize;
            panelShowFontColor.BackColor = defaultForeColor;
            panelShowTerminalBackgroundColor.BackColor = defaultBackColor;

            // 立即重置
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
        /// 保存按钮的点击事件：将所有设置保存到应用程序设置文件，并通知主窗体应用新配置。
        /// </summary>
        private void BtnSave_Click(object sender, EventArgs e)
        {
            // 保存自动更新设置
            Properties.Settings.Default.AutoRefreshInterval = (int)numericUpDownAutoUpdateTime.Value;
            Properties.Settings.Default.AutoUpdateEnabled = autoUpdateEnabled;

            // 保存终端设置
            if (comboBoxFont.SelectedItem != null)
                Properties.Settings.Default.TerminalFontName = comboBoxFont.SelectedItem.ToString();
            Properties.Settings.Default.TerminalFontSize = (float)numericUpDownFontSize.Value;
            Properties.Settings.Default.TerminalForeColor = panelShowFontColor.BackColor;
            Properties.Settings.Default.TerminalBackColor = panelShowTerminalBackgroundColor.BackColor;

            Properties.Settings.Default.Save();

            // 通知主窗体更新
            if (ParentForm is MainForm mainForm)
            {
                mainForm.UpdateRefreshInterval((int)numericUpDownAutoUpdateTime.Value);
                if (autoUpdateEnabled)
                    mainForm.SetAutoUpdateEnabled(true);
                mainForm.UpdateTerminalStyles();
            }

            MessageBox.Show("设置已保存", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}