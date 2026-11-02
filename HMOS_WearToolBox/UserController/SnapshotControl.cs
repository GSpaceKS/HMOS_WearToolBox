#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using HMOS_WearToolBox.Helper;
using HMOS_WearToolBox.Models;

namespace HMOS_WearToolBox.UserController
{
    /// <summary>应用快照页：保存应用信息快照，并与新快照或当前设备比对。</summary>
    public class SnapshotControl : UserControl
    {
        private static readonly Color ColorAdded = Color.FromArgb(0, 130, 70);
        private static readonly Color ColorRemoved = Color.FromArgb(190, 40, 40);
        private static readonly Color ColorModified = Color.FromArgb(196, 100, 0);
        private static readonly Color ColorUnchanged = Color.FromArgb(110, 110, 110);

        private TableLayoutPanel toolbar;
        private FlowLayoutPanel flowFilter;
        private ToolTip tipRow;
        private string lastTipKey;
        private ComboBox cmbBaseline;
        private ComboBox cmbCurrent;
        private Label lblBaselineCaption;
        private Label lblCurrentCaption;
        private Button btnCapture;
        private Button btnExport;
        private Button btnManage;
        private TextBox txtSearch;
        private CheckBox chkOnlyChanged;
        // 「只看」的开关 + 它右侧选择看系统还是第三方
        private CheckBox chkOnlyKind;
        private ComboBox cmbKind;
        private ListView listViewDiff;
        private ImageList legendImages;
        private Panel panelStatus;
        private Label lblSummary;
        private Label lblProgress;
        private ProgressBar progressBar;

        private List<AppSnapshot> snapshots = new List<AppSnapshot>();
        private List<AppDiff> diffs = new List<AppDiff>();
        private bool busy;

        /// <summary>上一次检查到的设备在线状态。没有设备时不能生成快照，也不让导出。</summary>
        private bool deviceOnline;
        private bool deviceCheckRunning;

        /// <summary>用户手动勾/去过「只看变化」之后，就按用户的意思来，不再自动改。</summary>
        private bool onlyChangedTouched;
        private bool suppressFilterEvent;
        private string lastPairKey;

        /// <summary>下拉框第一项（空白项）的文案。选它表示这一侧不参与比对，列表只展示单份快照的全部应用。</summary>
        private const string NoneItemText = "";
        private bool suppressComboEvent;
        /// <summary>当前是否处于「两份快照对比」状态；false 表示只是在浏览单份快照。</summary>
        private bool comparingNow;

        public SnapshotControl()
        {
            BuildUi();
            RefreshSnapshotList();

            // 每次切回这一页都重新看一次设备：手表可能在别处刚连上或刚断开
            VisibleChanged += (s, e) =>
            {
                if (Visible)
                {
                    _ = RefreshDeviceStateAsync();
                }
                else
                {
                    // 切到别的页时列表已经看不见了，悬停提示必须跟着收起来
                    HideRowTip();
                }
            };
        }

        #region 界面搭建

        /// <summary>列里空间有限，去掉版本号后面的 (versionCode)，完整内容走 tooltip。</summary>
        private static string CompactChange(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return System.Text.RegularExpressions.Regex.Replace(text, @"\s*\(\d+\)", string.Empty);
        }

        /// <summary>窄到状态列放不下文字时只显示颜色圆点（状态本身在「变化」列和详情页里都有）。</summary>
        private bool CompactStatus => UiScaler.FactorOf(this) < 0.85f;

        private bool lastCompact;

