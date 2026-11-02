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
        private SnapshotControl snapshotControl;
        private TerminalControl terminalControl;
        private SettingsControl settingsControl;
        private AboutControl aboutControl;
        private UserControl? currentPage;
        private float lastZoom = 1f;

        private const float MinZoom = 0.55f;

        public MainForm()
        {
            InitializeComponent();

            // 先按屏幕工作区收缩一次：设计尺寸 878x1044，1080p 笔记本上会比屏幕还高
            ApplyAdaptiveBounds();

            // 只创建一次；尺寸与停靠由 FitPage/ShowPage 负责（窗口小于设计尺寸时改出滚动条，不再硬压控件）
            homeControl = new HomeControl();
            softwareControl = new SoftwareControl();
            snapshotControl = new SnapshotControl();
            terminalControl = new TerminalControl();
            settingsControl = new SettingsControl();
            aboutControl = new AboutControl();

            contentPanel.AutoScroll = true;
            contentPanel.Resize += (s2, e2) => FitPage();

            // 绑定按钮事件
            btnHome.Click += (s, e) => ShowPage(homeControl);
            btnSoftware.Click += (s, e) => ShowPage(softwareControl);
            btnSnapshot.Click += (s, e) => ShowPage(snapshotControl);
            btnTerminal.Click += (s, e) => ShowPage(terminalControl);
            btnSettings.Click += (s, e) => ShowPage(settingsControl);
            btnAbout.Click += (s, e) => ShowPage(aboutControl);

            // 默认显示首页
            ShowPage(homeControl);
        }

        /// <summary>
        /// 按屏幕工作区收缩初始尺寸：设计尺寸是 878x1044（144 DPI），
        /// 但 1080p/768p 笔记本的工作区高度只有 1000 出头，硬按设计尺寸开窗会超出屏幕、
        /// 底部按钮点不到。这里取「设计尺寸」和「工作区减 40px」的较小值，并居中显示。
        /// </summary>
        private void ApplyAdaptiveBounds()
        {
            Rectangle area = Screen.PrimaryScreen != null ? Screen.PrimaryScreen.WorkingArea : Screen.GetWorkingArea(this);
            int maxWidth = Math.Max(760, area.Width - 40);
            int maxHeight = Math.Max(560, area.Height - 40);
            int width = Math.Min(ClientSize.Width, maxWidth);
            int height = Math.Min(ClientSize.Height, maxHeight);
            if (width != ClientSize.Width || height != ClientSize.Height)
            {
                ClientSize = new Size(width, height);
            }
            StartPosition = FormStartPosition.Manual;
            Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
        }

        /// <summary>
        /// 页面等比缩放：窗口装得下就按设计尺寸铺满（和原来的 Dock=Fill 一致），
        /// 装不下就整页等比缩小（1080P 笔记本 / 高缩放比时不至于把内容挤重叠或逼用户滚来滚去）。
        ///
        /// 页面里的控件绝大多数是绝对坐标（首页 HomeControl 光设计器就 785 行），
        /// 靠 WinForms 自己的 Dock/Anchor 是压不出比例的，所以缩放交给 Helper\UiScaler：
        /// 它记住每个控件的原始几何和字号，每次从原始值乘以同一个系数，来回拖窗口不会累积误差。
        /// </summary>
        private void FitPage()
        {
            if (currentPage == null) return;

            // 设计尺寸 878x1004 本身就是 144 DPI 下的像素值，只能按「DeviceDpi / 144」换算；
            // 用 LogicalToDeviceUnits 会再乘 1.5，窗口一开就出横向滚动条。
            float k = DeviceDpi / 144f;
            float designWidth = 878f * k;
            float designHeight = 1004f * k;
            int availWidth = contentPanel.ClientSize.Width;
            int availHeight = contentPanel.ClientSize.Height;
            if (availWidth <= 0 || availHeight <= 0) return;

            // 内容比窗口高时 contentPanel 会冒出竖向滚动条，它吃掉十几像素宽度；
            // 先把它扣掉再算比例，否则会出现「出滚动条 → 可用宽度变小 → 缩小一点 → 滚动条消失 → 又变大」的来回抖。
            if (contentPanel.VerticalScroll.Visible) availWidth -= SystemInformation.VerticalScrollBarWidth;
            if (availWidth <= 0) return;

            float zoom = Math.Min(1f, Math.Min(availWidth / designWidth, availHeight / designHeight));
            if (zoom < MinZoom) zoom = MinZoom;

            bool same = Math.Abs(zoom - lastZoom) < 0.002f;
            // 铺满时留 1px 余量：正好等于可用宽度时，AutoScroll 偶尔会因为取整冒出横向滚动条
            int targetWidth = zoom >= 0.995f ? Math.Max(1, availWidth - 1) : (int)Math.Round(designWidth * zoom);
            int targetHeight = zoom >= 0.995f ? availHeight : (int)Math.Round(designHeight * zoom);
            if (same && currentPage.Width == targetWidth && currentPage.Height == targetHeight) return;
            lastZoom = zoom;

            // 先按系数重排（此时页面还是上一轮的尺寸，UiScaler 用的是它自己的快照，不受影响）
            UiScaler.Apply(currentPage, zoom);

            // 再把页面本身摆到内容区（缩小后水平居中，顶部对齐）
            int x = zoom >= 0.995f ? 0 : Math.Max(0, (availWidth - targetWidth) / 2);
            currentPage.Bounds = new Rectangle(x, 0, targetWidth, targetHeight);
       }


        private void ShowPage(UserControl page)
        {
            contentPanel.SuspendLayout();
            contentPanel.Controls.Clear();
            currentPage = page;
            page.Dock = DockStyle.None;
            page.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            contentPanel.Controls.Add(page);
            FitPage();
            contentPanel.ResumeLayout(true);
            page.PerformLayout();

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
                // 关窗不能被 hdc 拖住：给一个短超时，超时就放弃
                HdcHelper.RunHdcCommand("kill", HdcHelper.QuickTimeoutMs);
            }
            catch { }
            HdcHelper.Cleanup();
            base.OnFormClosing(e);
        }

        public void ClearSoftwareList()
        {
            softwareControl?.ClearAppList();
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

        public void UpdateTerminalStyles()
        {
            terminalControl?.ApplySettings();
        }
    }
}