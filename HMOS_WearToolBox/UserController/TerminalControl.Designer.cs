namespace HMOS_WearToolBox.UserController
{
    partial class TerminalControl
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
            richTextBoxTerminal = new RichTextBox();
            SuspendLayout();
            // 
            // richTextBoxTerminal
            // 
            richTextBoxTerminal.BackColor = Color.Black;
            richTextBoxTerminal.Dock = DockStyle.Fill;
            richTextBoxTerminal.Font = new Font("Consolas", 12F);
            richTextBoxTerminal.ForeColor = Color.White;
            richTextBoxTerminal.Location = new Point(0, 0);
            richTextBoxTerminal.Name = "richTextBoxTerminal";
            richTextBoxTerminal.Size = new Size(878, 1017);
            richTextBoxTerminal.TabIndex = 0;
            richTextBoxTerminal.Text = "";
            // 
            // TerminalControl
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(richTextBoxTerminal);
            Name = "TerminalControl";
            Size = new Size(878, 1017);
            ResumeLayout(false);
        }

        #endregion

        private RichTextBox richTextBoxTerminal;
    }
}
