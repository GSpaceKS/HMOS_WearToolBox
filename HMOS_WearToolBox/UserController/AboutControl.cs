using System;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using HMOS_WearToolBox.Helper;

namespace HMOS_WearToolBox.UserController
{
    public partial class AboutControl : UserControl
    {
        private Label lblVersionInline = null!;
        private Button btnCheckUpdate = null!;
        private GroupBox groupVersion = null!;
        private CheckBox chkIncludeBeta = null!;
        private FlowLayoutPanel rowVersionTop = null!;
        private FlowLayoutPanel rowVersionToken = null!;
        private TextBox txtToken = null!;
        private Label lblTokenState = null!;
        private Button btnSaveToken = null!;
        private ToolTip tipToken = null!;
        private bool layingOutVersion;

        public AboutControl()
        {
            InitializeComponent();
            if (!DesignMode)
            {
                BuildVersionRow();
                LoadVersion();
                SetupLinks();
                FitTitle();
                LayoutVersionGroup();
                Resize += (s, e) => { FitTitle(); LayoutVersionGroup(); };
            }
        }

        /// <summary>整页被 UiScaler 缩放时会重新布局，这时候再量一次标题宽度。</summary>
        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);

            if (DesignMode)
                return;

            if (labelSoftwareNameenUS != null)
                FitTitle();

