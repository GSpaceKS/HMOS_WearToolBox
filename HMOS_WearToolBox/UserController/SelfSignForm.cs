using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using HMOS_WearToolBox.Helper;

namespace HMOS_WearToolBox.UserController
{
    /// <summary>「安装第三方应用（自签）」向导：换包名重打包 → 自己的证书签名 → 推到手表安装。</summary>
    public class SelfSignForm : Form
    {
        private const string AgcCertUrl = "https://developer.huawei.com/consumer/cn/service/josp/agc/index.html#/harmonyOSDevPlatform/9249519184596237889";
        private const string AgcDeviceUrl = "https://developer.huawei.com/consumer/cn/service/josp/agc/index.html#/harmonyOSDevPlatform/9249519184596237893";
        private const string AgcProfileUrl = "https://developer.huawei.com/consumer/cn/service/josp/agc/index.html#/harmonyOSDevPlatform/172249065903274468";
        private const string AgcAppIdUrl = "https://developer.huawei.com/consumer/cn/service/josp/agc/index.html#/harmonyOSDevPlatform/172249065903274453";

        private readonly SigningSettings settings;

        private HapInfo? hapInfo;
        private string certFile = string.Empty;
        private string profileFile = string.Empty;
        private bool busy;

        private TabControl tabMain = null!;

        // ① 应用包
        private TextBox txtHap = null!;
        private Label lblBundle = null!;
        private Label lblVersion = null!;
        private Label lblModule = null!;
        private Label lblSize = null!;
        private TextBox txtNewBundle = null!;
        private Label lblBundleState = null!;

        // ② 证书
        private Label lblKeyStatus = null!;
        private Label lblUdid = null!;
        private Label lblAppIdBundle = null!;
        private TextBox txtCert = null!;

        // ③ Profile
        private TextBox txtProfile = null!;
        private Label lblProfileInfo = null!;

        // ④ 安装
        private Label lblSummary = null!;
        private TextBox txtLog = null!;
        private ProgressBar progress = null!;
        private Button btnStart = null!;

        private Label lblStatus = null!;

        public SelfSignForm()
        {
            settings = SigningSettings.Load();
            BuildUi();
            ApplySettings();

            // 内嵌的签名工具链有 25MB，首次使用要解压一两秒；先在后台预热，
            // 免得用户点「生成密钥与 CSR」时界面卡一下。
            _ = Task.Run(() => SignTool.EnsureBundled(out _));

            // TabPage 在构造时还没有真实尺寸，等窗口显示出来（以及之后每次缩放）再把日志框拉满。
            Shown += (s, e) =>
            {
                // 屏幕小的时候别撑破工作区：先按可用空间收一下，再排版
                Rectangle area = Screen.FromControl(this).WorkingArea;
                int w = Math.Min(ClientSize.Width, area.Width - 80);
                int h = Math.Min(ClientSize.Height, area.Height - 80);
                if (w < ClientSize.Width || h < ClientSize.Height)
                    ClientSize = new Size(Math.Max(720, w), Math.Max(520, h));
                AutoReadUdid();
                BeginInvoke(new Action(LayoutSections));
            };

            // Resize 触发的那一刻 TabControl 往往还没跟着改完尺寸，直接用 page.ClientSize 会算成旧宽度，
            // 内容就被推到窗口外、底部还冒出横向滚动条。放到消息队列末尾再排一次。
            Resize += (s, e) =>
            {
                if (IsHandleCreated)
                    BeginInvoke(new Action(LayoutSections));
            };

            // 切页签时 TabPage 才被撑到真实尺寸；尺寸一变就重排，保证永远按真实宽度算。
            // 排版本身会改变滚动条的出现与否，从而再次触发 ClientSizeChanged，所以要防重入。
            foreach (TabPage tab in tabMain.TabPages)
                tab.ClientSizeChanged += (s, e) => LayoutSections();

            tabMain.SelectedIndexChanged += (s, e) =>
            {
                if (IsHandleCreated)
                    BeginInvoke(new Action(LayoutSections));
            };
        }

        private void BuildUi()
        {
            Text = "安装第三方应用（自签）";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(920, 900);
            MinimumSize = new Size(720, 520);
            AutoScaleDimensions = new SizeF(144F, 144F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Microsoft YaHei UI", 9F);

            Panel panelHeader = new Panel { Dock = DockStyle.Top, Height = 86, BackColor = Color.White };
            Label lblTitle = new Label
            {
                Text = "安装第三方应用（自签）",
                Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(18, 10)
            };
            Label lblSub = new Label
            {
                Text = "换掉被占用的包名，再用你自己华为账号的调试证书签名，最后推进手表安装。",
                ForeColor = Color.DimGray,
                AutoSize = true,
                Location = new Point(20, 48)
            };
            panelHeader.Controls.Add(lblTitle);
            panelHeader.Controls.Add(lblSub);

            Panel panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 58, BackColor = SystemColors.Control };
            lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(18, 0, 8, 0),
                ForeColor = Color.DimGray,
                AutoEllipsis = true,
                Text = "准备就绪。"
            };
            Button btnClose = UiFactory.MakeButton("关闭", 96, 34);
            btnClose.Dock = DockStyle.Right;
            btnClose.Click += (s, e) => Close();
            Panel panelCloseHost = new Panel { Dock = DockStyle.Right, Width = 112, Padding = new Padding(0, 10, 14, 10) };
            panelCloseHost.Controls.Add(btnClose);
            panelBottom.Controls.Add(lblStatus);
            panelBottom.Controls.Add(panelCloseHost);

            tabMain = new TabControl { Dock = DockStyle.Fill, Padding = new Point(14, 6) };
            tabMain.TabPages.Add(BuildHapTab());
            tabMain.TabPages.Add(BuildCertTab());
            tabMain.TabPages.Add(BuildProfileTab());
            tabMain.TabPages.Add(BuildInstallTab());

            // 停靠顺序：Fill 最后 Add 才能拿到剩余空间
            Controls.Add(tabMain);
            Controls.Add(panelBottom);
            Controls.Add(panelHeader);

