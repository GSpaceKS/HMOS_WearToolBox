#nullable disable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using HMOS_WearToolBox.Helper;
using HMOS_WearToolBox.Models;

namespace HMOS_WearToolBox.UserController
{
    /// <summary>
    /// 快照管理：列出磁盘上所有快照，勾选后批量删除。
    /// 主界面只有一个「管理快照」入口，删哪一份在这里挑。
    /// </summary>
    public class SnapshotManagerForm : Form
    {
        private Panel panelHeader;
        private Panel panelTools;
        private Panel panelBottom;
        private Label lblPath;
        private ToolTip tipPath;
        private Label lblSelection;
        private CheckBox chkAll;
        private Button btnClose;
        private Button btnDeleteSnapshots;
        private Button btnPickFolder;
        private Button btnResetFolder;
        private Button btnOpenFolder;
        private ListView listSnapshots;
        private List<AppSnapshot> snapshots = new List<AppSnapshot>();
        private bool suppressCheck;

        public bool Deleted { get; private set; }

        /// <summary>窗口里改动过数据（删除过快照、换过快照目录），调用方据此决定要不要刷新。</summary>
        public bool Changed { get; private set; }

        public SnapshotManagerForm()
        {
            BuildUi();
            ReloadSnapshots();
        }

        private void BuildUi()
        {
            Text = "快照管理";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(820, 560);
            MinimumSize = new Size(620, 400);
            Font = new Font("Microsoft YaHei UI", 9F);
            // 与 MainForm / AppDetailForm 一致：按 144 DPI 设计，否则 9pt 中文在 150% 缩放下会顶到按钮边上
            AutoScaleDimensions = new SizeF(144F, 144F);
            AutoScaleMode = AutoScaleMode.Dpi;

            // 停靠顺序：Fill 的控件要最先 Add，后 Add 的 Top/Bottom 才会把它挤进剩下的空间
            listSnapshots = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                CheckBoxes = true,
                FullRowSelect = true,
                MultiSelect = false,
                HideSelection = false,
                UseCompatibleStateImageBehavior = false,
                Font = new Font("Microsoft YaHei UI", 9F)
            };
            listSnapshots.Columns.Add("快照时间", 200, HorizontalAlignment.Left);
            listSnapshots.Columns.Add("应用数", 76, HorizontalAlignment.Center);
            listSnapshots.Columns.Add("系统", 56, HorizontalAlignment.Center);
            listSnapshots.Columns.Add("第三方", 66, HorizontalAlignment.Center);
            listSnapshots.Columns.Add("大小", 84, HorizontalAlignment.Right);
            listSnapshots.Columns.Add("设备", 300, HorizontalAlignment.Left);
            listSnapshots.ItemChecked += (s, e) => { if (!suppressCheck) UpdateSelection(); };
            listSnapshots.Resize += (s, e) => LayoutColumns();
            Controls.Add(listSnapshots);

            panelTools = new Panel { Dock = DockStyle.Top, Height = 40, Padding = new Padding(14, 4, 14, 4) };
            chkAll = new CheckBox { Text = "全选", AutoSize = true, Location = new Point(14, 9) };
            chkAll.CheckedChanged += ChkAll_CheckedChanged;
            var lblHint = new Label
            {
                Text = "勾选要删除的快照，可以多选。",
                AutoSize = true,
                ForeColor = Color.DimGray,
                Location = new Point(102, 11)
            };
            btnOpenFolder = UiFactory.MakeButton("打开目录", 96, 28);
            btnOpenFolder.Dock = DockStyle.Right;
            btnOpenFolder.Click += BtnOpenFolder_Click;
            btnPickFolder = UiFactory.MakeButton("更改目录…", 118, 28);
            btnPickFolder.Dock = DockStyle.Right;
            btnPickFolder.Click += BtnPickFolder_Click;
            btnResetFolder = UiFactory.MakeButton("恢复默认", 96, 28);
            btnResetFolder.Dock = DockStyle.Right;
            btnResetFolder.Click += BtnResetFolder_Click;

            panelTools.Controls.Add(chkAll);
            panelTools.Controls.Add(lblHint);
            // Dock=Right 的要最后加，不然会被前面 Add 的挤掉位置
            panelTools.Controls.Add(btnOpenFolder);
            panelTools.Controls.Add(btnResetFolder);
            panelTools.Controls.Add(btnPickFolder);
            Controls.Add(panelTools);

