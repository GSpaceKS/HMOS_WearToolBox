#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using HMOS_WearToolBox.Helper;
using HMOS_WearToolBox.Models;

namespace HMOS_WearToolBox.UserController
{
    /// <summary>
    /// 单个应用的详情：字段表 + 权限变化 + 全部权限。
    /// 由快照列表双击打开；浏览（没在比对）时不显示「旧值」，只显示当前值。
    /// </summary>
    public class AppDetailForm : Form
    {
        private static readonly Color ColorChanged = Color.FromArgb(196, 100, 0);

        private readonly AppDiff diff;
        private readonly bool browsing;
        private ListView listFields;

        public AppDetailForm(AppDiff diff, bool browsing = false)
        {
            this.diff = diff;
            this.browsing = browsing;
            BuildUi();
        }

        /// <summary>只有「真的改过」的应用才值得摆旧值；浏览、新装、卸载都只看当前值。</summary>
        private bool ShowOldValue
        {
            get { return diff.Kind == ChangeKind.Modified; }
        }

        private void BuildUi()
        {
            Text = (diff.DisplayName ?? "应用") + (browsing ? string.Empty : "  ·  " + diff.KindText);
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(880, 760);
            MinimumSize = new Size(680, 520);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Microsoft YaHei UI", 9F);
            ShowInTaskbar = false;
            // 与 MainForm 保持一致：设计基准 144 DPI（150% 缩放），由 WinForms 按当前 DPI 换算。
            // 用 None 的话控件尺寸不动、字体却按实际 DPI 放大，9pt 中文会变成 18px 宽，
            // 112px 的按钮里两个字就顶到边上了。
            AutoScaleDimensions = new SizeF(144F, 144F);
            AutoScaleMode = AutoScaleMode.Dpi;

            var panelFooter = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(14, 7, 14, 7) };
            var btnClose = UiFactory.MakeButton("关闭", 96, 34, new Padding(0, 1, 10, 0));
            btnClose.Dock = DockStyle.Right;
            btnClose.Click += (s, e) => Close();
            panelFooter.Controls.Add(btnClose);
            Controls.Add(panelFooter);

            // 顶栏内容 32(标题) + 24(包名) + 36(按钮行) = 92，加上上下内边距 14 需要 ≥106，
            // 定 98 时按钮行会往上溢出 8px，正好压住包名那一行（按钮顶部被切）。
            var panelHeader = new Panel { Dock = DockStyle.Top, Height = 112, Padding = new Padding(14, 10, 14, 6) };

            var flowHeaderButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            var btnCopyBundle = UiFactory.MakeButton("复制包名", 140, 34, new Padding(0, 1, 10, 0));
            btnCopyBundle.Click += (s, e) => CopyText(diff.BundleName);

            var btnCopyFingerprint = UiFactory.MakeButton("复制指纹", 140, 34, new Padding(0, 1, 10, 0));
            btnCopyFingerprint.Click += (s, e) =>
            {
                AppEntry entry = diff.Current;
                CopyText(entry != null ? entry.Fingerprint : "");
            };

            flowHeaderButtons.Controls.Add(btnCopyBundle);
            flowHeaderButtons.Controls.Add(btnCopyFingerprint);

            // 浏览模式没有比对对象，「无变化」这个状态没有意义
            if (!browsing)
            {
                var lblKind = new Label
                {
                    AutoSize = true,
                    Padding = new Padding(2, 10, 0, 0),
                    ForeColor = KindColor(diff.Kind),
                    Text = "状态：" + diff.KindText
                };
                flowHeaderButtons.Controls.Add(lblKind);
            }

            var lblBundle = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                ForeColor = Color.DimGray,
                Text = diff.BundleName
            };