            // 任何位置都能把文件拖进来：按扩展名自动分派到对应的页
            EnableFileDrop(this);
        }

        private TabPage BuildHapTab()
        {
            TabPage page = new TabPage("① 选择应用") { Padding = new Padding(20, 18, 20, 18), AutoScroll = true };

            Panel hapRow = new Panel { Location = new Point(14, 20), Size = new Size(856, 66) };
            hapRow.Controls.Add(Caption(0, 0, "HAP 文件"));
            txtHap = Box(0, 26, 720, 32, readOnly: true);
            txtHap.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            hapRow.Controls.Add(txtHap);
            Button btnBrowseHap = ButtonAt("浏览…", 740, 26, 96, 32, BtnBrowseHap_Click);
            btnBrowseHap.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            hapRow.Controls.Add(btnBrowseHap);
            page.Controls.Add(hapRow);

            GroupBox groupInfo = Group("包信息", 14, 98, 856, 240);
            groupInfo.Controls.Add(Caption(18, 38, "包名"));
            lblBundle = Value(120, 38, 700);
            groupInfo.Controls.Add(lblBundle);
            groupInfo.Controls.Add(Caption(18, 80, "版本"));
            lblVersion = Value(120, 80, 700);
            groupInfo.Controls.Add(lblVersion);
            groupInfo.Controls.Add(Caption(18, 122, "模块 / 设备类型"));
            lblModule = Value(160, 122, 660);
            groupInfo.Controls.Add(lblModule);
            groupInfo.Controls.Add(Caption(18, 164, "文件大小"));
            lblSize = Value(120, 164, 700);
            groupInfo.Controls.Add(lblSize);
            groupInfo.Controls.Add(Caption(18, 206, "说明"));
            groupInfo.Controls.Add(new Label
            {
                Text = "原包名被作者占用，无法直接申请调试证书，必须换成你自己的包名。",
                ForeColor = Color.DimGray,
                Location = new Point(120, 206),
                Size = new Size(700, 22)
            });
            page.Controls.Add(groupInfo);

            GroupBox groupName = Group("新的包名", 14, 352, 856, 216);
            groupName.Controls.Add(new Label
            {
                Text = "新包名（可以直接改）",
                ForeColor = Color.DimGray,
                Location = new Point(18, 32),
                Size = new Size(820, 22)
            });
            txtNewBundle = Box(18, 66, 340, 32, readOnly: false);
            groupName.Controls.Add(txtNewBundle);
            txtNewBundle.TextChanged += (s, e) =>
            {
                bool valid = ValidateBundleName();
                UpdateSummary();
                if (valid && hapInfo != null)
                {
                    // 记住用户最终选定的包名，下次打开同一份 HAP 直接带出来
                    settings.LastBundle = txtNewBundle.Text.Trim();
                    settings.Save();
                }
            };
            groupName.Controls.Add(ButtonAt("换一个", 368, 66, 88, 32, BtnRegenBundle_Click));
            groupName.Controls.Add(ButtonAt("复制", 466, 66, 88, 32, BtnCopyBundle_Click));
            lblBundleState = new Label
            {
                Location = new Point(18, 116),
                Size = new Size(820, 64),
                ForeColor = Color.DimGray
            };
            groupName.Controls.Add(lblBundleState);
            page.Controls.Add(groupName);

            return page;
        }

        private TabPage BuildCertTab()
        {
            TabPage page = new TabPage("② 调试证书") { Padding = new Padding(20, 18, 20, 18), AutoScroll = true };

            // 第 1 步：注册设备
            GroupBox groupUdid = Group("第 1 步：注册设备（把 UDID 新增到 AGC 设备列表）", 14, 12, 856, 104);
            lblUdid = new Label
            {
                Location = new Point(18, 30),
                Size = new Size(470, 26),
                Text = "尚未读取",
                Font = new Font("Consolas", 9.5F),
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft
            };
            groupUdid.Controls.Add(lblUdid);
            groupUdid.Controls.Add(ButtonAt("从设备读取", 496, 26, 112, 34, BtnReadUdid_Click));
            groupUdid.Controls.Add(ButtonAt("复制", 616, 26, 70, 34, BtnCopyUdid_Click));
            groupUdid.Controls.Add(ButtonAt("打开设备页面", 694, 26, 148, 34, (s, e) => OpenUrl(AgcDeviceUrl)));
            page.Controls.Add(groupUdid);

            // 第 2 步：证书
            GroupBox groupCert = Group("第 2 步：新建调试证书", 14, 130, 856, 212);
            lblKeyStatus = new Label
            {
                // GroupBox 的标题文字占住顶部约 24px，内容必须从 30 以下开始，否则标题会被压住
                Location = new Point(18, 32),
                Size = new Size(820, 46),
                ForeColor = Color.DimGray
            };
            groupCert.Controls.Add(lblKeyStatus);
            groupCert.Controls.Add(ButtonAt("生成密钥与 CSR", 18, 88, 166, 34, BtnMakeKey_Click));
            groupCert.Controls.Add(ButtonAt("打开目录", 196, 88, 110, 34, BtnOpenSignDir_Click));
            groupCert.Controls.Add(ButtonAt("复制 CSR 路径", 318, 88, 154, 34, BtnCopyCsr_Click));
            groupCert.Controls.Add(ButtonAt("打开证书页面", 484, 88, 152, 34, (s, e) => OpenUrl(AgcCertUrl)));
            groupCert.Controls.Add(new Label
            {
                Text = "一份证书可以给很多软件签名，建过一次就不用再建。",
                ForeColor = Color.DimGray,
                Location = new Point(18, 132),
                Size = new Size(820, 22)
            });
            txtCert = Box(18, 162, 600, 32, readOnly: true);
            groupCert.Controls.Add(txtCert);
            groupCert.Controls.Add(ButtonAt("选择 .cer", 630, 162, 106, 32, BtnPickCert_Click));
            page.Controls.Add(groupCert);

            // 第 3 步：APP ID
            GroupBox groupAppId = Group("第 3 步：新建 APP ID（用①页里的新包名建应用，建好后不能改）", 14, 356, 856, 104);
            lblAppIdBundle = new Label
            {
                Location = new Point(18, 32),
                Size = new Size(474, 26),
                Text = "—",
                Font = new Font("Consolas", 9.5F),
                ForeColor = Color.Black,
                TextAlign = ContentAlignment.MiddleLeft
            };
            groupAppId.Controls.Add(lblAppIdBundle);
            groupAppId.Controls.Add(ButtonAt("复制包名", 504, 28, 100, 34, BtnCopyBundle_Click));
            groupAppId.Controls.Add(ButtonAt("打开 APP ID 页面", 616, 28, 222, 34, (s, e) => OpenUrl(AgcAppIdUrl)));
            page.Controls.Add(groupAppId);

            // 第 4 步：Profile
            GroupBox groupProfile = Group("第 4 步：创建 Profile", 14, 474, 856, 132);
            groupProfile.Controls.Add(new Label
            {
                Text = "类型 debug、应用选第 3 步的 APP ID、证书选第 2 步的证书、设备勾选第 1 步的手表。\r\n" +
                       "下载的 .p7b 到「③ Profile」页选入。",
                ForeColor = Color.DimGray,
                Location = new Point(18, 32),
                Size = new Size(820, 46)
            });
            groupProfile.Controls.Add(ButtonAt("打开 Profile 页面", 18, 84, 168, 30, (s, e) => OpenUrl(AgcProfileUrl)));
            page.Controls.Add(groupProfile);

            return page;
        }