            panelHeader = new Panel { Dock = DockStyle.Top, Height = 82, Padding = new Padding(14, 10, 14, 6) };
            var lblTitle = new Label
            {
                Text = "快照管理",
                Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(14, 6)
            };
            lblPath = new Label
            {
                Text = "存放目录：" + SnapshotStore.Folder,
                AutoSize = false,
                Height = 22,
                Width = panelHeader.ClientSize.Width - 28,
                Location = new Point(14, 42),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                ForeColor = Color.DimGray,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            tipPath = new ToolTip { InitialDelay = 400, AutoPopDelay = 20000 };
            tipPath.SetToolTip(lblPath, SnapshotStore.Folder);
            panelHeader.Controls.Add(lblTitle);
            panelHeader.Controls.Add(lblPath);
            panelHeader.Resize += (s, e) => { lblPath.Width = Math.Max(80, panelHeader.ClientSize.Width - 28); };
            UpdateFolderUi();
            Controls.Add(panelHeader);

            panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(14, 11, 14, 11) };
            lblSelection = new Label
            {
                Dock = DockStyle.Left,
                Width = 360,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true
            };
            btnClose = UiFactory.MakeButton("关闭", 96, 34);
            btnClose.Dock = DockStyle.Right;
            btnClose.Click += (s, e) => Close();
            btnDeleteSnapshots = UiFactory.MakeButton("删除选中", 120, 34);
            btnDeleteSnapshots.Dock = DockStyle.Right;
            btnDeleteSnapshots.ForeColor = Color.Firebrick;
            btnDeleteSnapshots.Click += BtnDelete_Click;
            panelBottom.Controls.Add(lblSelection);
            panelBottom.Controls.Add(btnClose);
            panelBottom.Controls.Add(btnDeleteSnapshots);
            Controls.Add(panelBottom);

            CancelButton = btnClose;
        }

        /// <summary>窗口变宽时把多出来的宽度全给「设备」列，其余列固定。</summary>
        private void LayoutColumns()
        {
            if (listSnapshots == null || listSnapshots.Columns.Count < 6) return;
            int width = listSnapshots.ClientSize.Width;
            if (width <= 0) return;

            int others = 0;
            for (int i = 0; i < 5; i++) others += listSnapshots.Columns[i].Width;
            int device = Math.Max(200, width - others - 4); // 留一点余量，别把横向滚动条逼出来

            // 写回宽度会再触发 Resize，但结果收敛，不会死循环
            if (listSnapshots.Columns[5].Width != device) listSnapshots.Columns[5].Width = device;
        }

        private void UpdateFolderUi()
        {
            string folder = SnapshotStore.Folder;
            lblPath.Text = "存放目录：" + folder;
            tipPath.SetToolTip(lblPath, folder);
            btnResetFolder.Enabled = !string.Equals(
                Path.GetFullPath(folder).TrimEnd('\\'),
                Path.GetFullPath(SnapshotStore.DefaultFolder).TrimEnd('\\'),
                StringComparison.OrdinalIgnoreCase);
        }

