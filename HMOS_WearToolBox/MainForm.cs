using System;
using System.Windows.Forms;
using HMOS_WearToolBox.UserController;
using HMOS_WearToolBox.Helper;

namespace HMOS_WearToolBox
{
    public partial class MainForm : Form
    {
        private HomeControl homeControl;
        private SoftwareControl softwareControl;
        private TerminalControl terminalControl;
        private SettingsControl settingsControl;
        private AboutControl aboutControl;

        public MainForm()
        {
            InitializeComponent();

            // 只创建一次，并设置 Dock
            homeControl = new HomeControl { Dock = DockStyle.Fill };
            softwareControl = new SoftwareControl { Dock = DockStyle.Fill };
            terminalControl = new TerminalControl { Dock = DockStyle.Fill };
            settingsControl = new SettingsControl { Dock = DockStyle.Fill };
            aboutControl = new AboutControl { Dock = DockStyle.Fill };

            // 绑定按钮事件
            btnHome.Click += (s, e) => ShowPage(homeControl);
            btnSoftware.Click += (s, e) => ShowPage(softwareControl);
            btnTerminal.Click += (s, e) => ShowPage(terminalControl);
            btnSettings.Click += (s, e) => ShowPage(settingsControl);
            btnAbout.Click += (s, e) => ShowPage(aboutControl);

            // 默认显示首页
            ShowPage(homeControl);
        }

        private void ShowPage(UserControl page)
        {
            contentPanel.Controls.Clear();
            contentPanel.Controls.Add(page);

            // 切换到软件页面时，主动刷新连接状态
            if (page == softwareControl)
            {
                softwareControl.RefreshConnectionStatus();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                HdcHelper.RunHdcCommand("kill");
            }
            catch { }
            HdcHelper.Cleanup();
            base.OnFormClosing(e);
        }

        public void ClearSoftwareList()
        {
            softwareControl?.ClearAppList();
        }

        public void RefreshSoftwareConnection()
        {
            softwareControl?.RefreshConnectionStatus();
        }

        public void ForceRefreshSoftware()
        {
            softwareControl?.ForceRefresh();
        }

        /// <summary>
        /// 更新 HomeControl 的自动刷新间隔（由设置页面调用）
        /// </summary>
        /// <param name="seconds">刷新间隔（秒）</param>
        public void UpdateRefreshInterval(int seconds)
        {
            homeControl?.SetRefreshInterval(seconds);
        }

        /// <summary>
        /// 自动更新开关
        /// </summary>
        /// <param name="enabled"></param>
        public void SetAutoUpdateEnabled(bool enabled)
        {
            homeControl?.SetAutoUpdateEnabled(enabled);
        }

        /// <summary>
        /// Terminal 字体、背景设置
        /// </summary>
        public void UpdateTerminalStyles()
        {
            terminalControl?.ApplySettings();
        }
        private void MainForm_Load(object sender, EventArgs e) { }
    }
}