        private void LayoutColumns()
        {
            if (listViewDiff == null || listViewDiff.Columns.Count < 6) return;
            int w = listViewDiff.ClientSize.Width;
            if (w <= 240) return;
            // 页面上整页等比缩放时（UiScaler），这里的像素下限也要跟着缩，否则 0.7 倍时
            // 版本+变化两列会把软件名/包名挤成「腕上…」这种什么都看不出来的宽度。
            float k = UiScaler.FactorOf(this);
            // 缩到 0.85 以下时用户得靠颜色圆点认状态：44px 里塞不下「无变化」三个字，硬塞只会变成「无…」，
            // 所以窄的时候干脆只留圆点（双击进详情照样能看到完整状态），把宽度让给软件名/包名。
            // 「无变化」三个字 + 14px 圆点 + 左右留白，这个宽度是量出来的，不是拍脑袋：
            // 之前写死 52/62 在 1220 宽的窗口里都显示成「无…」，因为字宽随 DPI 变。
            int statusFit = TextRenderer.MeasureText("无变化", listViewDiff.Font).Width + 26;
            int status = CompactStatus
                ? Math.Max(24, (int)Math.Round(30 * k))
                : Math.Max(statusFit, (int)Math.Round(62 * k));
            int target = Math.Max(36, (int)Math.Round(74 * k));
            int rest = w - status - target - 2;
            int ver = Math.Max((int)Math.Round(180 * k), (int)(rest * 0.25));
            int chg = Math.Max((int)Math.Round(228 * k), (int)(rest * 0.32));
            int left = rest - ver - chg;
            if (left < (int)Math.Round(120 * k)) { left = (int)Math.Round(120 * k); chg = Math.Max(60, rest - ver - left); }
            int name = (int)(left * 0.40);
            int pkg = left - name;
            listViewDiff.Columns[0].Text = CompactStatus ? string.Empty : "状态";
            listViewDiff.Columns[0].Width = status;
            listViewDiff.Columns[1].Width = name;
            listViewDiff.Columns[2].Width = pkg;
            listViewDiff.Columns[3].Width = ver;
            listViewDiff.Columns[4].Width = chg;
            listViewDiff.Columns[5].Width = target;
            if (lastCompact != CompactStatus)
            {
                lastCompact = CompactStatus;
                ApplyFilter();
            }
        }

        private void BuildUi()
        {
            Dock = DockStyle.Fill;

            var panelMain = new Panel { Dock = DockStyle.Fill };
            Controls.Add(panelMain);

            panelStatus = new Panel { Dock = DockStyle.Bottom, Height = 70, Padding = new Padding(12, 4, 12, 4), BackColor = SystemColors.Control };
            lblSummary = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                Font = new Font("Microsoft YaHei UI", 9.5F),
                Text = "尚未载入快照",
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            var progressRow = new Panel { Dock = DockStyle.Fill };
            progressBar = new ProgressBar { Dock = DockStyle.Left, Width = 240, Style = ProgressBarStyle.Marquee, MarqueeAnimationSpeed = 0, Visible = false };
            lblProgress = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 9F),
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            progressRow.Controls.Add(lblProgress);
            progressRow.Controls.Add(progressBar);
            panelStatus.Controls.Add(progressRow);
            panelStatus.Controls.Add(lblSummary);

            listViewDiff = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false,
                HideSelection = false,
                UseCompatibleStateImageBehavior = false,
                Font = new Font("Microsoft YaHei UI", 9F),
                SmallImageList = BuildLegendImages()
            };
            listViewDiff.Columns.Add("状态", 62, HorizontalAlignment.Center);
            listViewDiff.Columns.Add("软件名", 132, HorizontalAlignment.Left);
            listViewDiff.Columns.Add("包名", 186, HorizontalAlignment.Left);
            listViewDiff.Columns.Add("版本", 184, HorizontalAlignment.Left);
            listViewDiff.Columns.Add("变化", 246, HorizontalAlignment.Left);
            listViewDiff.Columns.Add("Target", 66, HorizontalAlignment.Center);
            listViewDiff.DoubleClick += ListViewDiff_DoubleClick;
            listViewDiff.MouseMove += ListViewDiff_MouseMove;
            listViewDiff.MouseLeave += (o, e) => HideRowTip();
            listViewDiff.MouseWheel += (o, e) => HideRowTip();
            listViewDiff.Resize += (o, e) => LayoutColumns();
            listViewDiff.SelectedIndexChanged += (o, e) => HideRowTip();
            panelMain.Controls.Add(listViewDiff);
            panelMain.Controls.Add(panelStatus);
            LayoutColumns();

            var menuRow = new ContextMenuStrip();
            menuRow.Items.Add("查看详情", null, (o, e) => OpenDetail());
            menuRow.Items.Add("复制包名", null, (o, e) => CopySelectedBundle());
            listViewDiff.ContextMenuStrip = menuRow;