        private void BtnOpenFolder_Click(object sender, EventArgs e)
        {
            string folder = SnapshotStore.Folder;
            try
            {
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "创建目录失败：" + ex.Message + "\n\n目录：" + folder, "打开目录", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string error = ShellHelper.OpenFolder(folder);
            if (error != null)
                MessageBox.Show(this, "打开目录失败：" + error + "\n\n目录：" + folder, "打开目录", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void BtnPickFolder_Click(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "选择快照存放目录";
                dialog.UseDescriptionForTitle = true;
                dialog.ShowNewFolderButton = true;
                string current = SnapshotStore.Folder;
                dialog.SelectedPath = Directory.Exists(current) ? current : SnapshotStore.DefaultFolder;
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                SwitchFolder(dialog.SelectedPath);
            }
        }

        private void BtnResetFolder_Click(object sender, EventArgs e)
        {
            SwitchFolder(SnapshotStore.DefaultFolder);
        }

        /// <summary>切换目录；顺手问一句要不要把现有快照复制过去，免得列表突然变空。</summary>
        private void SwitchFolder(string target)
        {
            string current = SnapshotStore.Folder;
            try
            {
                if (string.Equals(Path.GetFullPath(current).TrimEnd('\\'), Path.GetFullPath(target).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(this, "这已经是当前的存放目录了。", "存放目录", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                int count = 0;
                if (Directory.Exists(current)) count = Directory.GetFiles(current, "*.json").Length;

                if (count > 0)
                {
                    string question = "要把当前目录里的 " + count + " 份快照复制到新目录吗？\n\n选「否」只换目录，不搬文件。";
                    if (MessageBox.Show(this, question, "存放目录", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        int copied = SnapshotStore.CopySnapshots(current, target);
                        if (copied < count)
                        {
                            MessageBox.Show(this, "有 " + (count - copied) + " 份没能复制（可能目标目录里已有同名文件，或者没有写入权限）。",
                                "存放目录", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                }

                SnapshotStore.SetFolder(target);
                Directory.CreateDirectory(target);
                Changed = true;
                UpdateFolderUi();
                ReloadSnapshots();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "切换存放目录失败：" + ex.Message, "存放目录", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReloadSnapshots()
        {
            snapshots = SnapshotStore.LoadAll();

            suppressCheck = true;
            listSnapshots.BeginUpdate();
            listSnapshots.Items.Clear();
            foreach (AppSnapshot snapshot in snapshots)
            {
                var item = new ListViewItem(snapshot.TimeText);
                item.SubItems.Add(snapshot.Apps.Count.ToString());
                item.SubItems.Add(snapshot.SystemCount.ToString());
                item.SubItems.Add(snapshot.ThirdCount.ToString());
                item.SubItems.Add(FormatSize(SnapshotStore.SizeOf(snapshot.Id)));
                item.SubItems.Add(string.IsNullOrEmpty(snapshot.DeviceName) ? "—" : snapshot.DeviceName);
                item.Tag = snapshot;
                listSnapshots.Items.Add(item);
            }
            listSnapshots.EndUpdate();
            suppressCheck = false;

            UpdateSelection();
        }

        private void ChkAll_CheckedChanged(object sender, EventArgs e)
        {
            if (suppressCheck) return;
            suppressCheck = true;
            foreach (ListViewItem item in listSnapshots.Items) item.Checked = chkAll.Checked;
            suppressCheck = false;
            UpdateSelection();
        }

        private void UpdateSelection()
        {
            int checkedCount = listSnapshots.CheckedItems.Count;
            long totalBytes = 0;
            foreach (ListViewItem item in listSnapshots.CheckedItems)
            {
                var snapshot = item.Tag as AppSnapshot;
                if (snapshot != null) totalBytes += SnapshotStore.SizeOf(snapshot.Id);
            }

            if (snapshots.Count == 0)
                lblSelection.Text = "还没有任何快照。";
            else if (checkedCount == 0)
                lblSelection.Text = "共 " + snapshots.Count + " 份快照，未选择。";
            else
                lblSelection.Text = "已选 " + checkedCount + " / " + snapshots.Count + " 份，合计 " + FormatSize(totalBytes) + "。";

            btnDeleteSnapshots.Enabled = checkedCount > 0;

            // 全选状态跟着走，但别让它再打回 ChkAll_CheckedChanged
            suppressCheck = true;
            chkAll.Checked = snapshots.Count > 0 && checkedCount == snapshots.Count;
            suppressCheck = false;
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            var ids = new List<string>();
            foreach (ListViewItem item in listSnapshots.CheckedItems)
            {
                var snapshot = item.Tag as AppSnapshot;
                if (snapshot != null) ids.Add(snapshot.Id);
            }

            if (ids.Count == 0)
            {
                MessageBox.Show(this, "先勾选要删除的快照。", "删除快照", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string question = ids.Count == 1
                ? "确定删除这 1 份快照？\n\n删除后无法恢复。"
                : "确定删除选中的 " + ids.Count + " 份快照？\n\n删除后无法恢复。";

            if (MessageBox.Show(this, question, "删除快照", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            int failed = 0;
            foreach (string id in ids)
            {
                SnapshotStore.Delete(id);
                if (File.Exists(SnapshotStore.PathFor(id))) failed++;
            }

            if (failed < ids.Count) { Deleted = true; Changed = true; }
            ReloadSnapshots();

            if (failed > 0)
                MessageBox.Show(this, "有 " + failed + " 份快照没能删除（文件可能被别的程序占用）。", "删除快照", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private static string FormatSize(long bytes)
        {
            if (bytes <= 0) return "—";
            if (bytes >= 1024 * 1024) return (bytes / 1024.0 / 1024.0).ToString("0.0") + " MB";
            return (bytes / 1024.0).ToString("0") + " KB";
        }
    }
}