            LayoutVersionGroup();
        }

        /// <summary>
        /// 窗口变窄时把大标题的字号降下来，避免「HMOS WareToolBox」被右边缘截断。
        /// </summary>
        private void FitTitle()
        {
            if (labelSoftwareNameenUS == null)
                return;

            // 整页会被 UiScaler 等比缩放，设计宽度不等于可见宽度：
            // 用「主窗口客户区宽度 × 当前缩放系数」当作真正能用的宽度。
            int visualWidth = TopLevelControl != null
                ? (int)Math.Round(TopLevelControl.ClientSize.Width * UiScaler.FactorOf(this))
                : ClientSize.Width;
            int available = Math.Max(120, visualWidth - 30);

            // 每次都从设计字号（24pt × 当前缩放）重新算，避免在已缩小的字号上反复递减导致越缩越小。
            float baseSize = 24F * UiScaler.FactorOf(this);
            if (baseSize < 11F)
                baseSize = 11F;

            float size = baseSize;
            while (size > 11F)
            {
                using (Font probe = new Font(labelSoftwareNameenUS.Font.FontFamily, size, FontStyle.Bold))
                {
                    if (TextRenderer.MeasureText(labelSoftwareNameenUS.Text, probe).Width <= available)
                        break;
                }

                size -= 1F;
            }

            if (Math.Abs(labelSoftwareNameenUS.Font.Size - size) > 0.05F)
                labelSoftwareNameenUS.Font = new Font(labelSoftwareNameenUS.Font.FontFamily, size, FontStyle.Bold);
        }

        /// <summary>
        /// 在「鸿蒙手表工具箱」下面加一行：版本号 + 检查更新按钮。
        /// 原来版本号挤在页面最底部，窗口一小就被压到看不见了，挪到标题正下方更直观。
        /// </summary>
        private void BuildVersionRow()
        {
            // 底部那行旧的版本号去掉，避免两处重复
            tableLayoutPanelButtom.Controls.Remove(tableLayoutPanelSoftwareVersion);
            tableLayoutPanelButtom.SetCellPosition(tableLayoutPanelLink, new TableLayoutPanelCellPosition(0, 0));
            tableLayoutPanelButtom.SetCellPosition(labelName, new TableLayoutPanelCellPosition(0, 1));
            tableLayoutPanelButtom.RowCount = 2;
            tableLayoutPanelButtom.RowStyles.Clear();
            tableLayoutPanelButtom.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
            tableLayoutPanelButtom.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));

            tableLayoutPanelTop.RowCount = 3;
            tableLayoutPanelTop.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // 用 GroupBox 把版本号、检查更新、beta 开关、接口令牌圈在一起，标题就是「版本」
            groupVersion = new GroupBox
            {
                Text = "版本",
                AutoSize = false,
                Height = 156,
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 26, 14, 10),
                Margin = new Padding(24, 12, 24, 12)
            };

            // 一行放版本号 + 按钮 + 开关，一行放令牌，最后一行说明；
            // 具体坐标由 LayoutVersionGroup() 按当前宽度算，窗口变窄会自动换行而不是被切掉。
            rowVersionTop = new FlowLayoutPanel
            {
                AutoSize = false,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            lblVersionInline = new Label
            {
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
                ForeColor = Color.DimGray,
                Margin = new Padding(6, 10, 18, 6),
                Text = "版本 " + UpdateChecker.CurrentVersion
            };

            btnCheckUpdate = new Button
            {
                Text = "检查更新",
                AutoSize = false,
                Size = new Size(112, 34),
                Margin = new Padding(4, 4, 4, 4),
                UseVisualStyleBackColor = true
            };
            btnCheckUpdate.Click += BtnCheckUpdate_Click;

            chkIncludeBeta = new CheckBox
            {
                Text = "接收 beta 版更新",
                AutoSize = true,
                Checked = UpdateChecker.IncludeBeta,
                Margin = new Padding(16, 10, 6, 6)
            };
            chkIncludeBeta.CheckedChanged += (s, e) => UpdateChecker.IncludeBeta = chkIncludeBeta.Checked;

            rowVersionTop.Controls.Add(lblVersionInline);
            rowVersionTop.Controls.Add(btnCheckUpdate);
            rowVersionTop.Controls.Add(chkIncludeBeta);

            // 第二行：GitHub 接口令牌。匿名每小时只有 60 次，反复点「检查更新」很容易被限流。
            rowVersionToken = new FlowLayoutPanel
            {
                AutoSize = false,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };

            Label lblToken = new Label
            {
                Text = "接口令牌",
                AutoSize = true,
                Margin = new Padding(6, 11, 8, 6)
            };

            txtToken = new TextBox
            {
                AutoSize = false,
                Height = 30,
                UseSystemPasswordChar = true,
                Text = UpdateChecker.Token,
                Margin = new Padding(0, 7, 8, 6)
            };

            btnSaveToken = new Button
            {
                Text = "保存",
                AutoSize = false,
                Size = new Size(76, 32),
                Margin = new Padding(0, 6, 8, 6),
                UseVisualStyleBackColor = true
            };

            Button btnOpenTokenPage = new Button
            {
                Text = "去 GitHub 生成",
                AutoSize = false,
                Size = new Size(140, 32),
                Margin = new Padding(0, 6, 8, 6),
                UseVisualStyleBackColor = true
            };
            btnOpenTokenPage.Click += (s, e) => OpenUrl("https://github.com/settings/tokens");

            lblTokenState = new Label
            {
                AutoSize = true,
                Margin = new Padding(10, 12, 4, 6),
            };
            UpdateTokenState();

            Label lblTokenInfo = new Label
            {
                Text = "\u24D8",
                AutoSize = true,
                ForeColor = Color.Black,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 9, 6, 6),
            };

            tipToken = new ToolTip
            {
                InitialDelay = 300,
                AutoPopDelay = 20000,
                ReshowDelay = 100,
            };
            tipToken.SetToolTip(lblTokenInfo,
                "填一个 GitHub 个人访问令牌，接口限额就从 60 次/小时提到 5000 次/小时。" + "\r\n" +
                "只读公开仓库的版本号：classic 令牌不勾任何权限即可，Fine-grained 令牌也不需要任何权限。" + "\r\n" +
                "点「保存」时会先向 GitHub 校验一次，能通过才保存。");
            tipToken.SetToolTip(lblTokenState, "点「保存」可以重新校验一次。");

            btnSaveToken.Click += async (s, e) => await SaveTokenAsync();

            rowVersionToken.Controls.Add(lblToken);
            rowVersionToken.Controls.Add(txtToken);
            rowVersionToken.Controls.Add(btnSaveToken);
            rowVersionToken.Controls.Add(btnOpenTokenPage);
            rowVersionToken.Controls.Add(lblTokenState);
            rowVersionToken.Controls.Add(lblTokenInfo);

            groupVersion.Controls.Add(rowVersionTop);
            groupVersion.Controls.Add(rowVersionToken);
            tableLayoutPanelTop.Controls.Add(groupVersion, 0, 2);

            LayoutVersionGroup();
        }

        /// <summary>
        /// 「版本」分组里的自适应排版：按当前可用宽度决定每行有多高（放不下就换行），
        /// 再据此调整 GroupBox 的高度，避免窄窗口下右侧被裁掉。
        /// </summary>
        private void LayoutVersionGroup()
        {
            if (layingOutVersion || groupVersion == null || rowVersionTop == null)
                return;

            int width = groupVersion.ClientSize.Width - groupVersion.Padding.Horizontal;
            if (width < 120)
                return;

            layingOutVersion = true;
            try
            {
                int left = groupVersion.Padding.Left;
                int y = groupVersion.Padding.Top;

                // 令牌框吃剩余宽度：先按其余控件的实际宽度算，保证「未配置/已配置」和它后面的 ⓘ 不会被挤到下一行。
                if (txtToken != null && rowVersionToken != null)
                {
                    int fixedWidth = 0;
                    foreach (Control child in rowVersionToken.Controls)
                    {
                        if (ReferenceEquals(child, txtToken))
                            continue;

                        fixedWidth += child.PreferredSize.Width + child.Margin.Horizontal;
                    }

                    int spare = width - fixedWidth - txtToken.Margin.Horizontal - 4;
                    int tokenWidth = Math.Max(100, Math.Min(320, spare));
                    if (txtToken.Width != tokenWidth)
                        txtToken.Width = tokenWidth;
                }

                foreach (FlowLayoutPanel? panel in new[] { rowVersionTop, rowVersionToken })
                {
                    if (panel == null) continue;

                    Size preferred = panel.GetPreferredSize(new Size(width, 0));
                    int height = Math.Max(40, preferred.Height);
                    panel.SetBounds(left, y, width, height);
                    y = panel.Bottom + 2;
                }

                y = y + groupVersion.Padding.Bottom;

                if (Math.Abs(groupVersion.Height - y) > 1)
                    groupVersion.Height = y;
            }
            finally
            {
                layingOutVersion = false;
            }
        }

        /// <summary>
        /// 从程序集加载版本号（优先使用 InformationalVersion）
        /// </summary>
        private void UpdateTokenState()
        {
            bool configured = UpdateChecker.Token.Length > 0;
            lblTokenState.Text = configured ? "已配置" : "未配置";
            lblTokenState.ForeColor = configured ? Color.ForestGreen : Color.Firebrick;
        }

        private async Task SaveTokenAsync()
        {
            string candidate = txtToken.Text.Trim();

            if (candidate.Length == 0)
            {
                if (UpdateChecker.Token.Length == 0)
                {
                    MessageBox.Show(this, "还没有填令牌。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    UpdateTokenState();
                    return;
                }

                DialogResult clear = MessageBox.Show(this,
                    "要清除已经保存的令牌吗？清除后就回到匿名访问（60 次/小时）。",
                    "清除令牌",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (clear != DialogResult.Yes)
                {
                    UpdateTokenState();
                    return;
                }

                UpdateChecker.Token = string.Empty;
                UpdateTokenState();
                MessageBox.Show(this, "令牌已清除，接口限额回到 60 次/小时。", "已清除", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            btnSaveToken.Enabled = false;
            lblTokenState.Text = "校验中…";
            lblTokenState.ForeColor = Color.DimGray;
            try
            {
                UpdateChecker.TokenCheck check = await UpdateChecker.VerifyTokenAsync(candidate);

                if (check.Valid)
                {
                    UpdateChecker.Token = candidate;
                    UpdateTokenState();
                    MessageBox.Show(this, check.Message + "\r\n\r\n已经保存。", "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                UpdateTokenState();

                if (check.Succeeded)
                {
                    MessageBox.Show(this, check.Message, "令牌不可用", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                DialogResult anyway = MessageBox.Show(this,
                    check.Message + "\r\n\r\n要不要先把令牌存下来？（存下来也没法确认它有效）",
                    "没法验证",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);
                if (anyway == DialogResult.Yes)
                {
                    UpdateChecker.Token = candidate;
                    UpdateTokenState();
                }
            }
            finally
            {
                btnSaveToken.Enabled = true;
            }
        }

        private void LoadVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            // 获取程序集信息版本（可包含任意字符串，如 "1.0.0-Alpha"）
            var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrEmpty(informationalVersion))
            {
                labelSoftwareVersionValue.Text = informationalVersion;
            }
            else
            {
                // 回退到普通版本号
                var version = assembly.GetName().Version;
                labelSoftwareVersionValue.Text = version?.ToString(3) ?? "1.0.0";
            }

            lblVersionInline.Text = "版本 " + UpdateChecker.CurrentVersion;
        }

        /// <summary>检查更新：读 GitHub 上最新的 Release，比当前版本新就引导去下载。</summary>
        private async void BtnCheckUpdate_Click(object? sender, EventArgs e)
        {
            btnCheckUpdate.Enabled = false;
            string originalText = btnCheckUpdate.Text;
            btnCheckUpdate.Text = "检查中…";

            try
            {
                UpdateChecker.Result result = await UpdateChecker.CheckAsync();

                if (!result.Succeeded)
                {
                    DialogResult openAnyway = MessageBox.Show(
                        this,
                        result.Message + "\r\n\r\n要打开 Releases 页面自己看看吗？",
                        "检查更新",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2);

                    if (openAnyway == DialogResult.Yes)
                        OpenUrl(UpdateChecker.ReleasesUrl);

                    return;
                }

                if (!result.HasUpdate)
                {
                    MessageBox.Show(this, result.Message, "检查更新", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                DialogResult open = MessageBox.Show(
                    this,
                    result.Message + "\r\n\r\n要打开下载页面吗？",
                    "发现新版本",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1);

                if (open == DialogResult.Yes)
                    OpenUrl(result.ReleaseUrl);
            }
            finally
            {
                btnCheckUpdate.Text = originalText;
                btnCheckUpdate.Enabled = true;
            }
        }

        private void SetupLinks()
        {
            linkLabelGitHub.LinkClicked += (s, e) => OpenUrl("https://github.com/GSpaceKS/HMOS_WearToolBox");
            linkLabelAgreement.LinkClicked += (s, e) => OpenUrl("https://github.com/GSpaceKS/HMOS_WearToolBox?tab=MIT-1-ov-file");
            linkLabelIssue.LinkClicked += (s, e) => OpenUrl("https://github.com/GSpaceKS/HMOS_WearToolBox/issues");
        }

        private void OpenUrl(string url)
        {
            string? error = ShellHelper.OpenUrl(url);
            if (error != null)
                MessageBox.Show($"无法打开链接：{error}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
