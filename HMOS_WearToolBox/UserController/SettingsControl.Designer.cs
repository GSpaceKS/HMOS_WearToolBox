namespace HMOS_WearToolBox.UserController
{
    partial class SettingsControl
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
            groupBoxMain = new GroupBox();
            groupBoxTerminal = new GroupBox();
            labelBlock_1 = new Label();
            labelBlock_2 = new Label();
            panelMain.SuspendLayout();
            groupBoxMain.SuspendLayout();
            groupBoxTerminal.SuspendLayout();
            SuspendLayout();
            // 
            // panelMain
            // 
            panelMain.AutoSize = true;
            panelMain.Controls.Add(groupBoxTerminal);
            panelMain.Controls.Add(groupBoxMain);
            panelMain.Dock = DockStyle.Fill;
            panelMain.Location = new Point(0, 0);
            panelMain.Name = "panelMain";
            panelMain.Size = new Size(878, 1017);
            panelMain.TabIndex = 0;
            // 
            // groupBoxMain
            // 
            groupBoxMain.Controls.Add(labelBlock_1);
            groupBoxMain.Dock = DockStyle.Top;
            groupBoxMain.Location = new Point(0, 0);
            groupBoxMain.Name = "groupBoxMain";
            groupBoxMain.Size = new Size(878, 150);
            groupBoxMain.TabIndex = 0;
            groupBoxMain.TabStop = false;
            groupBoxMain.Text = "软件全局设置";
            // 
            // groupBoxTerminal
            // 
            groupBoxTerminal.Controls.Add(labelBlock_2);
            groupBoxTerminal.Dock = DockStyle.Top;
            groupBoxTerminal.Location = new Point(0, 150);
            groupBoxTerminal.Name = "groupBoxTerminal";
            groupBoxTerminal.Size = new Size(878, 150);
            groupBoxTerminal.TabIndex = 1;
            groupBoxTerminal.TabStop = false;
            groupBoxTerminal.Text = "HDC 终端设置";
            // 
            // labelBlock_1
            // 
            labelBlock_1.Dock = DockStyle.Fill;
            labelBlock_1.Font = new Font("Microsoft YaHei UI", 25F, FontStyle.Bold);
            labelBlock_1.Location = new Point(3, 26);
            labelBlock_1.Name = "labelBlock_1";
            labelBlock_1.Size = new Size(872, 121);
            labelBlock_1.TabIndex = 0;
            labelBlock_1.Text = "正在施工...";
            labelBlock_1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // labelBlock_2
            // 
            labelBlock_2.Dock = DockStyle.Fill;
            labelBlock_2.Font = new Font("Microsoft YaHei UI", 25F, FontStyle.Bold);
            labelBlock_2.Location = new Point(3, 26);
            labelBlock_2.Name = "labelBlock_2";
            labelBlock_2.Size = new Size(872, 121);
            labelBlock_2.TabIndex = 0;
            labelBlock_2.Text = "正在施工...";
            labelBlock_2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // SettingsControl
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(panelMain);
            Name = "SettingsControl";
            Size = new Size(878, 1017);
            panelMain.ResumeLayout(false);
            groupBoxMain.ResumeLayout(false);
            groupBoxTerminal.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panelMain;
        private GroupBox groupBoxMain;
        private GroupBox groupBoxTerminal;
        private Label labelBlock_2;
        private Label labelBlock_1;
    }
}