        private TabPage BuildProfileTab()
        {
            TabPage page = new TabPage("③ Profile") { Padding = new Padding(20, 18, 20, 18), AutoScroll = true };

            page.Controls.Add(new Label
            {
                Text = "Profile 把「应用 + 证书 + 设备」绑在一起，是系统判定「这台设备能不能装这个应用」的依据。\r\n" +
                       "创建时：类型 debug、应用选新包名、证书选上一步的 .cer、设备勾选这块手表。",
                ForeColor = Color.DimGray,
                Location = new Point(16, 16),
                Size = new Size(840, 50)
            });

            GroupBox groupProfile = Group("AGC 下载的 Profile（.p7b）", 14, 84, 856, 110);
            txtProfile = Box(18, 36, 600, 32, readOnly: true);
            groupProfile.Controls.Add(txtProfile);
            groupProfile.Controls.Add(ButtonAt("选择 .p7b", 630, 36, 106, 32, BtnPickProfile_Click));
            page.Controls.Add(groupProfile);

            GroupBox groupCheck = Group("一致性检查", 14, 212, 856, 300);
            lblProfileInfo = new Label
            {
                Location = new Point(18, 30),
                Size = new Size(820, 258),
                ForeColor = Color.DimGray,
                Text = "选择 Profile 后，这里会显示它绑定的包名和设备，并检查是否和上面填的新包名一致。"
            };
            groupCheck.Controls.Add(lblProfileInfo);
            page.Controls.Add(groupCheck);

            return page;
        }

        private TabPage BuildInstallTab()
        {
            TabPage page = new TabPage("④ 签名并安装") { Padding = new Padding(20, 16, 20, 18), AutoScroll = true };

            lblSummary = new Label
            {
                Location = new Point(16, 8),
                Size = new Size(840, 112),
                ForeColor = Color.DimGray
            };
            page.Controls.Add(lblSummary);

            btnStart = UiFactory.MakeButton("签名并安装", 160, 34);
            btnStart.Location = new Point(16, 126);
            btnStart.Click += async (s, e) => await StartAsync();
            page.Controls.Add(btnStart);

            progress = new ProgressBar
            {
                Location = new Point(190, 126),
                Size = new Size(440, 30),
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 0
            };
            page.Controls.Add(progress);

            txtLog = new TextBox
            {
                Location = new Point(16, 172),
                Size = new Size(840, 320),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 9F),
                BackColor = Color.White
            };
            page.Controls.Add(txtLog);

