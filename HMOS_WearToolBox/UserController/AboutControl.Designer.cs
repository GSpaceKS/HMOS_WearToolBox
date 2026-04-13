namespace HMOS_WearToolBox.UserController
{
    partial class AboutControl
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
            panelMain = new Panel();
            tableLayoutPanelButtom = new TableLayoutPanel();
            labelName = new Label();
            tableLayoutPanelLink = new TableLayoutPanel();
            linkLabelGitHub = new LinkLabel();
            linkLabelAgreement = new LinkLabel();
            linkLabelIssue = new LinkLabel();
            tableLayoutPanelSoftwareVersion = new TableLayoutPanel();
            labelSoftwareVersion = new Label();
            labelSoftwareVersionValue = new Label();
            tableLayoutPanelTop = new TableLayoutPanel();
            labelSoftwareName = new Label();
            panelMain.SuspendLayout();
            tableLayoutPanelButtom.SuspendLayout();
            tableLayoutPanelLink.SuspendLayout();
            tableLayoutPanelSoftwareVersion.SuspendLayout();
            tableLayoutPanelTop.SuspendLayout();
            SuspendLayout();
            // 
            // panelMain
            // 
            panelMain.AutoSize = true;
            panelMain.Controls.Add(tableLayoutPanelButtom);
            panelMain.Controls.Add(tableLayoutPanelTop);
            panelMain.Dock = DockStyle.Fill;
            panelMain.Location = new Point(0, 0);
            panelMain.Name = "panelMain";
            panelMain.Size = new Size(878, 1017);
            panelMain.TabIndex = 0;
            // 
            // tableLayoutPanelButtom
            // 
            tableLayoutPanelButtom.ColumnCount = 1;
            tableLayoutPanelButtom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelButtom.Controls.Add(labelName, 0, 2);
            tableLayoutPanelButtom.Controls.Add(tableLayoutPanelLink, 0, 1);
            tableLayoutPanelButtom.Controls.Add(tableLayoutPanelSoftwareVersion, 0, 0);
            tableLayoutPanelButtom.Dock = DockStyle.Bottom;
            tableLayoutPanelButtom.Location = new Point(0, 867);
            tableLayoutPanelButtom.Name = "tableLayoutPanelButtom";
            tableLayoutPanelButtom.Padding = new Padding(5);
            tableLayoutPanelButtom.RowCount = 3;
            tableLayoutPanelButtom.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanelButtom.RowStyles.Add(new RowStyle(SizeType.Percent, 50.0000076F));
            tableLayoutPanelButtom.RowStyles.Add(new RowStyle());
            tableLayoutPanelButtom.Size = new Size(878, 150);
            tableLayoutPanelButtom.TabIndex = 1;
            // 
            // labelName
            // 
            labelName.AutoSize = true;
            labelName.Dock = DockStyle.Fill;
            labelName.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            labelName.Location = new Point(8, 119);
            labelName.Name = "labelName";
            labelName.Size = new Size(862, 26);
            labelName.TabIndex = 0;
            labelName.Text = "@GSpace";
            labelName.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tableLayoutPanelLink
            // 
            tableLayoutPanelLink.ColumnCount = 3;
            tableLayoutPanelLink.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanelLink.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanelLink.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanelLink.Controls.Add(linkLabelGitHub, 0, 0);
            tableLayoutPanelLink.Controls.Add(linkLabelAgreement, 1, 0);
            tableLayoutPanelLink.Controls.Add(linkLabelIssue, 2, 0);
            tableLayoutPanelLink.Dock = DockStyle.Fill;
            tableLayoutPanelLink.Location = new Point(8, 65);
            tableLayoutPanelLink.Name = "tableLayoutPanelLink";
            tableLayoutPanelLink.RowCount = 1;
            tableLayoutPanelLink.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelLink.Size = new Size(862, 51);
            tableLayoutPanelLink.TabIndex = 3;
            // 
            // linkLabelGitHub
            // 
            linkLabelGitHub.AutoSize = true;
            linkLabelGitHub.Dock = DockStyle.Fill;
            linkLabelGitHub.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            linkLabelGitHub.LinkColor = Color.Black;
            linkLabelGitHub.Location = new Point(3, 0);
            linkLabelGitHub.Name = "linkLabelGitHub";
            linkLabelGitHub.Size = new Size(281, 51);
            linkLabelGitHub.TabIndex = 0;
            linkLabelGitHub.TabStop = true;
            linkLabelGitHub.Text = "GitHub 仓库";
            linkLabelGitHub.TextAlign = ContentAlignment.MiddleCenter;
            linkLabelGitHub.VisitedLinkColor = Color.Black;
            // 
            // linkLabelAgreement
            // 
            linkLabelAgreement.AutoSize = true;
            linkLabelAgreement.Dock = DockStyle.Fill;
            linkLabelAgreement.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            linkLabelAgreement.LinkColor = Color.Black;
            linkLabelAgreement.Location = new Point(290, 0);
            linkLabelAgreement.Name = "linkLabelAgreement";
            linkLabelAgreement.Size = new Size(281, 51);
            linkLabelAgreement.TabIndex = 1;
            linkLabelAgreement.TabStop = true;
            linkLabelAgreement.Text = "开源协议 (GitHub)";
            linkLabelAgreement.TextAlign = ContentAlignment.MiddleCenter;
            linkLabelAgreement.VisitedLinkColor = Color.Black;
            // 
            // linkLabelIssue
            // 
            linkLabelIssue.AutoSize = true;
            linkLabelIssue.Dock = DockStyle.Fill;
            linkLabelIssue.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            linkLabelIssue.LinkColor = Color.Black;
            linkLabelIssue.Location = new Point(577, 0);
            linkLabelIssue.Name = "linkLabelIssue";
            linkLabelIssue.Size = new Size(282, 51);
            linkLabelIssue.TabIndex = 2;
            linkLabelIssue.TabStop = true;
            linkLabelIssue.Text = "问题反馈 (GitHub)";
            linkLabelIssue.TextAlign = ContentAlignment.MiddleCenter;
            linkLabelIssue.VisitedLinkColor = Color.Black;
            // 
            // tableLayoutPanelSoftwareVersion
            // 
            tableLayoutPanelSoftwareVersion.ColumnCount = 2;
            tableLayoutPanelSoftwareVersion.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanelSoftwareVersion.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanelSoftwareVersion.Controls.Add(labelSoftwareVersion, 0, 0);
            tableLayoutPanelSoftwareVersion.Controls.Add(labelSoftwareVersionValue, 1, 0);
            tableLayoutPanelSoftwareVersion.Dock = DockStyle.Fill;
            tableLayoutPanelSoftwareVersion.Location = new Point(8, 8);
            tableLayoutPanelSoftwareVersion.Name = "tableLayoutPanelSoftwareVersion";
            tableLayoutPanelSoftwareVersion.RowCount = 1;
            tableLayoutPanelSoftwareVersion.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            tableLayoutPanelSoftwareVersion.Size = new Size(862, 51);
            tableLayoutPanelSoftwareVersion.TabIndex = 4;
            // 
            // labelSoftwareVersion
            // 
            labelSoftwareVersion.AutoSize = true;
            labelSoftwareVersion.Dock = DockStyle.Fill;
            labelSoftwareVersion.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            labelSoftwareVersion.Location = new Point(3, 0);
            labelSoftwareVersion.Name = "labelSoftwareVersion";
            labelSoftwareVersion.Size = new Size(425, 51);
            labelSoftwareVersion.TabIndex = 0;
            labelSoftwareVersion.Text = "软件版本 :";
            labelSoftwareVersion.TextAlign = ContentAlignment.MiddleRight;
            // 
            // labelSoftwareVersionValue
            // 
            labelSoftwareVersionValue.AutoSize = true;
            labelSoftwareVersionValue.Dock = DockStyle.Fill;
            labelSoftwareVersionValue.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            labelSoftwareVersionValue.Location = new Point(434, 0);
            labelSoftwareVersionValue.Name = "labelSoftwareVersionValue";
            labelSoftwareVersionValue.Size = new Size(425, 51);
            labelSoftwareVersionValue.TabIndex = 1;
            labelSoftwareVersionValue.Text = "NaN";
            labelSoftwareVersionValue.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // tableLayoutPanelTop
            // 
            tableLayoutPanelTop.ColumnCount = 1;
            tableLayoutPanelTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanelTop.Controls.Add(labelSoftwareName, 0, 0);
            tableLayoutPanelTop.Dock = DockStyle.Top;
            tableLayoutPanelTop.Location = new Point(0, 0);
            tableLayoutPanelTop.Name = "tableLayoutPanelTop";
            tableLayoutPanelTop.RowCount = 1;
            tableLayoutPanelTop.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelTop.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanelTop.Size = new Size(878, 150);
            tableLayoutPanelTop.TabIndex = 0;
            // 
            // labelSoftwareName
            // 
            labelSoftwareName.AutoSize = true;
            labelSoftwareName.Dock = DockStyle.Fill;
            labelSoftwareName.Font = new Font("Microsoft YaHei UI", 35F, FontStyle.Bold);
            labelSoftwareName.Location = new Point(3, 0);
            labelSoftwareName.Name = "labelSoftwareName";
            labelSoftwareName.Padding = new Padding(10);
            labelSoftwareName.Size = new Size(872, 150);
            labelSoftwareName.TabIndex = 0;
            labelSoftwareName.Text = "HMOS_WareToolBox";
            labelSoftwareName.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // AboutControl
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(panelMain);
            Name = "AboutControl";
            Size = new Size(878, 1017);
            panelMain.ResumeLayout(false);
            tableLayoutPanelButtom.ResumeLayout(false);
            tableLayoutPanelButtom.PerformLayout();
            tableLayoutPanelLink.ResumeLayout(false);
            tableLayoutPanelLink.PerformLayout();
            tableLayoutPanelSoftwareVersion.ResumeLayout(false);
            tableLayoutPanelSoftwareVersion.PerformLayout();
            tableLayoutPanelTop.ResumeLayout(false);
            tableLayoutPanelTop.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panelMain;
        private TableLayoutPanel tableLayoutPanelTop;
        private Label labelSoftwareName;
        private TableLayoutPanel tableLayoutPanelButtom;
        private Label labelName;
        private TableLayoutPanel tableLayoutPanelLink;
        private LinkLabel linkLabelGitHub;
        private LinkLabel linkLabelAgreement;
        private LinkLabel linkLabelIssue;
        private TableLayoutPanel tableLayoutPanelSoftwareVersion;
        private Label labelSoftwareVersion;
        private Label labelSoftwareVersionValue;
    }
}
