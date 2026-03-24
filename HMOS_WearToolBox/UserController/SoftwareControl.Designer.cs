namespace HMOS_WearToolBox.UserController
{
    partial class SoftwareControl
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            flowLayoutPanelControl = new FlowLayoutPanel();
            lblConnectStatusC = new Label();
            lblConnectStatus = new Label();
            btnRefresh = new Button();
            btnInstall = new Button();
            btnUninstall = new Button();
            tableLayoutPanelNotices = new TableLayoutPanel();
            groupBoxTask = new GroupBox();
            progressBarTask = new ProgressBar();
            labelNotice_1 = new Label();
            labelNotice_2 = new Label();
            panelMain = new Panel();
            listViewApps = new ListView();
            columnHeader1 = new ColumnHeader();
            columnHeader2 = new ColumnHeader();
            flowLayoutPanelControl.SuspendLayout();
            tableLayoutPanelNotices.SuspendLayout();
            groupBoxTask.SuspendLayout();
            panelMain.SuspendLayout();
            SuspendLayout();
            // 
            // flowLayoutPanelControl
            // 
            flowLayoutPanelControl.AutoSize = true;
            flowLayoutPanelControl.Controls.Add(lblConnectStatusC);
            flowLayoutPanelControl.Controls.Add(lblConnectStatus);
            flowLayoutPanelControl.Controls.Add(btnRefresh);
            flowLayoutPanelControl.Controls.Add(btnInstall);
            flowLayoutPanelControl.Controls.Add(btnUninstall);
            flowLayoutPanelControl.Dock = DockStyle.Top;
            flowLayoutPanelControl.Location = new Point(0, 0);
            flowLayoutPanelControl.Name = "flowLayoutPanelControl";
            flowLayoutPanelControl.Size = new Size(878, 40);
            flowLayoutPanelControl.TabIndex = 0;
            // 
            // lblConnectStatusC
            // 
            lblConnectStatusC.AutoSize = true;
            lblConnectStatusC.Dock = DockStyle.Fill;
            lblConnectStatusC.Font = new Font("Microsoft YaHei UI", 10F);
            lblConnectStatusC.Location = new Point(3, 0);
            lblConnectStatusC.Name = "lblConnectStatusC";
            lblConnectStatusC.Size = new Size(97, 40);
            lblConnectStatusC.TabIndex = 3;
            lblConnectStatusC.Text = "连接状态:";
            lblConnectStatusC.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblConnectStatus
            // 
            lblConnectStatus.AutoSize = true;
            lblConnectStatus.Dock = DockStyle.Fill;
            lblConnectStatus.Font = new Font("Microsoft YaHei UI", 10F);
            lblConnectStatus.ForeColor = Color.Red;
            lblConnectStatus.Location = new Point(106, 0);
            lblConnectStatus.Name = "lblConnectStatus";
            lblConnectStatus.Size = new Size(72, 40);
            lblConnectStatus.TabIndex = 4;
            lblConnectStatus.Text = "未连接";
            lblConnectStatus.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new Point(184, 3);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(112, 34);
            btnRefresh.TabIndex = 0;
            btnRefresh.Text = "刷新列表";
            btnRefresh.UseVisualStyleBackColor = true;
            // 
            // btnInstall
            // 
            btnInstall.Location = new Point(302, 3);
            btnInstall.Name = "btnInstall";
            btnInstall.Size = new Size(112, 34);
            btnInstall.TabIndex = 1;
            btnInstall.Text = "安装软件";
            btnInstall.UseVisualStyleBackColor = true;
            // 
            // btnUninstall
            // 
            btnUninstall.Location = new Point(420, 3);
            btnUninstall.Name = "btnUninstall";
            btnUninstall.Size = new Size(112, 34);
            btnUninstall.TabIndex = 2;
            btnUninstall.Text = "卸载软件";
            btnUninstall.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanelNotices
            // 
            tableLayoutPanelNotices.AutoSize = true;
            tableLayoutPanelNotices.ColumnCount = 1;
            tableLayoutPanelNotices.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelNotices.Controls.Add(groupBoxTask, 0, 2);
            tableLayoutPanelNotices.Controls.Add(labelNotice_1, 0, 0);
            tableLayoutPanelNotices.Controls.Add(labelNotice_2, 0, 1);
            tableLayoutPanelNotices.Dock = DockStyle.Bottom;
            tableLayoutPanelNotices.Location = new Point(0, 820);
            tableLayoutPanelNotices.Name = "tableLayoutPanelNotices";
            tableLayoutPanelNotices.Padding = new Padding(10);
            tableLayoutPanelNotices.RowCount = 3;
            tableLayoutPanelNotices.RowStyles.Add(new RowStyle());
            tableLayoutPanelNotices.RowStyles.Add(new RowStyle());
            tableLayoutPanelNotices.RowStyles.Add(new RowStyle());
            tableLayoutPanelNotices.Size = new Size(878, 197);
            tableLayoutPanelNotices.TabIndex = 1;
            // 
            // groupBoxTask
            // 
            groupBoxTask.AutoSize = true;
            groupBoxTask.Controls.Add(progressBarTask);
            groupBoxTask.Dock = DockStyle.Fill;
            groupBoxTask.Location = new Point(13, 97);
            groupBoxTask.Name = "groupBoxTask";
            groupBoxTask.Padding = new Padding(15);
            groupBoxTask.Size = new Size(852, 87);
            groupBoxTask.TabIndex = 2;
            groupBoxTask.TabStop = false;
            groupBoxTask.Text = "任务进度";
            // 
            // progressBarTask
            // 
            progressBarTask.Dock = DockStyle.Top;
            progressBarTask.Location = new Point(15, 38);
            progressBarTask.Name = "progressBarTask";
            progressBarTask.Size = new Size(822, 34);
            progressBarTask.TabIndex = 0;
            // 
            // labelNotice_1
            // 
            labelNotice_1.AutoSize = true;
            labelNotice_1.Dock = DockStyle.Fill;
            labelNotice_1.Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold);
            labelNotice_1.ForeColor = Color.Red;
            labelNotice_1.Location = new Point(13, 10);
            labelNotice_1.Name = "labelNotice_1";
            labelNotice_1.Size = new Size(852, 37);
            labelNotice_1.TabIndex = 0;
            labelNotice_1.Text = "⚠软件名由作者从Watch5提取包名之后交由AI上网搜索而来⚠";
            labelNotice_1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // labelNotice_2
            // 
            labelNotice_2.AutoSize = true;
            labelNotice_2.Dock = DockStyle.Fill;
            labelNotice_2.Font = new Font("Microsoft YaHei UI", 18F, FontStyle.Bold);
            labelNotice_2.ForeColor = Color.Red;
            labelNotice_2.Location = new Point(13, 47);
            labelNotice_2.Name = "labelNotice_2";
            labelNotice_2.Size = new Size(852, 47);
            labelNotice_2.TabIndex = 1;
            labelNotice_2.Text = "软件名不包对，请谨慎卸载！";
            labelNotice_2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panelMain
            // 
            panelMain.AutoSize = true;
            panelMain.Controls.Add(listViewApps);
            panelMain.Controls.Add(tableLayoutPanelNotices);
            panelMain.Controls.Add(flowLayoutPanelControl);
            panelMain.Dock = DockStyle.Fill;
            panelMain.Location = new Point(0, 0);
            panelMain.Name = "panelMain";
            panelMain.Size = new Size(878, 1017);
            panelMain.TabIndex = 3;
            // 
            // listViewApps
            // 
            listViewApps.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2 });
            listViewApps.Dock = DockStyle.Fill;
            listViewApps.FullRowSelect = true;
            listViewApps.Location = new Point(0, 40);
            listViewApps.Name = "listViewApps";
            listViewApps.Size = new Size(878, 780);
            listViewApps.TabIndex = 2;
            listViewApps.UseCompatibleStateImageBehavior = false;
            listViewApps.View = View.Details;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "软件名";
            columnHeader1.TextAlign = HorizontalAlignment.Center;
            columnHeader1.Width = 270;
            // 
            // columnHeader2
            // 
            columnHeader2.Text = "包名";
            columnHeader2.TextAlign = HorizontalAlignment.Center;
            columnHeader2.Width = 608;
            // 
            // SoftwareControl
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(panelMain);
            Name = "SoftwareControl";
            Size = new Size(878, 1017);
            flowLayoutPanelControl.ResumeLayout(false);
            flowLayoutPanelControl.PerformLayout();
            tableLayoutPanelNotices.ResumeLayout(false);
            tableLayoutPanelNotices.PerformLayout();
            groupBoxTask.ResumeLayout(false);
            panelMain.ResumeLayout(false);
            panelMain.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private FlowLayoutPanel flowLayoutPanelControl;
        private Label lblConnectStatusC;
        private Label lblConnectStatus;
        private Button btnRefresh;
        private Button btnInstall;
        private Button btnUninstall;
        private TableLayoutPanel tableLayoutPanelNotices;
        private Label labelNotice_1;
        private Label labelNotice_2;
        private GroupBox groupBoxTask;
        private ProgressBar progressBarTask;
        private Panel panelMain;
        private ListView listViewApps;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
    }
}
