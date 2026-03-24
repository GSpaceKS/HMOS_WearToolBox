namespace HMOS_WearToolBox
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            navflowLayoutPanel = new FlowLayoutPanel();
            btnHome = new Button();
            btnSoftware = new Button();
            btnTerminal = new Button();
            btnSettings = new Button();
            btnAbout = new Button();
            contentPanel = new Panel();
            navflowLayoutPanel.SuspendLayout();
            SuspendLayout();
            // 
            // navflowLayoutPanel
            // 
            navflowLayoutPanel.AutoSize = true;
            navflowLayoutPanel.Controls.Add(btnHome);
            navflowLayoutPanel.Controls.Add(btnSoftware);
            navflowLayoutPanel.Controls.Add(btnTerminal);
            navflowLayoutPanel.Controls.Add(btnSettings);
            navflowLayoutPanel.Controls.Add(btnAbout);
            navflowLayoutPanel.Dock = DockStyle.Top;
            navflowLayoutPanel.Location = new Point(0, 0);
            navflowLayoutPanel.Name = "navflowLayoutPanel";
            navflowLayoutPanel.Size = new Size(878, 40);
            navflowLayoutPanel.TabIndex = 0;
            // 
            // btnHome
            // 
            btnHome.AutoSize = true;
            btnHome.Font = new Font("Microsoft YaHei UI", 9F);
            btnHome.Location = new Point(3, 3);
            btnHome.Name = "btnHome";
            btnHome.Size = new Size(80, 34);
            btnHome.TabIndex = 0;
            btnHome.Text = "主页";
            btnHome.UseVisualStyleBackColor = true;
            // 
            // btnSoftware
            // 
            btnSoftware.AutoSize = true;
            btnSoftware.Font = new Font("Microsoft YaHei UI", 9F);
            btnSoftware.Location = new Point(89, 3);
            btnSoftware.Name = "btnSoftware";
            btnSoftware.Size = new Size(100, 34);
            btnSoftware.TabIndex = 1;
            btnSoftware.Text = "软件管理";
            btnSoftware.UseVisualStyleBackColor = true;
            // 
            // btnTerminal
            // 
            btnTerminal.AutoSize = true;
            btnTerminal.Font = new Font("Microsoft YaHei UI", 9F);
            btnTerminal.Location = new Point(195, 3);
            btnTerminal.Name = "btnTerminal";
            btnTerminal.Size = new Size(101, 34);
            btnTerminal.TabIndex = 2;
            btnTerminal.Text = "HDC 终端";
            btnTerminal.UseVisualStyleBackColor = true;
            // 
            // btnSettings
            // 
            btnSettings.AutoSize = true;
            btnSettings.Font = new Font("Microsoft YaHei UI", 9F);
            btnSettings.Location = new Point(302, 3);
            btnSettings.Name = "btnSettings";
            btnSettings.Size = new Size(80, 34);
            btnSettings.TabIndex = 3;
            btnSettings.Text = "设置";
            btnSettings.UseVisualStyleBackColor = true;
            // 
            // btnAbout
            // 
            btnAbout.AutoSize = true;
            btnAbout.Font = new Font("Microsoft YaHei UI", 9F);
            btnAbout.Location = new Point(388, 3);
            btnAbout.Name = "btnAbout";
            btnAbout.Size = new Size(80, 34);
            btnAbout.TabIndex = 4;
            btnAbout.Text = "关于";
            btnAbout.UseVisualStyleBackColor = true;
            // 
            // contentPanel
            // 
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.Location = new Point(0, 40);
            contentPanel.Name = "contentPanel";
            contentPanel.Size = new Size(878, 1004);
            contentPanel.TabIndex = 1;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(144F, 144F);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(878, 1044);
            Controls.Add(contentPanel);
            Controls.Add(navflowLayoutPanel);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MinimumSize = new Size(900, 1100);
            Name = "MainForm";
            Text = "鸿蒙手表工具箱";
            Load += MainForm_Load;
            navflowLayoutPanel.ResumeLayout(false);
            navflowLayoutPanel.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private FlowLayoutPanel navflowLayoutPanel;
        private Button btnHome;
        private Button btnSoftware;
        private Button btnTerminal;
        private Button btnSettings;
        private Button btnAbout;
        private Panel contentPanel;
    }
}