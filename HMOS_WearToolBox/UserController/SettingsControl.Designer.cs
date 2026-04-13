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
            panelSave = new Panel();
            btnSave = new Button();
            groupBoxTerminal = new GroupBox();
            panelRestoreDefault = new Panel();
            btnRestoreDefault = new Button();
            tableLayoutPanelTerminalSetting = new TableLayoutPanel();
            labelFont = new Label();
            labelFontSize = new Label();
            labelFontColor = new Label();
            labelTerminalBackgroundColor = new Label();
            comboBoxFont = new ComboBox();
            numericUpDownFontSize = new NumericUpDown();
            tableLayoutPanelChooseFontColor = new TableLayoutPanel();
            buttonChooseFontColor = new Button();
            panelShowFontColor = new Panel();
            tableLayoutPanelChooseTerminalColor = new TableLayoutPanel();
            buttonChooseTerminalBackgroundColor = new Button();
            panelShowTerminalBackgroundColor = new Panel();
            groupBoxGlobal = new GroupBox();
            tableLayoutPanelGlobalSetting = new TableLayoutPanel();
            labelSetAutoUpdateTime = new Label();
            panelTimeSetting = new Panel();
            tableLayoutPanelTimeSetting = new TableLayoutPanel();
            numericUpDownAutoUpdateTime = new NumericUpDown();
            btnAutoUpdateControl = new Button();
            labelGlobalFont = new Label();
            tableLayoutPanelSoftwareFontSetting = new TableLayoutPanel();
            comboBoxGlobalFont = new ComboBox();
            btnGlobalFontRestoreDefault = new Button();
            colorDialog1 = new ColorDialog();
            panelMain.SuspendLayout();
            panelSave.SuspendLayout();
            groupBoxTerminal.SuspendLayout();
            panelRestoreDefault.SuspendLayout();
            tableLayoutPanelTerminalSetting.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDownFontSize).BeginInit();
            tableLayoutPanelChooseFontColor.SuspendLayout();
            tableLayoutPanelChooseTerminalColor.SuspendLayout();
            groupBoxGlobal.SuspendLayout();
            tableLayoutPanelGlobalSetting.SuspendLayout();
            panelTimeSetting.SuspendLayout();
            tableLayoutPanelTimeSetting.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDownAutoUpdateTime).BeginInit();
            tableLayoutPanelSoftwareFontSetting.SuspendLayout();
            SuspendLayout();
            // 
            // panelMain
            // 
            panelMain.AutoSize = true;
            panelMain.Controls.Add(panelSave);
            panelMain.Controls.Add(groupBoxTerminal);
            panelMain.Controls.Add(groupBoxGlobal);
            panelMain.Dock = DockStyle.Fill;
            panelMain.Location = new Point(0, 0);
            panelMain.Name = "panelMain";
            panelMain.Size = new Size(878, 1017);
            panelMain.TabIndex = 0;
            // 
            // panelSave
            // 
            panelSave.AutoSize = true;
            panelSave.Controls.Add(btnSave);
            panelSave.Dock = DockStyle.Bottom;
            panelSave.Location = new Point(0, 956);
            panelSave.Name = "panelSave";
            panelSave.Padding = new Padding(10);
            panelSave.Size = new Size(878, 61);
            panelSave.TabIndex = 2;
            // 
            // btnSave
            // 
            btnSave.AutoSize = true;
            btnSave.Dock = DockStyle.Fill;
            btnSave.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold);
            btnSave.Location = new Point(10, 10);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(858, 41);
            btnSave.TabIndex = 1;
            btnSave.Text = "保存";
            btnSave.UseVisualStyleBackColor = true;
            // 
            // groupBoxTerminal
            // 
            groupBoxTerminal.AutoSize = true;
            groupBoxTerminal.Controls.Add(panelRestoreDefault);
            groupBoxTerminal.Controls.Add(tableLayoutPanelTerminalSetting);
            groupBoxTerminal.Dock = DockStyle.Top;
            groupBoxTerminal.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold);
            groupBoxTerminal.Location = new Point(0, 155);
            groupBoxTerminal.Name = "groupBoxTerminal";
            groupBoxTerminal.Size = new Size(878, 298);
            groupBoxTerminal.TabIndex = 1;
            groupBoxTerminal.TabStop = false;
            groupBoxTerminal.Text = "HDC 终端设置";
            // 
            // panelRestoreDefault
            // 
            panelRestoreDefault.AutoSize = true;
            panelRestoreDefault.Controls.Add(btnRestoreDefault);
            panelRestoreDefault.Dock = DockStyle.Top;
            panelRestoreDefault.Location = new Point(3, 234);
            panelRestoreDefault.Name = "panelRestoreDefault";
            panelRestoreDefault.Padding = new Padding(10);
            panelRestoreDefault.Size = new Size(872, 61);
            panelRestoreDefault.TabIndex = 2;
            // 
            // btnRestoreDefault
            // 
            btnRestoreDefault.AutoSize = true;
            btnRestoreDefault.Dock = DockStyle.Fill;
            btnRestoreDefault.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold);
            btnRestoreDefault.Location = new Point(10, 10);
            btnRestoreDefault.Name = "btnRestoreDefault";
            btnRestoreDefault.Size = new Size(852, 41);
            btnRestoreDefault.TabIndex = 1;
            btnRestoreDefault.Text = "恢复默认设置";
            btnRestoreDefault.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanelTerminalSetting
            // 
            tableLayoutPanelTerminalSetting.AutoSize = true;
            tableLayoutPanelTerminalSetting.ColumnCount = 2;
            tableLayoutPanelTerminalSetting.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanelTerminalSetting.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanelTerminalSetting.Controls.Add(labelFont, 0, 0);
            tableLayoutPanelTerminalSetting.Controls.Add(labelFontSize, 0, 1);
            tableLayoutPanelTerminalSetting.Controls.Add(labelFontColor, 0, 2);
            tableLayoutPanelTerminalSetting.Controls.Add(labelTerminalBackgroundColor, 0, 3);
            tableLayoutPanelTerminalSetting.Controls.Add(comboBoxFont, 1, 0);
            tableLayoutPanelTerminalSetting.Controls.Add(numericUpDownFontSize, 1, 1);
            tableLayoutPanelTerminalSetting.Controls.Add(tableLayoutPanelChooseFontColor, 1, 2);
            tableLayoutPanelTerminalSetting.Controls.Add(tableLayoutPanelChooseTerminalColor, 1, 3);
            tableLayoutPanelTerminalSetting.Dock = DockStyle.Top;
            tableLayoutPanelTerminalSetting.Font = new Font("Microsoft YaHei UI", 10F);
            tableLayoutPanelTerminalSetting.Location = new Point(3, 34);
            tableLayoutPanelTerminalSetting.Name = "tableLayoutPanelTerminalSetting";
            tableLayoutPanelTerminalSetting.Padding = new Padding(10);
            tableLayoutPanelTerminalSetting.RowCount = 4;
            tableLayoutPanelTerminalSetting.RowStyles.Add(new RowStyle());
            tableLayoutPanelTerminalSetting.RowStyles.Add(new RowStyle());
            tableLayoutPanelTerminalSetting.RowStyles.Add(new RowStyle());
            tableLayoutPanelTerminalSetting.RowStyles.Add(new RowStyle());
            tableLayoutPanelTerminalSetting.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanelTerminalSetting.Size = new Size(872, 200);
            tableLayoutPanelTerminalSetting.TabIndex = 0;
            // 
            // labelFont
            // 
            labelFont.AutoSize = true;
            labelFont.Dock = DockStyle.Fill;
            labelFont.Font = new Font("Microsoft YaHei UI", 12F);
            labelFont.Location = new Point(13, 10);
            labelFont.Name = "labelFont";
            labelFont.Padding = new Padding(5);
            labelFont.Size = new Size(420, 41);
            labelFont.TabIndex = 0;
            labelFont.Text = "字体";
            labelFont.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // labelFontSize
            // 
            labelFontSize.AutoSize = true;
            labelFontSize.Dock = DockStyle.Fill;
            labelFontSize.Font = new Font("Microsoft YaHei UI", 12F);
            labelFontSize.Location = new Point(13, 51);
            labelFontSize.Name = "labelFontSize";
            labelFontSize.Padding = new Padding(5);
            labelFontSize.Size = new Size(420, 41);
            labelFontSize.TabIndex = 1;
            labelFontSize.Text = "字体大小";
            labelFontSize.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // labelFontColor
            // 
            labelFontColor.AutoSize = true;
            labelFontColor.Dock = DockStyle.Fill;
            labelFontColor.Font = new Font("Microsoft YaHei UI", 12F);
            labelFontColor.Location = new Point(13, 92);
            labelFontColor.Name = "labelFontColor";
            labelFontColor.Padding = new Padding(5);
            labelFontColor.Size = new Size(420, 49);
            labelFontColor.TabIndex = 2;
            labelFontColor.Text = "字体颜色";
            labelFontColor.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // labelTerminalBackgroundColor
            // 
            labelTerminalBackgroundColor.AutoSize = true;
            labelTerminalBackgroundColor.Dock = DockStyle.Fill;
            labelTerminalBackgroundColor.Font = new Font("Microsoft YaHei UI", 12F);
            labelTerminalBackgroundColor.Location = new Point(13, 141);
            labelTerminalBackgroundColor.Name = "labelTerminalBackgroundColor";
            labelTerminalBackgroundColor.Padding = new Padding(5);
            labelTerminalBackgroundColor.Size = new Size(420, 49);
            labelTerminalBackgroundColor.TabIndex = 3;
            labelTerminalBackgroundColor.Text = "终端背景色";
            labelTerminalBackgroundColor.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // comboBoxFont
            // 
            comboBoxFont.Dock = DockStyle.Fill;
            comboBoxFont.Font = new Font("Microsoft YaHei UI", 10F);
            comboBoxFont.FormattingEnabled = true;
            comboBoxFont.Location = new Point(439, 13);
            comboBoxFont.Name = "comboBoxFont";
            comboBoxFont.Size = new Size(420, 35);
            comboBoxFont.TabIndex = 4;
            // 
            // numericUpDownFontSize
            // 
            numericUpDownFontSize.Dock = DockStyle.Fill;
            numericUpDownFontSize.Font = new Font("Microsoft YaHei UI", 10F);
            numericUpDownFontSize.Location = new Point(439, 54);
            numericUpDownFontSize.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            numericUpDownFontSize.Name = "numericUpDownFontSize";
            numericUpDownFontSize.Size = new Size(420, 33);
            numericUpDownFontSize.TabIndex = 5;
            numericUpDownFontSize.TextAlign = HorizontalAlignment.Center;
            numericUpDownFontSize.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // tableLayoutPanelChooseFontColor
            // 
            tableLayoutPanelChooseFontColor.AutoSize = true;
            tableLayoutPanelChooseFontColor.ColumnCount = 2;
            tableLayoutPanelChooseFontColor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelChooseFontColor.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanelChooseFontColor.Controls.Add(buttonChooseFontColor, 1, 0);
            tableLayoutPanelChooseFontColor.Controls.Add(panelShowFontColor, 0, 0);
            tableLayoutPanelChooseFontColor.Dock = DockStyle.Fill;
            tableLayoutPanelChooseFontColor.Location = new Point(439, 95);
            tableLayoutPanelChooseFontColor.Name = "tableLayoutPanelChooseFontColor";
            tableLayoutPanelChooseFontColor.RowCount = 1;
            tableLayoutPanelChooseFontColor.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelChooseFontColor.Size = new Size(420, 43);
            tableLayoutPanelChooseFontColor.TabIndex = 6;
            // 
            // buttonChooseFontColor
            // 
            buttonChooseFontColor.AutoSize = true;
            buttonChooseFontColor.Font = new Font("Microsoft YaHei UI", 10F);
            buttonChooseFontColor.Location = new Point(305, 3);
            buttonChooseFontColor.Name = "buttonChooseFontColor";
            buttonChooseFontColor.Size = new Size(112, 37);
            buttonChooseFontColor.TabIndex = 0;
            buttonChooseFontColor.Text = "选择颜色";
            buttonChooseFontColor.UseVisualStyleBackColor = true;
            // 
            // panelShowFontColor
            // 
            panelShowFontColor.AutoSize = true;
            panelShowFontColor.Dock = DockStyle.Fill;
            panelShowFontColor.Location = new Point(3, 3);
            panelShowFontColor.Name = "panelShowFontColor";
            panelShowFontColor.Size = new Size(296, 37);
            panelShowFontColor.TabIndex = 1;
            // 
            // tableLayoutPanelChooseTerminalColor
            // 
            tableLayoutPanelChooseTerminalColor.AutoSize = true;
            tableLayoutPanelChooseTerminalColor.ColumnCount = 2;
            tableLayoutPanelChooseTerminalColor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelChooseTerminalColor.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanelChooseTerminalColor.Controls.Add(buttonChooseTerminalBackgroundColor, 1, 0);
            tableLayoutPanelChooseTerminalColor.Controls.Add(panelShowTerminalBackgroundColor, 0, 0);
            tableLayoutPanelChooseTerminalColor.Dock = DockStyle.Fill;
            tableLayoutPanelChooseTerminalColor.Location = new Point(439, 144);
            tableLayoutPanelChooseTerminalColor.Name = "tableLayoutPanelChooseTerminalColor";
            tableLayoutPanelChooseTerminalColor.RowCount = 1;
            tableLayoutPanelChooseTerminalColor.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelChooseTerminalColor.Size = new Size(420, 43);
            tableLayoutPanelChooseTerminalColor.TabIndex = 7;
            // 
            // buttonChooseTerminalBackgroundColor
            // 
            buttonChooseTerminalBackgroundColor.AutoSize = true;
            buttonChooseTerminalBackgroundColor.Font = new Font("Microsoft YaHei UI", 10F);
            buttonChooseTerminalBackgroundColor.Location = new Point(305, 3);
            buttonChooseTerminalBackgroundColor.Name = "buttonChooseTerminalBackgroundColor";
            buttonChooseTerminalBackgroundColor.Size = new Size(112, 37);
            buttonChooseTerminalBackgroundColor.TabIndex = 0;
            buttonChooseTerminalBackgroundColor.Text = "选择颜色";
            buttonChooseTerminalBackgroundColor.UseVisualStyleBackColor = true;
            // 
            // panelShowTerminalBackgroundColor
            // 
            panelShowTerminalBackgroundColor.AutoSize = true;
            panelShowTerminalBackgroundColor.Dock = DockStyle.Fill;
            panelShowTerminalBackgroundColor.Location = new Point(3, 3);
            panelShowTerminalBackgroundColor.Name = "panelShowTerminalBackgroundColor";
            panelShowTerminalBackgroundColor.Size = new Size(296, 37);
            panelShowTerminalBackgroundColor.TabIndex = 0;
            // 
            // groupBoxGlobal
            // 
            groupBoxGlobal.AutoSize = true;
            groupBoxGlobal.Controls.Add(tableLayoutPanelGlobalSetting);
            groupBoxGlobal.Dock = DockStyle.Top;
            groupBoxGlobal.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold);
            groupBoxGlobal.Location = new Point(0, 0);
            groupBoxGlobal.Name = "groupBoxGlobal";
            groupBoxGlobal.Size = new Size(878, 155);
            groupBoxGlobal.TabIndex = 0;
            groupBoxGlobal.TabStop = false;
            groupBoxGlobal.Text = "软件全局设置";
            // 
            // tableLayoutPanelGlobalSetting
            // 
            tableLayoutPanelGlobalSetting.AutoSize = true;
            tableLayoutPanelGlobalSetting.ColumnCount = 2;
            tableLayoutPanelGlobalSetting.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanelGlobalSetting.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tableLayoutPanelGlobalSetting.Controls.Add(labelSetAutoUpdateTime, 0, 0);
            tableLayoutPanelGlobalSetting.Controls.Add(panelTimeSetting, 1, 0);
            tableLayoutPanelGlobalSetting.Controls.Add(labelGlobalFont, 0, 1);
            tableLayoutPanelGlobalSetting.Controls.Add(tableLayoutPanelSoftwareFontSetting, 1, 1);
            tableLayoutPanelGlobalSetting.Dock = DockStyle.Fill;
            tableLayoutPanelGlobalSetting.Font = new Font("Microsoft YaHei UI", 10F);
            tableLayoutPanelGlobalSetting.Location = new Point(3, 34);
            tableLayoutPanelGlobalSetting.Name = "tableLayoutPanelGlobalSetting";
            tableLayoutPanelGlobalSetting.Padding = new Padding(10);
            tableLayoutPanelGlobalSetting.RowCount = 3;
            tableLayoutPanelGlobalSetting.RowStyles.Add(new RowStyle());
            tableLayoutPanelGlobalSetting.RowStyles.Add(new RowStyle());
            tableLayoutPanelGlobalSetting.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelGlobalSetting.Size = new Size(872, 118);
            tableLayoutPanelGlobalSetting.TabIndex = 0;
            // 
            // labelSetAutoUpdateTime
            // 
            labelSetAutoUpdateTime.AutoSize = true;
            labelSetAutoUpdateTime.Dock = DockStyle.Fill;
            labelSetAutoUpdateTime.Font = new Font("Microsoft YaHei UI", 12F);
            labelSetAutoUpdateTime.Location = new Point(13, 10);
            labelSetAutoUpdateTime.Name = "labelSetAutoUpdateTime";
            labelSetAutoUpdateTime.Padding = new Padding(5);
            labelSetAutoUpdateTime.Size = new Size(420, 49);
            labelSetAutoUpdateTime.TabIndex = 0;
            labelSetAutoUpdateTime.Text = "自动更新数据时间 (30s-600s)";
            labelSetAutoUpdateTime.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panelTimeSetting
            // 
            panelTimeSetting.AutoSize = true;
            panelTimeSetting.Controls.Add(tableLayoutPanelTimeSetting);
            panelTimeSetting.Dock = DockStyle.Fill;
            panelTimeSetting.Location = new Point(439, 13);
            panelTimeSetting.Name = "panelTimeSetting";
            panelTimeSetting.Size = new Size(420, 43);
            panelTimeSetting.TabIndex = 1;
            // 
            // tableLayoutPanelTimeSetting
            // 
            tableLayoutPanelTimeSetting.AutoSize = true;
            tableLayoutPanelTimeSetting.ColumnCount = 2;
            tableLayoutPanelTimeSetting.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelTimeSetting.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanelTimeSetting.Controls.Add(numericUpDownAutoUpdateTime, 0, 0);
            tableLayoutPanelTimeSetting.Controls.Add(btnAutoUpdateControl, 1, 0);
            tableLayoutPanelTimeSetting.Dock = DockStyle.Fill;
            tableLayoutPanelTimeSetting.Location = new Point(0, 0);
            tableLayoutPanelTimeSetting.Name = "tableLayoutPanelTimeSetting";
            tableLayoutPanelTimeSetting.RowCount = 1;
            tableLayoutPanelTimeSetting.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelTimeSetting.Size = new Size(420, 43);
            tableLayoutPanelTimeSetting.TabIndex = 0;
            // 
            // numericUpDownAutoUpdateTime
            // 
            numericUpDownAutoUpdateTime.Dock = DockStyle.Fill;
            numericUpDownAutoUpdateTime.Font = new Font("Microsoft YaHei UI", 10F);
            numericUpDownAutoUpdateTime.Location = new Point(3, 3);
            numericUpDownAutoUpdateTime.Maximum = new decimal(new int[] { 600, 0, 0, 0 });
            numericUpDownAutoUpdateTime.Minimum = new decimal(new int[] { 30, 0, 0, 0 });
            numericUpDownAutoUpdateTime.Name = "numericUpDownAutoUpdateTime";
            numericUpDownAutoUpdateTime.Size = new Size(328, 33);
            numericUpDownAutoUpdateTime.TabIndex = 0;
            numericUpDownAutoUpdateTime.TextAlign = HorizontalAlignment.Center;
            numericUpDownAutoUpdateTime.Value = new decimal(new int[] { 30, 0, 0, 0 });
            // 
            // btnAutoUpdateControl
            // 
            btnAutoUpdateControl.AutoSize = true;
            btnAutoUpdateControl.Font = new Font("Microsoft YaHei UI", 10F);
            btnAutoUpdateControl.Location = new Point(337, 3);
            btnAutoUpdateControl.Name = "btnAutoUpdateControl";
            btnAutoUpdateControl.Size = new Size(80, 37);
            btnAutoUpdateControl.TabIndex = 1;
            btnAutoUpdateControl.Text = "开";
            btnAutoUpdateControl.UseVisualStyleBackColor = true;
            // 
            // labelGlobalFont
            // 
            labelGlobalFont.AutoSize = true;
            labelGlobalFont.Dock = DockStyle.Fill;
            labelGlobalFont.Font = new Font("Microsoft YaHei UI", 12F);
            labelGlobalFont.Location = new Point(13, 59);
            labelGlobalFont.Name = "labelGlobalFont";
            labelGlobalFont.Size = new Size(420, 49);
            labelGlobalFont.TabIndex = 2;
            labelGlobalFont.Text = "软件字体";
            labelGlobalFont.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tableLayoutPanelSoftwareFontSetting
            // 
            tableLayoutPanelSoftwareFontSetting.AutoSize = true;
            tableLayoutPanelSoftwareFontSetting.ColumnCount = 2;
            tableLayoutPanelSoftwareFontSetting.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelSoftwareFontSetting.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanelSoftwareFontSetting.Controls.Add(comboBoxGlobalFont, 0, 0);
            tableLayoutPanelSoftwareFontSetting.Controls.Add(btnGlobalFontRestoreDefault, 1, 0);
            tableLayoutPanelSoftwareFontSetting.Dock = DockStyle.Fill;
            tableLayoutPanelSoftwareFontSetting.Location = new Point(439, 62);
            tableLayoutPanelSoftwareFontSetting.Name = "tableLayoutPanelSoftwareFontSetting";
            tableLayoutPanelSoftwareFontSetting.RowCount = 1;
            tableLayoutPanelSoftwareFontSetting.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelSoftwareFontSetting.Size = new Size(420, 43);
            tableLayoutPanelSoftwareFontSetting.TabIndex = 3;
            // 
            // comboBoxGlobalFont
            // 
            comboBoxGlobalFont.Dock = DockStyle.Fill;
            comboBoxGlobalFont.Font = new Font("Microsoft YaHei UI", 10F);
            comboBoxGlobalFont.FormattingEnabled = true;
            comboBoxGlobalFont.Location = new Point(3, 3);
            comboBoxGlobalFont.Name = "comboBoxGlobalFont";
            comboBoxGlobalFont.Size = new Size(296, 35);
            comboBoxGlobalFont.TabIndex = 0;
            // 
            // btnGlobalFontRestoreDefault
            // 
            btnGlobalFontRestoreDefault.AutoSize = true;
            btnGlobalFontRestoreDefault.Font = new Font("Microsoft YaHei UI", 10F);
            btnGlobalFontRestoreDefault.Location = new Point(305, 3);
            btnGlobalFontRestoreDefault.Name = "btnGlobalFontRestoreDefault";
            btnGlobalFontRestoreDefault.Size = new Size(112, 37);
            btnGlobalFontRestoreDefault.TabIndex = 1;
            btnGlobalFontRestoreDefault.Text = "恢复默认";
            btnGlobalFontRestoreDefault.UseVisualStyleBackColor = true;
            // 
            // SettingsControl
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(panelMain);
            Name = "SettingsControl";
            Size = new Size(878, 1017);
            panelMain.ResumeLayout(false);
            panelMain.PerformLayout();
            panelSave.ResumeLayout(false);
            panelSave.PerformLayout();
            groupBoxTerminal.ResumeLayout(false);
            groupBoxTerminal.PerformLayout();
            panelRestoreDefault.ResumeLayout(false);
            panelRestoreDefault.PerformLayout();
            tableLayoutPanelTerminalSetting.ResumeLayout(false);
            tableLayoutPanelTerminalSetting.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDownFontSize).EndInit();
            tableLayoutPanelChooseFontColor.ResumeLayout(false);
            tableLayoutPanelChooseFontColor.PerformLayout();
            tableLayoutPanelChooseTerminalColor.ResumeLayout(false);
            tableLayoutPanelChooseTerminalColor.PerformLayout();
            groupBoxGlobal.ResumeLayout(false);
            groupBoxGlobal.PerformLayout();
            tableLayoutPanelGlobalSetting.ResumeLayout(false);
            tableLayoutPanelGlobalSetting.PerformLayout();
            panelTimeSetting.ResumeLayout(false);
            panelTimeSetting.PerformLayout();
            tableLayoutPanelTimeSetting.ResumeLayout(false);
            tableLayoutPanelTimeSetting.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDownAutoUpdateTime).EndInit();
            tableLayoutPanelSoftwareFontSetting.ResumeLayout(false);
            tableLayoutPanelSoftwareFontSetting.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panelMain;
        private GroupBox groupBoxGlobal;
        private GroupBox groupBoxTerminal;
        private TableLayoutPanel tableLayoutPanelGlobalSetting;
        private Label labelSetAutoUpdateTime;
        private Panel panelTimeSetting;
        private Button btnSave;
        private Panel panelSave;
        private TableLayoutPanel tableLayoutPanelTimeSetting;
        private NumericUpDown numericUpDownAutoUpdateTime;
        private Button btnAutoUpdateControl;
        private TableLayoutPanel tableLayoutPanelTerminalSetting;
        private Label labelFont;
        private Label labelFontSize;
        private Label labelFontColor;
        private Label labelTerminalBackgroundColor;
        private ComboBox comboBoxFont;
        private NumericUpDown numericUpDownFontSize;
        private ColorDialog colorDialog1;
        private TableLayoutPanel tableLayoutPanelChooseFontColor;
        private Button buttonChooseFontColor;
        private Panel panelShowFontColor;
        private TableLayoutPanel tableLayoutPanelChooseTerminalColor;
        private Button buttonChooseTerminalBackgroundColor;
        private Panel panelShowTerminalBackgroundColor;
        private Button btnRestoreDefault;
        private Panel panelRestoreDefault;
        private Label labelGlobalFont;
        private TableLayoutPanel tableLayoutPanelSoftwareFontSetting;
        private ComboBox comboBoxGlobalFont;
        private Button btnGlobalFontRestoreDefault;
    }
}