            var lblName = new Label
            {
                Dock = DockStyle.Top,
                Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold),
                Text = diff.DisplayName
            };

            // 标题行高必须按实际字体量出来：14pt 粗体在 144 DPI 下光字身就 28px 左右，
            // 写死 32 时 y/g/p 这类带下伸部的字母会被裁掉下半截。
            lblName.Height = TextRenderer.MeasureText(lblName.Text, lblName.Font).Height + 8;
            lblBundle.Height = TextRenderer.MeasureText(lblBundle.Text, lblBundle.Font).Height + 6;
            panelHeader.Height = lblName.Height + lblBundle.Height
                + flowHeaderButtons.Height + panelHeader.Padding.Vertical;

            panelHeader.Controls.Add(lblBundle);
            panelHeader.Controls.Add(lblName);
            panelHeader.Controls.Add(flowHeaderButtons);
            Controls.Add(panelHeader);

            var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(14, 6) };
            tabs.TabPages.Add(BuildFieldTab());
            tabs.TabPages.Add(BuildAllPermissionTab());
            tabs.TabPages.Add(BuildPermissionTab());
            // 停靠顺序：索引越大越先占位。Fill 必须最先 Add（索引 0、最后布局），
            // 否则它会先吃掉整块客户区，顶栏和底栏再叠在它上面（曾导致列表表头和首行被顶栏遮住）。
            Controls.Add(tabs);
            Controls.Add(panelHeader);
            Controls.Add(panelFooter);
        }

        /// <summary>字段表按当前宽度分配：两列时值列吃满，三列时旧值/新值平分剩余空间。</summary>
        private void LayoutFieldColumns()
        {
            if (listFields == null || listFields.Columns.Count < 2) return;
            int width = listFields.ClientSize.Width;
            if (width <= 320) return;

            int label = Math.Max(110, (int)(width * 0.20));
            int rest = width - label - 2;
            listFields.Columns[0].Width = label;

            if (listFields.Columns.Count == 2)
            {
                listFields.Columns[1].Width = rest;
                return;
            }

            int half = rest / 2;
            listFields.Columns[1].Width = half;
            listFields.Columns[2].Width = rest - half;
        }

        private TabPage BuildFieldTab()
        {
            var page = new TabPage(ShowOldValue ? "字段变化" : "字段") { Padding = new Padding(6) };

            var list = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            list.Columns.Add("字段", 150, HorizontalAlignment.Left);
            if (ShowOldValue)
            {
                list.Columns.Add("旧值", 320, HorizontalAlignment.Left);
                list.Columns.Add("新值", 320, HorizontalAlignment.Left);
            }
            else
            {
                list.Columns.Add("值", 480, HorizontalAlignment.Left);
            }
            listFields = list;
            list.Resize += (s3, e3) => LayoutFieldColumns();
            LayoutFieldColumns();

            foreach (FieldChange field in diff.Fields)
            {
                // 指纹是 64 位 hex，整串塞进列里会被截成两段一样的文字，
                // 变更反而看不出来；列表里只放「前 12 … 后 8」，完整值走 tooltip。
                bool isFingerprint = field.Label == "签名指纹";
                var item = new ListViewItem(field.Label);
                if (ShowOldValue)
                {
                    item.SubItems.Add(isFingerprint ? ShortFingerprint(field.OldValue) : field.OldValue);
                    item.SubItems.Add(isFingerprint ? ShortFingerprint(field.NewValue) : field.NewValue);
                }
                else
                {
                    string value = DiffValue(field);
                    item.SubItems.Add(isFingerprint ? ShortFingerprint(value) : value);
                }
                item.Tag = field;
                // 新装/卸载的应用每个字段都算「有变化」，整表标色反而没有信息量，只给版本内变更高亮
                if (field.Changed && diff.Kind == ChangeKind.Modified)
                {
                    item.ForeColor = ColorChanged;
                    item.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
                }
                list.Items.Add(item);
            }

            var tipCell = new ToolTip { InitialDelay = 500, AutoPopDelay = 20000, ReshowDelay = 100, ShowAlways = true };
            list.MouseMove += (s2, e2) =>
            {
                ListViewHitTestInfo hit = list.HitTest(e2.Location);
                if (hit.Item == null || hit.SubItem == null)
                {
                    tipCell.SetToolTip(list, string.Empty);
                    return;
                }
                var field = hit.Item.Tag as FieldChange;
                if (field == null)
                {
                    tipCell.SetToolTip(list, string.Empty);
                    return;
                }
                string text;
                int index = hit.Item.SubItems.IndexOf(hit.SubItem);
                if (!ShowOldValue) text = field.Label + "\r\n" + DiffValue(field);
                else if (index == 1) text = field.Label + "（旧值）\r\n" + field.OldValue;
                else if (index == 2) text = field.Label + "（新值）\r\n" + field.NewValue;
                else text = field.Label;
                tipCell.SetToolTip(list, text);
            };

            page.Controls.Add(list);
            return page;
        }

        /// <summary>当前（或最后一次）快照里这个应用声明的全部权限，含用户是否已授权。</summary>
        private TabPage BuildAllPermissionTab()
        {
            AppEntry entry = diff.Current;
            List<string> permissions = entry != null && entry.Permissions != null
                ? entry.Permissions
                : new List<string>();

            var page = new TabPage("全部权限（" + permissions.Count + "）") { Padding = new Padding(6) };

            if (permissions.Count == 0)
            {
                var lblEmpty = new Label
                {
                    Dock = DockStyle.Fill,
                    Text = "这个应用没有声明任何权限。",
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = Color.DimGray
                };
                page.Controls.Add(lblEmpty);
                return page;
            }

            var list = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            list.Columns.Add("#", 48, HorizontalAlignment.Right);
            list.Columns.Add("权限", 520, HorizontalAlignment.Left);
            list.Columns.Add("授权状态", 90, HorizontalAlignment.Left);

            for (int i = 0; i < permissions.Count; i++)
            {
                string state = "—";
                if (entry != null && entry.PermissionStates != null && i < entry.PermissionStates.Count)
                    state = entry.PermissionStates[i] != 0 ? "已授权" : "未授权";

                var item = new ListViewItem((i + 1).ToString());
                item.SubItems.Add(permissions[i]);
                item.SubItems.Add(state);
                item.ForeColor = state == "已授权" ? Color.FromArgb(0, 110, 60) : Color.DimGray;
                list.Items.Add(item);
            }

            // 权限名很长（最多见过 77 条），窗口拉宽时把中间那列撑开
            list.Resize += (s, e) =>
            {
                int width = list.ClientSize.Width - list.Columns[0].Width - list.Columns[2].Width - 4;
                if (width > 200) list.Columns[1].Width = width;
            };

            page.Controls.Add(list);
            return page;
        }

        private TabPage BuildPermissionTab()
        {
            var page = new TabPage("权限变化") { Padding = new Padding(6) };
            var host = new Panel { Dock = DockStyle.Fill, AutoScroll = true };

            int top = 4;
            top = AddPermissionBlock(host, top, "新增权限（+" + diff.PermissionAdded.Count + "）", diff.PermissionAdded, Color.FromArgb(0, 130, 70));
            top = AddPermissionBlock(host, top, "移除权限（-" + diff.PermissionRemoved.Count + "）", diff.PermissionRemoved, Color.FromArgb(190, 40, 40));
            AddPermissionBlock(host, top, "授权状态变化（" + diff.PermissionStateChanged.Count + " 项，用户设置导致，与应用更新无关）",
                diff.PermissionStateChanged, Color.FromArgb(196, 100, 0));

            page.Controls.Add(host);
            return page;
        }

        private static int AddPermissionBlock(Control host, int top, string title, List<string> items, Color color)
        {
            var lblTitle = new Label
            {
                Text = title,
                AutoSize = true,
                Left = 8,
                Top = top,
                Font = new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold),
                ForeColor = color
            };
            host.Controls.Add(lblTitle);
            top += 30;

            if (items == null || items.Count == 0)
            {
                var lblEmpty = new Label { Text = "（无）", AutoSize = true, Left = 18, Top = top, ForeColor = Color.DimGray };
                host.Controls.Add(lblEmpty);
                return top + 34;
            }

            foreach (string item in items)
            {
                var lblItem = new Label
                {
                    Text = "·  " + item,
                    AutoSize = true,
                    Left = 18,
                    Top = top,
                    Font = new Font("Consolas", 9F),
                    ForeColor = color
                };
                host.Controls.Add(lblItem);
                top += 24;
            }
            return top + 12;
        }

        /// <summary>列表里只显示当前值：新值优先，空的话退回旧值（卸载的应用只有旧值）。</summary>
        private static string DiffValue(FieldChange field)
        {
            if (!string.IsNullOrWhiteSpace(field.NewValue) && field.NewValue != "—")
                return field.NewValue;
            return field.OldValue;
        }

        /// <summary>64 位指纹收成「前 12 … 后 8」，完整值走单元格 tooltip。</summary>
        private static string ShortFingerprint(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return value;
            if (value.Length <= 28) return value;
            return value.Substring(0, 12) + "…" + value.Substring(value.Length - 8);
        }

        private static Color KindColor(ChangeKind kind)
        {
            switch (kind)
            {
                case ChangeKind.Added: return Color.FromArgb(0, 130, 70);
                case ChangeKind.Removed: return Color.FromArgb(190, 40, 40);
                case ChangeKind.Modified: return Color.FromArgb(196, 100, 0);
                default: return Color.DimGray;
            }
        }

        private void CopyText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show(this, "没有可复制的内容。", "复制", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                Clipboard.SetText(text);
            }
            catch { }
        }
    }
}