            return page;
        }

        /// <summary>把 GroupBox 里的控件按行重排，窗口缩放时控件不被切掉。</summary>
        private static void ReflowSection(GroupBox box)
        {
            int innerWidth = box.ClientSize.Width - box.Padding.Horizontal;
            if (innerWidth < 80)
                return;

            List<Control> kids = new List<Control>();
            foreach (Control child in box.Controls)
                kids.Add(child);
            kids.Sort((a, b) =>
            {
                int byTop = a.Top.CompareTo(b.Top);
                return byTop != 0 ? byTop : a.Left.CompareTo(b.Left);
            });

            // 同一行：设计坐标 Top 相差不超过 12px；行内再按 Left 排序
            List<List<Control>> rows = new List<List<Control>>();
            int rowTop = int.MinValue;
            foreach (Control child in kids)
            {
                if (rows.Count == 0 || Math.Abs(child.Top - rowTop) > 12)
                {
                    rows.Add(new List<Control>());
                    rowTop = child.Top;
                }
                rows[rows.Count - 1].Add(child);
            }
            foreach (List<Control> row in rows)
                row.Sort((a, b) => a.Left.CompareTo(b.Left));

            int y = box.Padding.Top;
            foreach (List<Control> row in rows)
            {
                List<Control> buttons = new List<Control>();
                List<Control> others = new List<Control>();
                foreach (Control child in row)
                {
                    if (child is Button)
                        buttons.Add(child);
                    else
                        others.Add(child);
                }

                if (others.Count == 0)
                {
                    // 整行都是按钮：从左边依次排开
                    int bx = box.Padding.Left;
                    foreach (Control button in buttons)
                    {
                        button.Top = y;
                        button.Left = bx;
                        bx += button.Width + 10;
                    }
                }
                else
                {
                    // 有输入框/文字的行：按钮贴右边，路径框和标签占满左边的剩余宽度
                    int right = innerWidth;
                    for (int i = buttons.Count - 1; i >= 0; i--)
                    {
                        Control button = buttons[i];
                        right -= button.Width;
                        button.Top = y;
                        button.Left = right;
                        right -= 10;
                    }

                    int x = box.Padding.Left;
                    foreach (Control child in others)
                    {
                        int available = Math.Max(120, right - x);
                        child.Top = y;
                        child.Left = x;
                        if (child is Label)
                        {
                            // 标签一律自动长高：文字长了就换行，不会再被右边缘切掉
                            child.AutoSize = true;
                            child.MaximumSize = new Size(available, 0);
                        }
                        else if (child is TextBox)
                        {
                            // 单行 TextBox 的 AutoSize 默认是 true，硬赋 Width 会被它忽略，
                            // 路径框就停在设计宽度、右边留一大条空档。这里先关掉再撑满。
                            ((TextBox)child).AutoSize = false;
                            child.Width = available;
                        }
                        else if (child.AutoSize)
                        {
                            child.MaximumSize = new Size(available, 0);
                        }
                        else
                        {
                            child.Width = available;
                        }
                        x += child.Width + 8;
                    }
                }

                int rowHeight = 0;
                foreach (Control child in row)
                    rowHeight = Math.Max(rowHeight, child.Height);
                y += rowHeight + 12;
            }

            box.Height = Math.Max(74, y + box.Padding.Bottom);
        }

        private bool layingOut;

        // 自适应排版：GroupBox 高度按内容算，区块依次往下堆，日志框吃掉剩余高度。
        private void LayoutSections()
        {
            if (layingOut)
                return;

            layingOut = true;
            try
            {
                LayoutSectionsCore();
            }
            finally
            {
                layingOut = false;
            }
        }

        private void LayoutSectionsCore()
        {
            // 只排当前选中的那一页：没被选中的 TabPage 尺寸还停在设计值，
            // 按它算出来的宽度会把内容推出窗口，还硬拉出横向滚动条。
            if (tabMain.SelectedTab is not TabPage page)
                return;

            int width = page.ClientSize.Width - page.Padding.Horizontal;
            if (width <= 80)
                return;

            LayoutPage(page, width);

            // 内容变高后竖向滚动条会冒出来，客户区随之变窄，再排一次免得底部出现横向滚动条。
            if (page.HorizontalScroll.Visible)
                LayoutPage(page, width - SystemInformation.VerticalScrollBarWidth - 6);
        }

        /// <summary>把一页里的区块从上到下堆好，宽度按可用宽度自适应。</summary>
        private void LayoutPage(TabPage page, int width)
        {
            // 控件的设计坐标就是期望的排列顺序；Z 顺序是反的，这里按 Top 排一遍。
            List<Control> ordered = new List<Control>();
            foreach (Control control in page.Controls)
                ordered.Add(control);
            ordered.Sort((a, b) => a.Top.CompareTo(b.Top));

            int y = page.Padding.Top;
            foreach (Control control in ordered)
            {
                if (control == txtLog)
                    continue;

                if (control == progress && btnStart.Parent == page)
                {
                    int px = btnStart.Right + 12;
                    progress.SetBounds(px, btnStart.Top + (btnStart.Height - progress.Height) / 2,
                        Math.Max(120, width - (px - page.Padding.Left) - 12), progress.Height);
                    continue;
                }

                if (control is GroupBox box)
                {
                    box.SetBounds(page.Padding.Left, y, width, box.Height);
                    ReflowSection(box);
                    y = box.Bottom + 16;
                    continue;
                }

                control.Left = page.Padding.Left;
                control.Top = y;
                if (control is Label label)
                {
                    // Label 设了 AutoSize 后 Width 是只读的，硬赋 Width 会被忽略、宽度还是设计值，
                    // 超出客户区就会给整页拉出横向滚动条。所以用 MaximumSize 限制换行宽度。
                    label.AutoSize = true;
                    label.MaximumSize = new Size(width, 0);
                }
                else if (control is Panel)
                {
                    control.Width = Math.Max(80, width);
                }
                y = control.Bottom + 12;
            }

            if (txtLog.Parent == page)
            {
                int logHeight = page.ClientSize.Height - y - page.Padding.Bottom;
                txtLog.SetBounds(page.Padding.Left, y, width, Math.Max(160, logHeight));
            }
        }

        /// <summary>镜像所有文件的拖放目标：整窗任意位置都能把文件拖进来。</summary>
        private static readonly string[] DropExtensions = { ".hap", ".cer", ".pem", ".crt", ".p7b" };

        private void EnableFileDrop(Control root)
        {
            root.AllowDrop = true;
            root.DragEnter += FileDropDragEnter;
            root.DragDrop += FileDropDragDrop;
            foreach (Control child in root.Controls)
                EnableFileDrop(child);
        }

        private static void FileDropDragEnter(object? sender, DragEventArgs e)
        {
            e.Effect = e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void FileDropDragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data == null || !e.Data.GetDataPresent(DataFormats.FileDrop))
                return;
            if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
                return;

            AcceptDroppedFiles(files);
            e.Effect = DragDropEffects.Copy;
        }

        /// <summary>按扩展名把拖进来的文件分派到对应的页，类型不对就报错。</summary>
        private void AcceptDroppedFiles(string[] files)
        {
            List<string> rejected = new List<string>();

            foreach (string file in files)
            {
                string name = Path.GetFileName(file);
                string ext = Path.GetExtension(file).ToLowerInvariant();

                if (!File.Exists(file) || Array.IndexOf(DropExtensions, ext) < 0)
                {
                    rejected.Add(name);
                    continue;
                }

                if (ext == ".hap")
                {
                    try
                    {
                        LoadHap(file, keepBundle: false);
                        tabMain.SelectedIndex = 0;
                        SetStatus("已载入 HAP：" + name);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, "这个 HAP 读不了：" + ex.Message, "文件错误",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else if (ext == ".p7b")
                {
                    SetProfileFile(file);
                    tabMain.SelectedIndex = 2;
                }
                else
                {
                    SetCertFile(file);
                    tabMain.SelectedIndex = 1;
                }
            }

            if (rejected.Count > 0)
            {
                MessageBox.Show(this,
                    "下面这些文件用不上：\r\n    " + string.Join("\r\n    ", rejected) +
                    "\r\n\r\n这里只接受：\r\n" +
                    "    .hap   应用安装包\r\n" +
                    "    .cer   AGC 下载的调试证书\r\n" +
                    "    .p7b   AGC 下载的调试 Profile",
                    "文件类型不对", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>打开向导时只回填可复用的物料（密钥库、证书）。</summary>
        private void ApplySettings()
        {
            UpdateKeyStatus();
            // 证书可以一直复用，但文件被挪走/删掉就别再填了
            bool certExists = !string.IsNullOrEmpty(settings.CertFile) && File.Exists(settings.CertFile);
            txtCert.Text = certExists ? settings.CertFile : string.Empty;
            certFile = certExists ? settings.CertFile : string.Empty;

            txtProfile.Text = string.Empty;
            profileFile = string.Empty;

            // 磁盘上也不留旧值，免得下次启动又被读回来
            if (!string.IsNullOrEmpty(settings.LastHap) ||
                !string.IsNullOrEmpty(settings.LastBundle) ||
                !string.IsNullOrEmpty(settings.ProfileFile))
            {
                settings.LastHap = string.Empty;
                settings.LastBundle = string.Empty;
                settings.ProfileFile = string.Empty;
                settings.Save();
            }

            UpdateSummary();
        }

        private void UpdateKeyStatus()
        {
            bool hasKey = File.Exists(settings.KeyStoreFile);
            // 路径是一整段没有空格的长串，WinForms 的标签不会在中间断行，窗口一窄就被右边缘切掉。
            // 所以这里主动分行：状态一行、目录一行或多行、文件名一行。
            string folder = hasKey
                ? Path.GetDirectoryName(settings.KeyStoreFile) ?? string.Empty
                : SigningSettings.Folder;
            string file = hasKey ? Path.GetFileName(settings.KeyStoreFile) : string.Empty;

            lblKeyStatus.Text = hasKey
                ? "密钥库已就绪。\r\n目录：" + WrapPath(folder) + "\r\n文件名：" + file
                : "还没有密钥库。点「生成密钥与 CSR」就会创建它。\r\n存放目录：" + WrapPath(folder);
            lblKeyStatus.ForeColor = hasKey ? Color.DimGray : Color.DarkOrange;
        }

        /// <summary>长路径按目录分隔符拆行，避免被标签右边缘硬切。</summary>
        private static string WrapPath(string path)
        {
            const int MaxChars = 58;
            if (string.IsNullOrEmpty(path) || path.Length <= MaxChars)
                return path;

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            int lineStart = 0;
            for (int i = 0; i < path.Length; i++)
            {
                if (i - lineStart >= MaxChars && (path[i] == '\\' || path[i] == '/'))
                {
                    builder.Append(path, lineStart, i - lineStart + 1);
                    builder.Append("\r\n");
                    lineStart = i + 1;
                }
            }
            builder.Append(path, lineStart, path.Length - lineStart);
            return builder.ToString();
        }

        private void BtnBrowseHap_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "选择要安装的 HAP 文件",
                Filter = "HAP 文件|*.hap|所有文件|*.*",
                Multiselect = false
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                LoadHap(dialog.FileName, keepBundle: false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "读取 HAP 失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadHap(string path, bool keepBundle)
        {
            hapInfo = HapPatcher.ReadInfo(path);
            txtHap.Text = path;

            lblBundle.Text = hapInfo.BundleName;
            lblVersion.Text = hapInfo.VersionName + " (" + hapInfo.VersionCode + ")   Target API " +
                              SnapshotCapture.DecodeApi(hapInfo.TargetApiVersion);
            lblModule.Text = hapInfo.ModuleName + " / " + hapInfo.DeviceTypesText;
            lblSize.Text = hapInfo.SizeText;

            if (!keepBundle || string.IsNullOrWhiteSpace(txtNewBundle.Text))
            {
                // 同一份 HAP 再次打开时沿用上次的包名：包名一旦在 AGC 登记过就是固定的，
                // 每次重新随机一遍等于让人把登记流程再做一次。
                bool reuse = string.Equals(settings.LastHap, path, StringComparison.OrdinalIgnoreCase)
                             && !string.IsNullOrWhiteSpace(settings.LastBundle)
                             && settings.LastBundle.StartsWith(hapInfo.BundleName, StringComparison.Ordinal);
                txtNewBundle.Text = reuse ? settings.LastBundle : HapPatcher.SuggestBundleName(hapInfo.BundleName);
            }

            ValidateBundleName();
            UpdateSummary();

            settings.LastHap = path;
            settings.Save();
        }

        private void BtnRegenBundle_Click(object? sender, EventArgs e)
        {
            if (hapInfo == null)
                return;

            txtNewBundle.Text = HapPatcher.SuggestBundleName(hapInfo.BundleName);
            ValidateBundleName();
            UpdateSummary();
        }

        private void BtnCopyBundle_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNewBundle.Text))
                return;

            Clipboard.SetText(txtNewBundle.Text.Trim());
            SetStatus("新包名已复制到剪贴板，去 AGC 新建应用时粘贴即可。");
        }

        private bool ValidateBundleName()
        {
            if (lblBundleState == null || txtNewBundle == null)
                return false;

            string value = txtNewBundle.Text.Trim();
            if (value.Length == 0)
            {
                lblBundleState.Text = string.Empty;
                if (lblAppIdBundle != null)
                {
                    lblAppIdBundle.Text = "—";
                    lblAppIdBundle.ForeColor = Color.DimGray;
                }
                return false;
            }

            if (!Regex.IsMatch(value, "^[a-z][a-z0-9_]*(\\.[a-z0-9_]+)+$"))
            {
                lblBundleState.Text = "包名格式不对：只能用小写字母、数字、下划线，至少两段，段间用点分隔。";
                lblBundleState.ForeColor = Color.Firebrick;
                return false;
            }

            if (hapInfo != null && string.Equals(value, hapInfo.BundleName, StringComparison.Ordinal))
            {
                lblBundleState.Text = "这和原包名一样。原包名已被作者占用，AGC 不会给你发证书，请换个后缀。";
                lblBundleState.ForeColor = Color.Firebrick;
                return false;
            }

            lblBundleState.Text = "格式正确。这个包名需要先在 AGC 里建成你自己的应用，才能申请调试证书。";
            lblBundleState.ForeColor = Color.ForestGreen;
            if (lblAppIdBundle != null)
            {
                lblAppIdBundle.Text = value;
                lblAppIdBundle.ForeColor = Color.ForestGreen;
            }
            return true;
        }

        private async void BtnMakeKey_Click(object? sender, EventArgs e)
        {
            if (!SignTool.TryResolve(out string message))
            {
                MessageBox.Show(this, message, "缺少签名工具", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (File.Exists(settings.KeyStoreFile))
            {
                DialogResult overwrite = MessageBox.Show(this,
                    "密钥库已存在：" + settings.KeyStoreFile + "\r\n重新生成会覆盖它，之前那张证书就再也签不了名了。继续吗？",
                    "确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (overwrite != DialogResult.Yes)
                    return;
            }

            SetBusy(true);
            try
            {
                string? csr = await Task.Run(() =>
                {
                    SelfSignService.EnsureKeyStore(settings, _ => { });
                    return SelfSignService.GenerateCsr(settings, "C=CN,O=HMOSWearToolBox,OU=Developer,CN=HMOS WearToolBox Debug", _ => { });
                });

                settings.Save();
                UpdateKeyStatus();
                SetStatus("CSR 已生成：" + csr);
                MessageBox.Show(this,
                    "密钥与 CSR 已生成：\r\n" + csr + "\r\n\r\n" +
                    "下一步：在浏览器里打开 AGC → 证书管理 → 新增证书 → 上传这个 .csr 文件，下载回来的 .cer 再用第 3 步选进来。",
                    "生成成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "生成失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void BtnOpenSignDir_Click(object? sender, EventArgs e)
        {
            try
            {
                Directory.CreateDirectory(SigningSettings.Folder);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "创建目录失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string? error = ShellHelper.OpenFolder(SigningSettings.Folder);
            if (error != null)
                MessageBox.Show(this, "打开目录失败：" + error, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void BtnCopyCsr_Click(object? sender, EventArgs e)
        {
            string csr = SigningSettings.DefaultCsr;
            if (!File.Exists(csr))
            {
                MessageBox.Show(this, "还没有生成 CSR，请先点「生成密钥与 CSR」。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Clipboard.SetText(csr);
            SetStatus("CSR 路径已复制：" + csr);
        }

        private void OpenUrl(string url)
        {
            string? error = ShellHelper.OpenUrl(url);
            if (error == null) SetStatus("已打开浏览器。完成这一页的操作后回到这里继续。");
            else MessageBox.Show(this, "打开浏览器失败：" + error, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>窗口一打开就在后台读 UDID，这样 ③ 页的一致性检查能直接核对设备。</summary>
        private async void AutoReadUdid()
        {
            if (lblUdid == null || lblUdid.Text.StartsWith("读取", StringComparison.Ordinal))
                return;

            string udid = lblUdid.Text.Trim();
            if (udid.Length >= 32)
                return;

            try
            {
                string output = await Task.Run(() => HdcHelper.RunHdcCommand("shell bm get -u"));
                string read = ExtractUdid(output);
                if (read.Length < 32)
                    return;

                lblUdid.Text = read;
                lblUdid.ForeColor = Color.Black;
                InspectProfile();
            }
            catch
            {
                // 读不到就算了，用户仍可以手动点「从设备读取」。
            }
        }

        private async void BtnReadUdid_Click(object? sender, EventArgs e)
        {
            SetBusy(true);
            try
            {
                lblUdid.Text = "读取中…";
                lblUdid.ForeColor = Color.DimGray;

                string output = await Task.Run(() => HdcHelper.RunHdcCommand("shell bm get -u"));
                string udid = ExtractUdid(output);

                if (udid.Length == 0)
                {
                    lblUdid.Text = "读取失败：" + output.Trim();
                    lblUdid.ForeColor = Color.Firebrick;
                    SetStatus("读取 UDID 失败，请确认手表已连接。");
                    return;
                }

                lblUdid.Text = udid;
                lblUdid.ForeColor = Color.Black;
                SetStatus("已读取 UDID。");
            }
            catch (Exception ex)
            {
                lblUdid.Text = "读取失败：" + ex.Message;
                lblUdid.ForeColor = Color.Firebrick;
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void BtnCopyUdid_Click(object? sender, EventArgs e)
        {
            string udid = lblUdid.Text.Trim();
            if (udid.Length < 16 || udid.StartsWith("尚未", StringComparison.Ordinal) || udid.StartsWith("读取", StringComparison.Ordinal))
            {
                MessageBox.Show(this, "还没有读到 UDID，请先点「从设备读取」。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Clipboard.SetText(udid);
            SetStatus("UDID 已复制。");
        }

        private static string ExtractUdid(string output)
        {
            Match match = Regex.Match(output, "UDID\\s*[:=]\\s*([0-9A-Fa-f]{16,})");
            if (match.Success)
                return match.Groups[1].Value;

            foreach (string line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                Match inline = Regex.Match(line, "([0-9A-Fa-f]{32,})");
                if (inline.Success)
                    return inline.Groups[1].Value;
            }

            return string.Empty;
        }

        private void BtnPickCert_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "选择 AGC 下载的调试证书",
                Filter = "证书文件|*.cer;*.pem;*.crt|所有文件|*.*",
                Multiselect = false
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            SetCertFile(dialog.FileName);
        }

        private void BtnPickProfile_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog dialog = new OpenFileDialog
            {
                Title = "选择 AGC 下载的调试 Profile",
                Filter = "Profile 文件|*.p7b|所有文件|*.*",
                Multiselect = false
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            SetProfileFile(dialog.FileName);
        }

        private void SetCertFile(string path)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show(this, "找不到这个文件：" + path, "文件错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            certFile = path;
            txtCert.Text = certFile;
            settings.CertFile = certFile;
            settings.Save();
            UpdateSummary();
            SetStatus("已选择证书：" + Path.GetFileName(certFile));
        }

        /// <summary>选入 Profile（文件对话框和拖拽都走这里）。</summary>
        private void SetProfileFile(string path)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show(this, "找不到这个文件：" + path, "文件错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            profileFile = path;
            txtProfile.Text = profileFile;
            settings.ProfileFile = profileFile;
            settings.Save();

            // Profile 是「应用 + 证书 + 设备」绑在一起的结果，它里面的包名才是设备认可的包名，
            // 所以这里主动问一句要不要按它同步，省得用户来回对照。
            if (ProfileInspector.TryRead(profileFile, out SignedProfileInfo adopted) &&
                !string.IsNullOrWhiteSpace(adopted.BundleName) &&
                !string.Equals(adopted.BundleName, txtNewBundle.Text.Trim(), StringComparison.Ordinal))
            {
                DialogResult sync = MessageBox.Show(this,
                    "这个 Profile 绑定的包名是：\r\n    " + adopted.BundleName +
                    "\r\n\r\n当前填的新包名是：\r\n    " + txtNewBundle.Text.Trim() +
                    "\r\n\r\n要把新包名同步成 Profile 里的那个吗？（不同步的话，装到手表上会失败）",
                    "按 Profile 同步包名",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                if (sync == DialogResult.Yes)
                {
                    txtNewBundle.Text = adopted.BundleName;
                    SetStatus("已按 Profile 同步新包名：" + adopted.BundleName);
                }
            }

            InspectProfile();
            UpdateSummary();
        }

        private void InspectProfile()
        {
            if (!ProfileInspector.TryRead(profileFile, out SignedProfileInfo info))
            {
                lblProfileInfo.Text = "无法解析这个 Profile（可能不是 AGC 下载的 .p7b）。签名时仍会原样交给签名工具。";
                lblProfileInfo.ForeColor = Color.DarkOrange;
                return;
            }

            string bundle = txtNewBundle.Text.Trim();
            bool matched = string.Equals(info.BundleName, bundle, StringComparison.Ordinal);

            List<string> lines = new List<string>
            {
                "绑定的包名：" + info.BundleName,
                "类型：" + (info.Type.Length == 0 ? "未知" : info.Type) +
                "    版本：" + (info.VersionName.Length == 0 ? "未知" : info.VersionName),
                "包含设备：" + (info.DeviceIds.Count == 0 ? "未绑定设备" : string.Join(", ", info.DeviceIds))
            };

            if (info.Type.Length > 0 && !string.Equals(info.Type, "debug", StringComparison.OrdinalIgnoreCase))
                lines.Add("注意：这个 Profile 不是 debug 类型，装到个人设备上大概率会被拒。");

            bool deviceOk = true;
            string udid = lblUdid == null ? string.Empty : lblUdid.Text.Trim();
            bool udidKnown = udid.Length >= 32 &&
                             !udid.StartsWith("尚未", StringComparison.Ordinal) &&
                             !udid.StartsWith("读取", StringComparison.Ordinal);

            if (info.DeviceIds.Count == 0)
            {
                lines.Add("注意：这个 Profile 没有绑定任何设备，debug 类型一般要求绑定设备。");
            }
            else if (!udidKnown)
            {
                lines.Add("想核对设备，先到「② 调试证书」点「从设备读取」拿到 UDID，再回来看这里。");
            }
            else
            {
                bool contains = false;
                foreach (string id in info.DeviceIds)
                {
                    if (string.Equals(id, udid, StringComparison.OrdinalIgnoreCase))
                    {
                        contains = true;
                        break;
                    }
                }

                deviceOk = contains;
                lines.Add(contains
                    ? "✓ Profile 的设备列表里有这块手表。"
                    : "✗ Profile 的设备列表里没有这块手表，安装会被拒。请回 AGC 把设备加进去，再重新下载 Profile。");
            }

            lines.Add(matched ? "✓ 包名一致，可以继续。" : "✗ 包名不一致：Profile 绑定的是 " + info.BundleName + "，当前填的是 " + bundle + "。");

            lblProfileInfo.Text = string.Join("\r\n", lines);
            lblProfileInfo.ForeColor = matched && deviceOk ? Color.ForestGreen : Color.Firebrick;
        }

        private void UpdateSummary()
        {
            if (lblSummary == null)
                return;

            List<string> lines = new List<string>();
            lines.Add("源文件：" + (hapInfo == null ? "未选择" : Path.GetFileName(hapInfo.FilePath)));
            lines.Add("新包名：" + (string.IsNullOrWhiteSpace(txtNewBundle?.Text) ? "未生成" : txtNewBundle!.Text.Trim()));
            lines.Add("证书：" + (certFile.Length == 0 ? "未选择" : Path.GetFileName(certFile)));
            lines.Add("Profile：" + (profileFile.Length == 0 ? "未选择" : Path.GetFileName(profileFile)));
            lblSummary.Text = string.Join("\r\n", lines);
        }

        private async Task StartAsync()
        {
            if (busy)
                return;

            if (hapInfo == null || !File.Exists(hapInfo.FilePath))
            {
                MessageBox.Show(this, "请先在「① 选择应用」里选一个 HAP 文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!ValidateBundleName())
            {
                MessageBox.Show(this, "新包名不合法，请先修正。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists(settings.KeyStoreFile))
            {
                MessageBox.Show(this, "还没有密钥库，请先在「② 调试证书」里生成。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists(certFile))
            {
                MessageBox.Show(this, "请先选择 AGC 下载的证书（.cer）。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists(profileFile))
            {
                MessageBox.Show(this, "请先选择 AGC 下载的 Profile（.p7b）。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (ProfileInspector.TryRead(profileFile, out SignedProfileInfo info) &&
                !string.Equals(info.BundleName, txtNewBundle.Text.Trim(), StringComparison.Ordinal))
            {
                DialogResult go = MessageBox.Show(this,
                    "Profile 绑定的包名是 " + info.BundleName + "，与当前新包名 " + txtNewBundle.Text.Trim() + " 不一致。\r\n" +
                    "这样签名会失败或装上后无法启动。仍然继续吗？",
                    "包名不一致", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
                if (go != DialogResult.Yes)
                    return;
            }

            if (!SignTool.TryResolve(out string toolMessage))
            {
                MessageBox.Show(this, toolMessage, "缺少签名工具", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string newBundle = txtNewBundle.Text.Trim();
            SetBusy(true);
            txtLog.Clear();
            progress.Style = ProgressBarStyle.Marquee;
            progress.MarqueeAnimationSpeed = 30;

            try
            {
                string workDir = SelfSignService.CreateWorkDir();
                AppendLog("工作目录：" + workDir);

                string signed = await Task.Run(() =>
                {
                    AppendLog("→ 重打包（换包名 + 补 wearable）");
                    string patched = SelfSignService.Repack(hapInfo, newBundle, workDir, AppendLog);

                    // 改完立刻体检：只自动改文本与「整条等于旧包名」的字节码字符串，
                    // 剩下改不动的地方（.so、拼出来的包名）在这里按严重程度列出来。
                    AppendLog("→ 体检（检查改名有没有改干净）");
                    List<CheckFinding> findings = HapCheck.Inspect(patched, hapInfo.BundleName, newBundle);
                    AppendLog(HapCheck.Format(findings));
                    if (HapCheck.HasFatal(findings))
                        throw new InvalidOperationException("体检没通过：上面标红的地方必须先解决，已经停止签名。");

                    AppendLog("→ 签名");
                    string signedHap = SelfSignService.Sign(patched, newBundle, certFile, profileFile, settings, workDir, AppendLog);

                    AppendLog("→ 校验签名");
                    SelfSignService.Verify(signedHap, workDir, AppendLog);
                    return signedHap;
                });

                AppendLog("→ 推送到手表并安装");
                string installResult = await InstallWithRecoveryAsync(signed, newBundle);
                if (installResult.IndexOf("success", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException("安装失败：" + installResult + SelfSignService.HintFor(installResult));

                AppendLog("安装成功。");

                // 设备上读不到 HAP，也就读不到真正的应用名；用安装包文件名当显示名，
                // 记在 renames.json 里，刷新列表、生成快照都会显示这个名字。
                string displayName = RenameStore.NameFromHapFile(hapInfo.FilePath);
                RenameStore.Remember(newBundle, displayName, hapInfo.BundleName, Path.GetFileName(hapInfo.FilePath));
                AppendLog("显示名记为「" + (string.IsNullOrWhiteSpace(displayName) ? newBundle : displayName) +
                          "」（原包名 " + hapInfo.BundleName + "）。");

                SetStatus("安装完成。可以在手表桌面上找到「" + newBundle + "」，或者点「软件管理」刷新列表。");

                if (MessageBox.Show(this,
                        "安装成功。要现在在手表上打开看看吗？",
                        "安装完成", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes)
                {
                    string ability = hapInfo.MainAbility;
                    AppendLog("→ 在手表上启动 " + (string.IsNullOrWhiteSpace(ability) ? "EntryAbility" : ability));
                    string started;
                    try
                    {
                        started = await Task.Run(() => SelfSignService.StartApp(newBundle, ability, AppendLog));
                    }
                    catch (Exception ex)
                    {
                        AppendLog("启动失败：" + ex.Message);
                        started = string.Empty;
                    }

                    if (started.IndexOf("successfully", StringComparison.OrdinalIgnoreCase) >= 0)
                        SetStatus("已安装并在手表上启动。");
                    else
                        SetStatus("已安装。启动命令没有返回成功，请到手表桌面上手动打开。");
                }
            }
            catch (Exception ex)
            {
                AppendLog("失败：" + ex.Message);
                SetStatus("安装失败：" + ex.Message);
                MessageBox.Show(this, ex.Message, "失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                progress.MarqueeAnimationSpeed = 0;
                progress.Style = ProgressBarStyle.Blocks;
                SetBusy(false);
            }
        }

        /// <summary>
        /// 安装，遇到 9568263（同名应用版本更高）时问用户要不要卸掉旧的再装。
        /// </summary>
        private async Task<string> InstallWithRecoveryAsync(string signedHap, string bundleName)
        {
            string result = await Task.Run(() => SelfSignService.Install(signedHap, AppendLog));
            if (result.IndexOf("9568263", StringComparison.Ordinal) < 0)
                return result;

            DialogResult answer = MessageBox.Show(this,
                "手表上已经装了同一个包名、但版本号更高的应用（错误码 9568263）。\r\n" +
                "要先把旧的卸掉，再装这一份吗？",
                "版本降级", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (answer != DialogResult.Yes)
                return result;

            string retried = result;
            await Task.Run(() =>
            {
                AppendLog("→ 卸载旧版本 " + bundleName);
                SelfSignService.Uninstall(bundleName, AppendLog);
                AppendLog("→ 重新安装");
                retried = SelfSignService.Install(signedHap, AppendLog);
            });
            return retried;
        }

        private void AppendLog(string line)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AppendLog), line);
                return;
            }

            txtLog.AppendText("[" + DateTime.Now.ToString("HH:mm:ss") + "] " + line + "\r\n");
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.ScrollToCaret();
        }

        private void SetBusy(bool value)
        {
            busy = value;
            Cursor = value ? Cursors.WaitCursor : Cursors.Default;
            btnStart.Enabled = !value;
            tabMain.Enabled = !value;
        }

        private void SetStatus(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(SetStatus), text);
                return;
            }
            lblStatus.Text = text;
        }

        private static Label Caption(int x, int y, string text) => new Label
        {
            Text = text,
            AutoSize = true,
            Location = new Point(x, y)
        };

        private static Label Value(int x, int y, int width) => new Label
        {
            Text = "—",
            Location = new Point(x, y),
            Size = new Size(width, 24),
            AutoEllipsis = true
        };

        private static TextBox Box(int x, int y, int width, int height, bool readOnly)
        {
            TextBox box = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(width, height),
                ReadOnly = readOnly
            };
            return box;
        }

        private static GroupBox Group(string text, int x, int y, int width, int height) => new GroupBox
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(width, height),
            // 左/右留白给正文，上边距要让开 GroupBox 自己的标题
            Padding = new Padding(18, 32, 18, 12)
        };

        private static Button ButtonAt(string text, int x, int y, int width, int height, EventHandler handler)
        {
            Button button = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height)
            };
            button.Click += handler;
            return button;
        }
    }
}