            // 第二行：选择要比对的两份快照 + 过滤条件
            flowFilter = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(4, 2, 4, 4),
                // 允许换行：整页缩到 0.7 倍时这一行原本会被页面右边缘切掉「只看 + 类型」
                WrapContents = true
            };

            lblBaselineCaption = MakeCaption("基准:");
            cmbBaseline = MakeCombo();
            cmbBaseline.SelectedIndexChanged += (s, e) => { if (!suppressComboEvent) RefreshDiff(); };

            lblCurrentCaption = MakeCaption("对照:");
            cmbCurrent = MakeCombo();
            cmbCurrent.SelectedIndexChanged += (s, e) => { if (!suppressComboEvent) RefreshDiff(); };

            chkOnlyChanged = new CheckBox { Text = "只看变化", AutoSize = true, Font = new Font("Microsoft YaHei UI", 9F), Padding = new Padding(8, 9, 0, 0) };
            chkOnlyChanged.CheckedChanged += (s, e) =>
            {
                if (suppressFilterEvent) return;
                onlyChangedTouched = true;
                ApplyFilter();
            };

            chkOnlyKind = new CheckBox { Text = "只看", AutoSize = true, Font = new Font("Microsoft YaHei UI", 9F), Padding = new Padding(6, 9, 0, 0) };
            chkOnlyKind.CheckedChanged += (s, e) =>
            {
                // 没勾「只看」的时候类型选择不起作用，连下拉都禁掉，免得让人以为选了就生效；
                // 勾选框本身还能点（否则永远打不开），但文字也跟着变灰，和右边的禁用下拉保持一致
                cmbKind.Enabled = chkOnlyKind.Checked;
                chkOnlyKind.ForeColor = chkOnlyKind.Checked ? SystemColors.ControlText : SystemColors.GrayText;
                ApplyFilter();
            };

            // 勾上「只看」之后按这里选的类型过滤；不勾时这个选择不起作用。
            // 走和「基准/对照」同一个工厂方法，字体才会跟旁边一致（手写 new ComboBox 会大一截）
            cmbKind = MakeCombo();
            cmbKind.Width = 96;
            cmbKind.Margin = new Padding(0, 6, 3, 6);
            cmbKind.Items.Add("系统");
            cmbKind.Items.Add("第三方");
            cmbKind.SelectedIndex = 1;
            // ComboBox 画的 CJK 比旁边的 Label 显大一圈，单独降半号让两者观感一致
            cmbKind.Font = new Font("Microsoft YaHei UI", 8F);
            cmbKind.Enabled = false;
            chkOnlyKind.ForeColor = SystemColors.GrayText;
            cmbKind.SelectedIndexChanged += (s, e) => ApplyFilter();

            flowFilter.Controls.Add(lblBaselineCaption);
            flowFilter.Controls.Add(cmbBaseline);
            flowFilter.Controls.Add(lblCurrentCaption);
            flowFilter.Controls.Add(cmbCurrent);
            flowFilter.Controls.Add(chkOnlyChanged);
            flowFilter.Controls.Add(chkOnlyKind);
            flowFilter.Controls.Add(cmbKind);
            panelMain.Controls.Add(flowFilter);

            // 第一行：操作按钮 + 搜索。用 TableLayoutPanel 而不是 FlowLayoutPanel：
            // 三个按钮走 AutoSize 列，搜索框吃掉剩下的 100% 列，窗口拉宽时自动变长。
            toolbar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(4, 4, 8, 2),
                ColumnCount = 4,
                RowCount = 1
            };
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            toolbar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            btnCapture = UiFactory.MakeButton("生成快照");
            btnCapture.Click += BtnCapture_Click;

            btnExport = UiFactory.MakeButton("导出报告");
            btnExport.Click += BtnExport_Click;

            // 先按「没连设备」画，等检测回来再放开，避免刚打开时闪一下可点状态
            btnCapture.Enabled = false;

            btnManage = UiFactory.MakeButton("管理快照");
            btnManage.Click += BtnManage_Click;

            txtSearch = new TextBox
            {
                Font = new Font("Microsoft YaHei UI", 9F),
                // 高度由单行文本框自己定，Anchor Left|Right 只接管宽度；上边距用来跟 34px 高的按钮居中对齐
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(12, 5, 0, 5)
            };
            txtSearch.PlaceholderText = "搜索名称或包名";
            txtSearch.TextChanged += (s, e) => ApplyFilter();

            toolbar.Controls.Add(btnCapture, 0, 0);
            toolbar.Controls.Add(btnManage, 1, 0);
            toolbar.Controls.Add(btnExport, 2, 0);
            toolbar.Controls.Add(txtSearch, 3, 0);
            panelMain.Controls.Add(toolbar);
        }

        // ComboBox 在 144 DPI 下会拿着建控件那一刻的 GDI 字体画字，看着比旁边的 Label 大一圈；
        // 句柄建好之后重新赋一次同样的字体，让它按当前 DPI 重新建 GDI 字体。
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            Font ui = new Font("Microsoft YaHei UI", 9F);
            if (cmbBaseline != null) cmbBaseline.Font = ui;
            if (cmbCurrent != null) cmbCurrent.Font = ui;
        }

        private Label MakeCaption(string text)
        {
            var label = new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 9F),
                Padding = new Padding(8, 9, 0, 0)
            };
            return label;
        }

        private ComboBox MakeCombo()
        {
            var combo = new ComboBox
            {
                // 230 时这一行在 878 宽的页面上正好卡边缘，窗口稍窄就换行；
                // 缩到 180 给「只看 + 类型下拉框」腾位置，正常窗口下始终保持一行。
                Width = 180,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft YaHei UI", 9F),
                Margin = new Padding(3, 6, 3, 6)
            };
            return combo;
        }

        private ImageList BuildLegendImages()
        {
            legendImages = new ImageList { ImageSize = new Size(14, 14), ColorDepth = ColorDepth.Depth32Bit };
            legendImages.Images.Add(DrawDot(ColorAdded));
            legendImages.Images.Add(DrawDot(ColorRemoved));
            legendImages.Images.Add(DrawDot(ColorModified));
            legendImages.Images.Add(DrawDot(Color.FromArgb(150, 40, 150)));
            legendImages.Images.Add(DrawDot(ColorUnchanged));
            return legendImages;
        }

        private static Bitmap DrawDot(Color color)
        {
            var bitmap = new Bitmap(14, 14);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (var brush = new SolidBrush(color))
                    g.FillEllipse(brush, 2, 2, 10, 10);
            }
            return bitmap;
        }

        #endregion

        #region 数据刷新

        public void RefreshSnapshotList()
        {
            snapshots = SnapshotStore.LoadAll();

            string keepBaseline = SelectedId(cmbBaseline);
            string keepCurrent = SelectedId(cmbCurrent);

            suppressComboEvent = true;
            try
            {
                cmbBaseline.Items.Clear();
                cmbCurrent.Items.Clear();
                cmbBaseline.Items.Add(NoneItemText);
                cmbCurrent.Items.Add(NoneItemText);
                foreach (AppSnapshot snapshot in snapshots)
                {
                    string text = snapshot.TimeText;
                    cmbBaseline.Items.Add(text);
                    cmbCurrent.Items.Add(text);
                }

                bool hasAny = snapshots.Count > 0;
                btnManage.Enabled = hasAny;

                // 默认两侧都是「（不选）」：这时列表展示最新一份快照的全部应用；
                // 想比差异就自己各选一份（且不能选同一份）。
                cmbBaseline.SelectedIndex = IndexOfId(keepBaseline, 0);
                cmbCurrent.SelectedIndex = IndexOfId(keepCurrent, 0);
            }
            finally
            {
                suppressComboEvent = false;
            }

            RefreshDiff();
            _ = RefreshDeviceStateAsync();
        }

        /// <summary>返回某份快照在（带「（不选）」首项的）下拉框里的下标；查不到就用 fallback。</summary>
        private int IndexOfId(string id, int fallback)
        {
            if (!string.IsNullOrEmpty(id))
                for (int i = 0; i < snapshots.Count; i++)
                    if (snapshots[i].Id == id) return i + 1;
            return Math.Max(0, Math.Min(fallback, snapshots.Count));
        }

        private bool HasSelection(ComboBox combo)
        {
            return combo != null && combo.SelectedIndex > 0 && combo.SelectedIndex <= snapshots.Count;
        }

        private string SelectedId(ComboBox combo)
        {
            return HasSelection(combo) ? snapshots[combo.SelectedIndex - 1].Id : null;
        }

        private AppSnapshot SelectedSnapshot(ComboBox combo)
        {
            return HasSelection(combo) ? snapshots[combo.SelectedIndex - 1] : null;
        }

        /// <summary>程序里改下拉框时别把事件再打回 RefreshDiff，否则会递归。</summary>
        private void SelectCombo(ComboBox combo, int index)
        {
            if (combo == null || combo.Items.Count == 0) return;
            if (index < 0) index = 0;
            if (index >= combo.Items.Count) index = combo.Items.Count - 1;
            suppressComboEvent = true;
            combo.SelectedIndex = index;
            suppressComboEvent = false;
        }

        /// <summary>两份都选好才比对，否则列出单份快照的全部应用。</summary>
        private void RefreshDiff()
        {
            bool clearedDuplicate = false;
            AppSnapshot baseline = SelectedSnapshot(cmbBaseline);
            AppSnapshot current = SelectedSnapshot(cmbCurrent);

            if (baseline != null && current != null && baseline.Id == current.Id)
            {
                SelectCombo(cmbCurrent, 0);
                current = null;
                clearedDuplicate = true;
            }

            comparingNow = baseline != null && current != null;
            AppSnapshot view = comparingNow ? current : ViewSnapshot(baseline, current);

            if (comparingNow)
            {
                string pairKey = baseline.Id + "|" + current.Id;
                if (pairKey != lastPairKey)
                {
                    lastPairKey = pairKey;
                    onlyChangedTouched = false;
                }
                diffs = SnapshotDiff.Compare(baseline, current);
            }
            else if (view != null)
            {
                // 浏览模式：让这份快照自己跟自己比，得到一张「全部无变化」的完整应用表
                lastPairKey = null;
                onlyChangedTouched = false;
                diffs = SnapshotDiff.Compare(view, view);
            }
            else
            {
                lastPairKey = null;
                diffs = new List<AppDiff>();
            }

            bool hasChanges = comparingNow && HasChanges(diffs);
            chkOnlyChanged.Enabled = hasChanges;
            ApplyDefaultFilter(hasChanges);
            UpdateSummary(view);
            ApplyFilter();

            if (clearedDuplicate)
                lblProgress.Text = "基准和对照不能相同，已取消对照。";
            else if (comparingNow || view != null)
                lblProgress.Text = "双击行看详情";
            else
                lblProgress.Text = string.Empty;
        }

        /// <summary>不比对时展示哪一份快照。</summary>
        private AppSnapshot ViewSnapshot(AppSnapshot baseline, AppSnapshot current)
        {
            if (current != null) return current;
            if (baseline != null) return baseline;
            return snapshots.Count > 0 ? snapshots[0] : null;
        }

        /// <summary>有差异时默认只看有变化的行；没有差异（浏览单份快照）时默认全部列出。</summary>
        private void ApplyDefaultFilter(bool hasChanges)
        {
            if (onlyChangedTouched) return;
            if (chkOnlyChanged.Checked == hasChanges) return;
            suppressFilterEvent = true;
            chkOnlyChanged.Checked = hasChanges;
            suppressFilterEvent = false;
        }

        private static bool HasChanges(List<AppDiff> list)
        {
            foreach (AppDiff diff in list)
                if (diff.Kind != ChangeKind.Unchanged) return true;
            return false;
        }

        /// <summary>「2026-10-30 23:03」→「10-30 23:03」，底部摘要没必要重复年份。</summary>
        private static string ShortTime(string text)
        {
            if (text.Length > 5 && text[4] == '-' && text[0] >= '0' && text[0] <= '9')
                return text.Substring(5);
            return text;
        }

        private void UpdateSummary(AppSnapshot view)
        {
            if (snapshots.Count == 0)
            {
                lblSummary.Text = "还没有快照，点「生成快照」开始。";
                lblSummary.ForeColor = Color.DimGray;
                return;
            }

            if (!comparingNow)
            {
                // 没在比对：报一下看到的是哪一份、一共多少个应用
                lblSummary.Text = view == null
                    ? "已存 " + snapshots.Count + " 份快照"
                    : "共 " + diffs.Count + " 应用 · " + ShortTime(view.TimeText) + " 的快照";
                lblSummary.ForeColor = Color.DimGray;
                return;
            }

            AppSnapshot baseline = SelectedSnapshot(cmbBaseline);
            AppSnapshot current = SelectedSnapshot(cmbCurrent);

            int added = 0, removed = 0, modified = 0, unchanged = 0;
            foreach (AppDiff diff in diffs)
            {
                switch (diff.Kind)
                {
                    case ChangeKind.Added: added++; break;
                    case ChangeKind.Removed: removed++; break;
                    case ChangeKind.Modified: modified++; break;
                    default: unchanged++; break;
                }
            }
            lblSummary.Text = "共 " + diffs.Count + " 应用 · 变更 " + modified + " · 新装 " + added
                + " · 卸载 " + removed + " · 无变化 " + unchanged
                + " · " + (baseline != null ? ShortTime(baseline.TimeText) : "—")
                + " → " + (current != null ? ShortTime(current.TimeText) : "当前设备");
            lblSummary.ForeColor = modified + added + removed > 0 ? Color.FromArgb(180, 90, 0) : Color.DimGray;
        }

        private void ApplyFilter()
        {
            // 行马上要被清掉重建，旧的悬停提示再留着就指向不存在的行了
            HideRowTip();

            string keyword = txtSearch.Text.Trim();
            bool onlyChanged = chkOnlyChanged.Checked && comparingNow;
            bool onlyKind = chkOnlyKind.Checked;
            bool wantSystem = cmbKind.SelectedIndex == 0;

            listViewDiff.BeginUpdate();
            listViewDiff.Items.Clear();

            foreach (AppDiff diff in diffs)
            {
                if (onlyChanged && diff.Kind == ChangeKind.Unchanged) continue;
                if (onlyKind && diff.Current != null && diff.Current.IsSystem != wantSystem) continue;
                if (keyword.Length > 0
                    && diff.DisplayName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) < 0
                    && diff.BundleName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) < 0) continue;

                var item = new ListViewItem(CompactStatus ? string.Empty : diff.KindText);
                item.SubItems.Add(diff.DisplayName);
                item.SubItems.Add(diff.BundleName);
                item.SubItems.Add(VersionText(diff));
                item.SubItems.Add(CompactChange(diff.ChangeSummary));

                AppEntry entry = diff.Current;
                item.SubItems.Add(entry != null && entry.ApiTarget > 0 ? entry.ApiTarget.ToString() : "—");

                int imageIndex = 4;
                Color foreColor = ColorUnchanged;
                if (diff.Kind == ChangeKind.Added) { imageIndex = 0; foreColor = ColorAdded; }
                else if (diff.Kind == ChangeKind.Removed) { imageIndex = 1; foreColor = ColorRemoved; }
                else if (diff.Kind == ChangeKind.Modified)
                {
                    if (diff.SignatureChanged) { imageIndex = 3; foreColor = Color.FromArgb(150, 40, 150); }
                    else { imageIndex = 2; foreColor = ColorModified; }
                }

                item.ImageIndex = imageIndex;
                item.ForeColor = foreColor;
                item.Tag = diff;
                listViewDiff.Items.Add(item);
            }

            listViewDiff.EndUpdate();
        }

        private static string VersionText(AppDiff diff)
        {
            if (diff.Kind == ChangeKind.Added)
                return diff.New != null ? diff.New.VersionText : "—";
            if (diff.Kind == ChangeKind.Removed)
                return diff.Old != null ? diff.Old.VersionText : "—";

            string oldText = diff.Old != null ? diff.Old.VersionText : "—";
            string newText = diff.New != null ? diff.New.VersionText : "—";
            if (oldText == newText) return newText;
            // 列宽有限：只在版本变化时显示「旧 → 新」，versionCode 留给详情页
            string oldShort = diff.Old != null ? diff.Old.VersionName : oldText;
            string newShort = diff.New != null ? diff.New.VersionName : newText;
            return oldShort + "  →  " + newShort;
        }

        /// <summary>鼠标离开行、滚轮、切页、开详情时都要主动收起，否则它会自己挂好几秒。</summary>
        private void HideRowTip()
        {
            if (tipRow != null) tipRow.Hide(listViewDiff);
            lastTipKey = null;
        }

        private void ListViewDiff_MouseMove(object sender, MouseEventArgs e)
        {
            ListViewItem item = listViewDiff.GetItemAt(e.X, e.Y);
            AppDiff diff = item != null ? item.Tag as AppDiff : null;
            string key = diff == null ? null : diff.BundleName + "|" + (int)diff.Kind + "|" + diff.ChangeScore;

            if (key == lastTipKey) return;
            lastTipKey = key;

            if (diff == null)
            {
                if (tipRow != null) tipRow.Hide(listViewDiff);
                return;
            }

            if (tipRow == null)
            {
                // 直接 Show，所以 InitialDelay 用不上；显示时长压到 3 秒，跟鼠标停留的直觉一致
                tipRow = new ToolTip();
                tipRow.InitialDelay = 200;
                tipRow.ReshowDelay = 100;
                tipRow.AutoPopDelay = 3000;
            }

            tipRow.Show(diff.DisplayName + "\r\n" + diff.BundleName + "\r\n" + diff.ChangeSummary,
                listViewDiff, e.X + 18, e.Y + 22, 3000);
        }

        private void ListViewDiff_DoubleClick(object sender, EventArgs e)
        {
            OpenDetail();
        }

        /// <summary>列表上按回车等同双击。</summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Enter && listViewDiff != null && listViewDiff.Focused
                && listViewDiff.SelectedItems.Count > 0)
            {
                OpenDetail();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void CopySelectedBundle()
        {
            if (listViewDiff.SelectedItems.Count == 0) return;
            var diff = listViewDiff.SelectedItems[0].Tag as AppDiff;
            if (diff == null) return;
            try { Clipboard.SetText(diff.BundleName); } catch { }
        }

        private void OpenDetail()
        {
            HideRowTip();
            if (listViewDiff.SelectedItems.Count == 0) return;
            var diff = listViewDiff.SelectedItems[0].Tag as AppDiff;
            if (diff == null) return;

            using (var form = new AppDetailForm(diff, !comparingNow))
            {
                form.ShowDialog(this);
            }
        }

        #endregion

        #region 操作

        private async void BtnCapture_Click(object sender, EventArgs e)
        {
            if (busy) return;

            SetBusy(true, "正在读取设备信息…");
            try
            {
                var progress = new Progress<string>(message => lblProgress.Text = message);
                AppSnapshot snapshot = await Task.Run(() => SnapshotCapture.Capture(progress));

                SnapshotStore.Save(snapshot);
                RefreshSnapshotList();

                // 刚存完就自动摆出「上一次 → 这一次」，省得再去下拉框里挑一趟
                if (snapshots.Count >= 2)
                {
                    SelectCombo(cmbBaseline, 2);   // 倒数第二新（下标 0 是「（不选）」）
                    SelectCombo(cmbCurrent, 1);    // 最新
                    RefreshDiff();
                }

                MessageBox.Show(this,
                    "快照已保存。\n\n设备：" + snapshot.DeviceName + "\n应用：" + snapshot.Apps.Count + " 个（系统 " + snapshot.SystemCount + " / 第三方 " + snapshot.ThirdCount + "）\n"
                    + "文件：" + SnapshotStore.SizeOf(snapshot.Id) / 1024 + " KB",
                    "生成快照", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "生成快照失败：\n" + ex.Message, "生成快照", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                SetBusy(false, "");
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (diffs.Count == 0)
            {
                MessageBox.Show(this, "还没有比对结果，先比对一次再导出。", "导出报告", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!HasChanges(diffs))
            {
                MessageBox.Show(this, "这次比对没有任何变化，没有可导出的差异。", "导出报告", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "Markdown 文件|*.md|文本文件|*.txt";
                dialog.FileName = "应用快照比对-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".md";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    File.WriteAllText(dialog.FileName, BuildMarkdown(), new UTF8Encoding(true));
                    MessageBox.Show(this, "报告已导出：\n" + dialog.FileName, "导出报告", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "导出失败：\n" + ex.Message, "导出报告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private string BuildMarkdown()
        {
            var builder = new StringBuilder();
            AppSnapshot baseline = SelectedSnapshot(cmbBaseline);
            AppSnapshot current = SelectedSnapshot(cmbCurrent);

            builder.AppendLine("# 应用快照比对报告");
            builder.AppendLine();
            builder.AppendLine("- 基准快照：" + (baseline != null ? baseline.TimeText + "（" + baseline.Id + "）" : "—"));
            builder.AppendLine("- 对照快照：" + (current != null ? current.TimeText + "（" + current.Id + "）" : "当前设备"));
            builder.AppendLine("- 生成时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            builder.AppendLine();

            foreach (AppDiff diff in diffs)
            {
                if (diff.Kind == ChangeKind.Unchanged) continue;
                builder.AppendLine("## " + diff.DisplayName + "  （" + diff.KindText + "）");
                builder.AppendLine();
                builder.AppendLine("\u0060" + diff.BundleName + "\u0060");
                builder.AppendLine();
                builder.AppendLine("> " + diff.ChangeSummary);
                builder.AppendLine();
                builder.AppendLine("| 字段 | 旧值 | 新值 |");
                builder.AppendLine("| --- | --- | --- |");
                foreach (FieldChange field in diff.Fields)
                {
                    builder.AppendLine("| " + field.Label + " | " + Escape(field.OldValue) + " | " + Escape(field.NewValue) + " |");
                }
                if (diff.PermissionAdded.Count > 0)
                    builder.AppendLine("\n**新增权限**：" + string.Join(", ", diff.PermissionAdded.ToArray()));
                if (diff.PermissionRemoved.Count > 0)
                    builder.AppendLine("\n**移除权限**：" + string.Join(", ", diff.PermissionRemoved.ToArray()));
                if (diff.PermissionStateChanged.Count > 0)
                    builder.AppendLine("\n**授权状态变化**：" + string.Join("；", diff.PermissionStateChanged.ToArray()));
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string Escape(string text)
        {
            return (text ?? "").Replace("|", "\\|");
        }

        private void BtnManage_Click(object sender, EventArgs e)
        {
            using (var form = new SnapshotManagerForm())
            {
                form.ShowDialog(this);
                if (!form.Changed) return;

                // 删掉的可能是当前选中的基准/对照，重新读一遍并重新比对
                RefreshSnapshotList();
                lblProgress.Text = "快照已保存，共 " + snapshots.Count + " 份";
            }
        }

        /// <summary>按钮能不能点，取决于「正在忙」和「有没有连上手表」两个条件。</summary>
        private void ApplyButtonState()
        {
            btnCapture.Enabled = !busy && deviceOnline;
            // 导出只是把两份历史快照写成 Markdown，不需要手表在场，所以不受设备状态影响
            btnExport.Enabled = !busy;
            btnManage.Enabled = !busy && snapshots.Count > 0;
            cmbBaseline.Enabled = !busy;
            cmbCurrent.Enabled = !busy;
        }

        /// <summary>查一下设备在不在线，只用来控制按钮，失败就当没连。</summary>
        private async Task RefreshDeviceStateAsync()
        {
            if (deviceCheckRunning) return;
            deviceCheckRunning = true;
            try
            {
                // 注意：这里不能因为「状态没变」就提前 return，否则首次检测（默认 false → 查到 false）
                // 永远不会刷新按钮，离线时两个按钮会一直亮着。
                deviceOnline = await DeviceConnectionHelper.CheckAndUpdateConnectionStatusAsync();
                ApplyButtonState();
            }
            catch
            {
                // 检测本身失败不提示，用户点刷新或重新连接时自然会看到状态
            }
            finally
            {
                deviceCheckRunning = false;
            }
        }

        private void SetBusy(bool value, string status)
        {
            busy = value;
            ApplyButtonState();
            progressBar.Visible = value;
            progressBar.MarqueeAnimationSpeed = value ? 30 : 0;
            if (status != null) lblProgress.Text = status;
        }

        #endregion
    }
}
