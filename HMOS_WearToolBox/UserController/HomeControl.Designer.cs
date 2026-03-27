namespace HMOS_WearToolBox.UserController
{
    partial class HomeControl
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
            flowLayoutPanelConnectControl = new FlowLayoutPanel();
            cmbDevices = new ComboBox();
            btnReconnect = new Button();
            btnDisconnect = new Button();
            btnAddDevice = new Button();
            btnDeleteDevice = new Button();
            tableLayoutPanelMain = new TableLayoutPanel();
            groupBox_StorageInfo = new GroupBox();
            tableLayoutPanel_StorageInfo = new TableLayoutPanel();
            InnertableLayoutPanel_StorageInfo = new TableLayoutPanel();
            lblTotalStorage = new Label();
            lblTotalStorageValue = new Label();
            lblUsedStorage = new Label();
            lblUsedStorageValue = new Label();
            lblFreeStorage = new Label();
            lblFreeStorageValue = new Label();
            pbStorageRing = new PictureBox();
            groupBox_BatteryInfo = new GroupBox();
            tableLayoutPanel_BatteryInfo = new TableLayoutPanel();
            InnertableLayoutPanel_BatteryInfo = new TableLayoutPanel();
            lblBattery = new Label();
            lblBatteryValue = new Label();
            lblVoltage = new Label();
            lblVoltageValue = new Label();
            lblChargeStatus = new Label();
            lblChargeStatusValue = new Label();
            lblHealthStatus = new Label();
            lblHealthStatusValue = new Label();
            pbBatteryRing = new PictureBox();
            groupBox_DeviceInfo = new GroupBox();
            tableLayoutPanel_DeviceInfo = new TableLayoutPanel();
            InnertableLayoutPanel_DeviceInfo = new TableLayoutPanel();
            lblDeviceName = new Label();
            lblDeviceModel = new Label();
            lblSysVersion = new Label();
            lblApiVersion = new Label();
            lblCpuArch = new Label();
            lblResolution = new Label();
            lblDeviceNameValue = new Label();
            lblDeviceModelValue = new Label();
            lblSysVersionValue = new Label();
            lblApiVersionValue = new Label();
            lblCpuArchValue = new Label();
            lblResolutionValue = new Label();
            tableLayoutPanelConnectStatus = new TableLayoutPanel();
            labelConnectStatus = new Label();
            labelConnectStatusValue = new Label();
            btnRefresh = new Button();
            flowLayoutPanelConnectControl.SuspendLayout();
            tableLayoutPanelMain.SuspendLayout();
            groupBox_StorageInfo.SuspendLayout();
            tableLayoutPanel_StorageInfo.SuspendLayout();
            InnertableLayoutPanel_StorageInfo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbStorageRing).BeginInit();
            groupBox_BatteryInfo.SuspendLayout();
            tableLayoutPanel_BatteryInfo.SuspendLayout();
            InnertableLayoutPanel_BatteryInfo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pbBatteryRing).BeginInit();
            groupBox_DeviceInfo.SuspendLayout();
            tableLayoutPanel_DeviceInfo.SuspendLayout();
            InnertableLayoutPanel_DeviceInfo.SuspendLayout();
            tableLayoutPanelConnectStatus.SuspendLayout();
            SuspendLayout();
            // 
            // flowLayoutPanelConnectControl
            // 
            flowLayoutPanelConnectControl.AutoSize = true;
            flowLayoutPanelConnectControl.Controls.Add(cmbDevices);
            flowLayoutPanelConnectControl.Controls.Add(btnReconnect);
            flowLayoutPanelConnectControl.Controls.Add(btnDisconnect);
            flowLayoutPanelConnectControl.Controls.Add(btnAddDevice);
            flowLayoutPanelConnectControl.Controls.Add(btnDeleteDevice);
            flowLayoutPanelConnectControl.Dock = DockStyle.Fill;
            flowLayoutPanelConnectControl.Location = new Point(3, 3);
            flowLayoutPanelConnectControl.Name = "flowLayoutPanelConnectControl";
            flowLayoutPanelConnectControl.Padding = new Padding(5);
            flowLayoutPanelConnectControl.Size = new Size(872, 50);
            flowLayoutPanelConnectControl.TabIndex = 0;
            // 
            // cmbDevices
            // 
            cmbDevices.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbDevices.FormattingEnabled = true;
            cmbDevices.Location = new Point(8, 8);
            cmbDevices.Name = "cmbDevices";
            cmbDevices.Size = new Size(377, 32);
            cmbDevices.TabIndex = 0;
            // 
            // btnReconnect
            // 
            btnReconnect.Location = new Point(391, 8);
            btnReconnect.Name = "btnReconnect";
            btnReconnect.Size = new Size(112, 34);
            btnReconnect.TabIndex = 1;
            btnReconnect.Text = "重新连接";
            btnReconnect.UseVisualStyleBackColor = true;
            // 
            // btnDisconnect
            // 
            btnDisconnect.Location = new Point(509, 8);
            btnDisconnect.Name = "btnDisconnect";
            btnDisconnect.Size = new Size(112, 34);
            btnDisconnect.TabIndex = 2;
            btnDisconnect.Text = "断开连接";
            btnDisconnect.UseVisualStyleBackColor = true;
            // 
            // btnAddDevice
            // 
            btnAddDevice.Location = new Point(627, 8);
            btnAddDevice.Name = "btnAddDevice";
            btnAddDevice.Size = new Size(112, 34);
            btnAddDevice.TabIndex = 3;
            btnAddDevice.Text = "添加设备";
            btnAddDevice.UseVisualStyleBackColor = true;
            // 
            // btnDeleteDevice
            // 
            btnDeleteDevice.Location = new Point(745, 8);
            btnDeleteDevice.Name = "btnDeleteDevice";
            btnDeleteDevice.Size = new Size(112, 34);
            btnDeleteDevice.TabIndex = 4;
            btnDeleteDevice.Text = "删除设备";
            btnDeleteDevice.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanelMain
            // 
            tableLayoutPanelMain.ColumnCount = 1;
            tableLayoutPanelMain.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanelMain.Controls.Add(flowLayoutPanelConnectControl, 0, 0);
            tableLayoutPanelMain.Controls.Add(groupBox_StorageInfo, 0, 4);
            tableLayoutPanelMain.Controls.Add(groupBox_BatteryInfo, 0, 3);
            tableLayoutPanelMain.Controls.Add(groupBox_DeviceInfo, 0, 2);
            tableLayoutPanelMain.Controls.Add(tableLayoutPanelConnectStatus, 0, 1);
            tableLayoutPanelMain.Dock = DockStyle.Fill;
            tableLayoutPanelMain.Location = new Point(0, 0);
            tableLayoutPanelMain.Name = "tableLayoutPanelMain";
            tableLayoutPanelMain.RowCount = 5;
            tableLayoutPanelMain.RowStyles.Add(new RowStyle());
            tableLayoutPanelMain.RowStyles.Add(new RowStyle());
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3344421F));
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.Percent, 33.334446F));
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3311157F));
            tableLayoutPanelMain.Size = new Size(878, 1017);
            tableLayoutPanelMain.TabIndex = 1;
            // 
            // groupBox_StorageInfo
            // 
            groupBox_StorageInfo.Controls.Add(tableLayoutPanel_StorageInfo);
            groupBox_StorageInfo.Dock = DockStyle.Fill;
            groupBox_StorageInfo.Location = new Point(3, 717);
            groupBox_StorageInfo.Name = "groupBox_StorageInfo";
            groupBox_StorageInfo.Size = new Size(872, 297);
            groupBox_StorageInfo.TabIndex = 3;
            groupBox_StorageInfo.TabStop = false;
            groupBox_StorageInfo.Text = "存储空间";
            // 
            // tableLayoutPanel_StorageInfo
            // 
            tableLayoutPanel_StorageInfo.ColumnCount = 2;
            tableLayoutPanel_StorageInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            tableLayoutPanel_StorageInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            tableLayoutPanel_StorageInfo.Controls.Add(InnertableLayoutPanel_StorageInfo, 0, 0);
            tableLayoutPanel_StorageInfo.Controls.Add(pbStorageRing, 1, 0);
            tableLayoutPanel_StorageInfo.Dock = DockStyle.Fill;
            tableLayoutPanel_StorageInfo.Location = new Point(3, 26);
            tableLayoutPanel_StorageInfo.Name = "tableLayoutPanel_StorageInfo";
            tableLayoutPanel_StorageInfo.RowCount = 1;
            tableLayoutPanel_StorageInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel_StorageInfo.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel_StorageInfo.Size = new Size(866, 268);
            tableLayoutPanel_StorageInfo.TabIndex = 0;
            // 
            // InnertableLayoutPanel_StorageInfo
            // 
            InnertableLayoutPanel_StorageInfo.ColumnCount = 2;
            InnertableLayoutPanel_StorageInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            InnertableLayoutPanel_StorageInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            InnertableLayoutPanel_StorageInfo.Controls.Add(lblTotalStorage, 0, 0);
            InnertableLayoutPanel_StorageInfo.Controls.Add(lblTotalStorageValue, 1, 0);
            InnertableLayoutPanel_StorageInfo.Controls.Add(lblUsedStorage, 0, 1);
            InnertableLayoutPanel_StorageInfo.Controls.Add(lblUsedStorageValue, 1, 1);
            InnertableLayoutPanel_StorageInfo.Controls.Add(lblFreeStorage, 0, 2);
            InnertableLayoutPanel_StorageInfo.Controls.Add(lblFreeStorageValue, 1, 2);
            InnertableLayoutPanel_StorageInfo.Dock = DockStyle.Fill;
            InnertableLayoutPanel_StorageInfo.Location = new Point(3, 3);
            InnertableLayoutPanel_StorageInfo.Name = "InnertableLayoutPanel_StorageInfo";
            InnertableLayoutPanel_StorageInfo.Padding = new Padding(10);
            InnertableLayoutPanel_StorageInfo.RowCount = 3;
            InnertableLayoutPanel_StorageInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            InnertableLayoutPanel_StorageInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            InnertableLayoutPanel_StorageInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            InnertableLayoutPanel_StorageInfo.Size = new Size(600, 262);
            InnertableLayoutPanel_StorageInfo.TabIndex = 0;
            // 
            // lblTotalStorage
            // 
            lblTotalStorage.AutoSize = true;
            lblTotalStorage.Dock = DockStyle.Fill;
            lblTotalStorage.Location = new Point(13, 10);
            lblTotalStorage.Name = "lblTotalStorage";
            lblTotalStorage.Size = new Size(284, 80);
            lblTotalStorage.TabIndex = 0;
            lblTotalStorage.Text = "总空间";
            lblTotalStorage.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblTotalStorageValue
            // 
            lblTotalStorageValue.AutoSize = true;
            lblTotalStorageValue.Dock = DockStyle.Fill;
            lblTotalStorageValue.Location = new Point(303, 10);
            lblTotalStorageValue.Name = "lblTotalStorageValue";
            lblTotalStorageValue.Size = new Size(284, 80);
            lblTotalStorageValue.TabIndex = 1;
            lblTotalStorageValue.Text = "N/A GB";
            lblTotalStorageValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblUsedStorage
            // 
            lblUsedStorage.AutoSize = true;
            lblUsedStorage.Dock = DockStyle.Fill;
            lblUsedStorage.Location = new Point(13, 90);
            lblUsedStorage.Name = "lblUsedStorage";
            lblUsedStorage.Size = new Size(284, 80);
            lblUsedStorage.TabIndex = 2;
            lblUsedStorage.Text = "已用空间";
            lblUsedStorage.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblUsedStorageValue
            // 
            lblUsedStorageValue.AutoSize = true;
            lblUsedStorageValue.Dock = DockStyle.Fill;
            lblUsedStorageValue.Location = new Point(303, 90);
            lblUsedStorageValue.Name = "lblUsedStorageValue";
            lblUsedStorageValue.Size = new Size(284, 80);
            lblUsedStorageValue.TabIndex = 3;
            lblUsedStorageValue.Text = "N/A GB";
            lblUsedStorageValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblFreeStorage
            // 
            lblFreeStorage.AutoSize = true;
            lblFreeStorage.Dock = DockStyle.Fill;
            lblFreeStorage.Location = new Point(13, 170);
            lblFreeStorage.Name = "lblFreeStorage";
            lblFreeStorage.Size = new Size(284, 82);
            lblFreeStorage.TabIndex = 4;
            lblFreeStorage.Text = "可用空间";
            lblFreeStorage.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblFreeStorageValue
            // 
            lblFreeStorageValue.AutoSize = true;
            lblFreeStorageValue.Dock = DockStyle.Fill;
            lblFreeStorageValue.Location = new Point(303, 170);
            lblFreeStorageValue.Name = "lblFreeStorageValue";
            lblFreeStorageValue.Size = new Size(284, 82);
            lblFreeStorageValue.TabIndex = 5;
            lblFreeStorageValue.Text = "N/A GB";
            lblFreeStorageValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pbStorageRing
            // 
            pbStorageRing.Dock = DockStyle.Fill;
            pbStorageRing.Location = new Point(609, 3);
            pbStorageRing.Name = "pbStorageRing";
            pbStorageRing.Padding = new Padding(10);
            pbStorageRing.Size = new Size(254, 262);
            pbStorageRing.TabIndex = 1;
            pbStorageRing.TabStop = false;
            // 
            // groupBox_BatteryInfo
            // 
            groupBox_BatteryInfo.Controls.Add(tableLayoutPanel_BatteryInfo);
            groupBox_BatteryInfo.Dock = DockStyle.Fill;
            groupBox_BatteryInfo.Location = new Point(3, 414);
            groupBox_BatteryInfo.Name = "groupBox_BatteryInfo";
            groupBox_BatteryInfo.Size = new Size(872, 297);
            groupBox_BatteryInfo.TabIndex = 2;
            groupBox_BatteryInfo.TabStop = false;
            groupBox_BatteryInfo.Text = "电池信息";
            // 
            // tableLayoutPanel_BatteryInfo
            // 
            tableLayoutPanel_BatteryInfo.ColumnCount = 2;
            tableLayoutPanel_BatteryInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            tableLayoutPanel_BatteryInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            tableLayoutPanel_BatteryInfo.Controls.Add(InnertableLayoutPanel_BatteryInfo, 0, 0);
            tableLayoutPanel_BatteryInfo.Controls.Add(pbBatteryRing, 1, 0);
            tableLayoutPanel_BatteryInfo.Dock = DockStyle.Fill;
            tableLayoutPanel_BatteryInfo.Location = new Point(3, 26);
            tableLayoutPanel_BatteryInfo.Name = "tableLayoutPanel_BatteryInfo";
            tableLayoutPanel_BatteryInfo.RowCount = 1;
            tableLayoutPanel_BatteryInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel_BatteryInfo.Size = new Size(866, 268);
            tableLayoutPanel_BatteryInfo.TabIndex = 0;
            // 
            // InnertableLayoutPanel_BatteryInfo
            // 
            InnertableLayoutPanel_BatteryInfo.ColumnCount = 2;
            InnertableLayoutPanel_BatteryInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            InnertableLayoutPanel_BatteryInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            InnertableLayoutPanel_BatteryInfo.Controls.Add(lblBattery, 0, 0);
            InnertableLayoutPanel_BatteryInfo.Controls.Add(lblBatteryValue, 1, 0);
            InnertableLayoutPanel_BatteryInfo.Controls.Add(lblVoltage, 0, 1);
            InnertableLayoutPanel_BatteryInfo.Controls.Add(lblVoltageValue, 1, 1);
            InnertableLayoutPanel_BatteryInfo.Controls.Add(lblChargeStatus, 0, 2);
            InnertableLayoutPanel_BatteryInfo.Controls.Add(lblChargeStatusValue, 1, 2);
            InnertableLayoutPanel_BatteryInfo.Controls.Add(lblHealthStatus, 0, 3);
            InnertableLayoutPanel_BatteryInfo.Controls.Add(lblHealthStatusValue, 1, 3);
            InnertableLayoutPanel_BatteryInfo.Dock = DockStyle.Fill;
            InnertableLayoutPanel_BatteryInfo.Location = new Point(3, 3);
            InnertableLayoutPanel_BatteryInfo.Name = "InnertableLayoutPanel_BatteryInfo";
            InnertableLayoutPanel_BatteryInfo.Padding = new Padding(10);
            InnertableLayoutPanel_BatteryInfo.RowCount = 4;
            InnertableLayoutPanel_BatteryInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            InnertableLayoutPanel_BatteryInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            InnertableLayoutPanel_BatteryInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            InnertableLayoutPanel_BatteryInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            InnertableLayoutPanel_BatteryInfo.Size = new Size(600, 262);
            InnertableLayoutPanel_BatteryInfo.TabIndex = 0;
            // 
            // lblBattery
            // 
            lblBattery.AutoSize = true;
            lblBattery.Dock = DockStyle.Fill;
            lblBattery.Location = new Point(13, 10);
            lblBattery.Name = "lblBattery";
            lblBattery.Size = new Size(284, 60);
            lblBattery.TabIndex = 0;
            lblBattery.Text = "电池电量";
            lblBattery.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblBatteryValue
            // 
            lblBatteryValue.AutoSize = true;
            lblBatteryValue.Dock = DockStyle.Fill;
            lblBatteryValue.Location = new Point(303, 10);
            lblBatteryValue.Name = "lblBatteryValue";
            lblBatteryValue.Size = new Size(284, 60);
            lblBatteryValue.TabIndex = 1;
            lblBatteryValue.Text = "N/A %";
            lblBatteryValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblVoltage
            // 
            lblVoltage.AutoSize = true;
            lblVoltage.Dock = DockStyle.Fill;
            lblVoltage.Location = new Point(13, 70);
            lblVoltage.Name = "lblVoltage";
            lblVoltage.Size = new Size(284, 60);
            lblVoltage.TabIndex = 2;
            lblVoltage.Text = "电池电压";
            lblVoltage.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblVoltageValue
            // 
            lblVoltageValue.AutoSize = true;
            lblVoltageValue.Dock = DockStyle.Fill;
            lblVoltageValue.Location = new Point(303, 70);
            lblVoltageValue.Name = "lblVoltageValue";
            lblVoltageValue.Size = new Size(284, 60);
            lblVoltageValue.TabIndex = 3;
            lblVoltageValue.Text = "N/A V";
            lblVoltageValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblChargeStatus
            // 
            lblChargeStatus.AutoSize = true;
            lblChargeStatus.Dock = DockStyle.Fill;
            lblChargeStatus.Location = new Point(13, 130);
            lblChargeStatus.Name = "lblChargeStatus";
            lblChargeStatus.Size = new Size(284, 60);
            lblChargeStatus.TabIndex = 4;
            lblChargeStatus.Text = "充电状态";
            lblChargeStatus.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblChargeStatusValue
            // 
            lblChargeStatusValue.AutoSize = true;
            lblChargeStatusValue.Dock = DockStyle.Fill;
            lblChargeStatusValue.Location = new Point(303, 130);
            lblChargeStatusValue.Name = "lblChargeStatusValue";
            lblChargeStatusValue.Size = new Size(284, 60);
            lblChargeStatusValue.TabIndex = 5;
            lblChargeStatusValue.Text = "未知";
            lblChargeStatusValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblHealthStatus
            // 
            lblHealthStatus.AutoSize = true;
            lblHealthStatus.Dock = DockStyle.Fill;
            lblHealthStatus.Location = new Point(13, 190);
            lblHealthStatus.Name = "lblHealthStatus";
            lblHealthStatus.Size = new Size(284, 62);
            lblHealthStatus.TabIndex = 6;
            lblHealthStatus.Text = "健康状态";
            lblHealthStatus.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblHealthStatusValue
            // 
            lblHealthStatusValue.AutoSize = true;
            lblHealthStatusValue.Dock = DockStyle.Fill;
            lblHealthStatusValue.Location = new Point(303, 190);
            lblHealthStatusValue.Name = "lblHealthStatusValue";
            lblHealthStatusValue.Size = new Size(284, 62);
            lblHealthStatusValue.TabIndex = 7;
            lblHealthStatusValue.Text = "未知";
            lblHealthStatusValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // pbBatteryRing
            // 
            pbBatteryRing.Dock = DockStyle.Fill;
            pbBatteryRing.Location = new Point(609, 3);
            pbBatteryRing.Name = "pbBatteryRing";
            pbBatteryRing.Padding = new Padding(10);
            pbBatteryRing.Size = new Size(254, 262);
            pbBatteryRing.TabIndex = 1;
            pbBatteryRing.TabStop = false;
            // 
            // groupBox_DeviceInfo
            // 
            groupBox_DeviceInfo.Controls.Add(tableLayoutPanel_DeviceInfo);
            groupBox_DeviceInfo.Dock = DockStyle.Fill;
            groupBox_DeviceInfo.Location = new Point(3, 111);
            groupBox_DeviceInfo.Name = "groupBox_DeviceInfo";
            groupBox_DeviceInfo.Size = new Size(872, 297);
            groupBox_DeviceInfo.TabIndex = 1;
            groupBox_DeviceInfo.TabStop = false;
            groupBox_DeviceInfo.Text = "设备信息";
            // 
            // tableLayoutPanel_DeviceInfo
            // 
            tableLayoutPanel_DeviceInfo.ColumnCount = 1;
            tableLayoutPanel_DeviceInfo.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanel_DeviceInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel_DeviceInfo.Controls.Add(InnertableLayoutPanel_DeviceInfo, 0, 0);
            tableLayoutPanel_DeviceInfo.Dock = DockStyle.Fill;
            tableLayoutPanel_DeviceInfo.Location = new Point(3, 26);
            tableLayoutPanel_DeviceInfo.Name = "tableLayoutPanel_DeviceInfo";
            tableLayoutPanel_DeviceInfo.RowCount = 1;
            tableLayoutPanel_DeviceInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel_DeviceInfo.Size = new Size(866, 268);
            tableLayoutPanel_DeviceInfo.TabIndex = 1;
            // 
            // InnertableLayoutPanel_DeviceInfo
            // 
            InnertableLayoutPanel_DeviceInfo.ColumnCount = 2;
            InnertableLayoutPanel_DeviceInfo.ColumnStyles.Add(new ColumnStyle());
            InnertableLayoutPanel_DeviceInfo.ColumnStyles.Add(new ColumnStyle());
            InnertableLayoutPanel_DeviceInfo.Controls.Add(lblDeviceName, 0, 0);
            InnertableLayoutPanel_DeviceInfo.Controls.Add(lblDeviceModel, 0, 1);
            InnertableLayoutPanel_DeviceInfo.Controls.Add(lblSysVersion, 0, 2);
            InnertableLayoutPanel_DeviceInfo.Controls.Add(lblApiVersion, 0, 3);
            InnertableLayoutPanel_DeviceInfo.Controls.Add(lblCpuArch, 0, 4);
            InnertableLayoutPanel_DeviceInfo.Controls.Add(lblResolution, 0, 5);
            InnertableLayoutPanel_DeviceInfo.Controls.Add(lblDeviceNameValue, 1, 0);
            InnertableLayoutPanel_DeviceInfo.Controls.Add(lblDeviceModelValue, 1, 1);
            InnertableLayoutPanel_DeviceInfo.Controls.Add(lblSysVersionValue, 1, 2);
            InnertableLayoutPanel_DeviceInfo.Controls.Add(lblApiVersionValue, 1, 3);
            InnertableLayoutPanel_DeviceInfo.Controls.Add(lblCpuArchValue, 1, 4);
            InnertableLayoutPanel_DeviceInfo.Controls.Add(lblResolutionValue, 1, 5);
            InnertableLayoutPanel_DeviceInfo.Dock = DockStyle.Fill;
            InnertableLayoutPanel_DeviceInfo.Location = new Point(3, 3);
            InnertableLayoutPanel_DeviceInfo.Name = "InnertableLayoutPanel_DeviceInfo";
            InnertableLayoutPanel_DeviceInfo.Padding = new Padding(10);
            InnertableLayoutPanel_DeviceInfo.RowCount = 6;
            InnertableLayoutPanel_DeviceInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666641F));
            InnertableLayoutPanel_DeviceInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666641F));
            InnertableLayoutPanel_DeviceInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666641F));
            InnertableLayoutPanel_DeviceInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666641F));
            InnertableLayoutPanel_DeviceInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666641F));
            InnertableLayoutPanel_DeviceInfo.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6666641F));
            InnertableLayoutPanel_DeviceInfo.Size = new Size(860, 262);
            InnertableLayoutPanel_DeviceInfo.TabIndex = 0;
            // 
            // lblDeviceName
            // 
            lblDeviceName.AutoSize = true;
            lblDeviceName.Dock = DockStyle.Fill;
            lblDeviceName.Font = new Font("Microsoft YaHei UI", 10F);
            lblDeviceName.Location = new Point(13, 10);
            lblDeviceName.Name = "lblDeviceName";
            lblDeviceName.Size = new Size(112, 40);
            lblDeviceName.TabIndex = 0;
            lblDeviceName.Text = "设备名称";
            lblDeviceName.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblDeviceModel
            // 
            lblDeviceModel.AutoSize = true;
            lblDeviceModel.Dock = DockStyle.Fill;
            lblDeviceModel.Font = new Font("Microsoft YaHei UI", 10F);
            lblDeviceModel.Location = new Point(13, 50);
            lblDeviceModel.Name = "lblDeviceModel";
            lblDeviceModel.Size = new Size(112, 40);
            lblDeviceModel.TabIndex = 1;
            lblDeviceModel.Text = "设备型号";
            lblDeviceModel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblSysVersion
            // 
            lblSysVersion.AutoSize = true;
            lblSysVersion.Dock = DockStyle.Fill;
            lblSysVersion.Font = new Font("Microsoft YaHei UI", 10F);
            lblSysVersion.Location = new Point(13, 90);
            lblSysVersion.Name = "lblSysVersion";
            lblSysVersion.Size = new Size(112, 40);
            lblSysVersion.TabIndex = 2;
            lblSysVersion.Text = "系统版本";
            lblSysVersion.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblApiVersion
            // 
            lblApiVersion.AutoSize = true;
            lblApiVersion.Dock = DockStyle.Fill;
            lblApiVersion.Font = new Font("Microsoft YaHei UI", 10F);
            lblApiVersion.Location = new Point(13, 130);
            lblApiVersion.Name = "lblApiVersion";
            lblApiVersion.Size = new Size(112, 40);
            lblApiVersion.TabIndex = 3;
            lblApiVersion.Text = "API 版本";
            lblApiVersion.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblCpuArch
            // 
            lblCpuArch.AutoSize = true;
            lblCpuArch.Dock = DockStyle.Fill;
            lblCpuArch.Font = new Font("Microsoft YaHei UI", 10F);
            lblCpuArch.Location = new Point(13, 170);
            lblCpuArch.Name = "lblCpuArch";
            lblCpuArch.Size = new Size(112, 40);
            lblCpuArch.TabIndex = 4;
            lblCpuArch.Text = "CPU 架构";
            lblCpuArch.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblResolution
            // 
            lblResolution.AutoSize = true;
            lblResolution.Dock = DockStyle.Fill;
            lblResolution.Font = new Font("Microsoft YaHei UI", 10F);
            lblResolution.Location = new Point(13, 210);
            lblResolution.Name = "lblResolution";
            lblResolution.Size = new Size(112, 42);
            lblResolution.TabIndex = 5;
            lblResolution.Text = "屏幕分辨率";
            lblResolution.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblDeviceNameValue
            // 
            lblDeviceNameValue.AutoSize = true;
            lblDeviceNameValue.Dock = DockStyle.Fill;
            lblDeviceNameValue.Font = new Font("Microsoft YaHei UI", 10F);
            lblDeviceNameValue.Location = new Point(131, 10);
            lblDeviceNameValue.Name = "lblDeviceNameValue";
            lblDeviceNameValue.Size = new Size(716, 40);
            lblDeviceNameValue.TabIndex = 6;
            lblDeviceNameValue.Text = "未知";
            lblDeviceNameValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblDeviceModelValue
            // 
            lblDeviceModelValue.AutoSize = true;
            lblDeviceModelValue.Dock = DockStyle.Fill;
            lblDeviceModelValue.Font = new Font("Microsoft YaHei UI", 10F);
            lblDeviceModelValue.Location = new Point(131, 50);
            lblDeviceModelValue.Name = "lblDeviceModelValue";
            lblDeviceModelValue.Size = new Size(716, 40);
            lblDeviceModelValue.TabIndex = 7;
            lblDeviceModelValue.Text = "未知";
            lblDeviceModelValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblSysVersionValue
            // 
            lblSysVersionValue.AutoSize = true;
            lblSysVersionValue.Dock = DockStyle.Fill;
            lblSysVersionValue.Font = new Font("Microsoft YaHei UI", 10F);
            lblSysVersionValue.Location = new Point(131, 90);
            lblSysVersionValue.Name = "lblSysVersionValue";
            lblSysVersionValue.Size = new Size(716, 40);
            lblSysVersionValue.TabIndex = 8;
            lblSysVersionValue.Text = "未知";
            lblSysVersionValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblApiVersionValue
            // 
            lblApiVersionValue.AutoSize = true;
            lblApiVersionValue.Dock = DockStyle.Fill;
            lblApiVersionValue.Font = new Font("Microsoft YaHei UI", 10F);
            lblApiVersionValue.Location = new Point(131, 130);
            lblApiVersionValue.Name = "lblApiVersionValue";
            lblApiVersionValue.Size = new Size(716, 40);
            lblApiVersionValue.TabIndex = 9;
            lblApiVersionValue.Text = "未知";
            lblApiVersionValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblCpuArchValue
            // 
            lblCpuArchValue.AutoSize = true;
            lblCpuArchValue.Dock = DockStyle.Fill;
            lblCpuArchValue.Font = new Font("Microsoft YaHei UI", 10F);
            lblCpuArchValue.Location = new Point(131, 170);
            lblCpuArchValue.Name = "lblCpuArchValue";
            lblCpuArchValue.Size = new Size(716, 40);
            lblCpuArchValue.TabIndex = 10;
            lblCpuArchValue.Text = "未知";
            lblCpuArchValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblResolutionValue
            // 
            lblResolutionValue.AutoSize = true;
            lblResolutionValue.Dock = DockStyle.Fill;
            lblResolutionValue.Font = new Font("Microsoft YaHei UI", 10F);
            lblResolutionValue.Location = new Point(131, 210);
            lblResolutionValue.Name = "lblResolutionValue";
            lblResolutionValue.Size = new Size(716, 42);
            lblResolutionValue.TabIndex = 11;
            lblResolutionValue.Text = "未知";
            lblResolutionValue.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // tableLayoutPanelConnectStatus
            // 
            tableLayoutPanelConnectStatus.AutoSize = true;
            tableLayoutPanelConnectStatus.ColumnCount = 3;
            tableLayoutPanelConnectStatus.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanelConnectStatus.ColumnStyles.Add(new ColumnStyle());
            tableLayoutPanelConnectStatus.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelConnectStatus.Controls.Add(labelConnectStatus, 0, 0);
            tableLayoutPanelConnectStatus.Controls.Add(labelConnectStatusValue, 1, 0);
            tableLayoutPanelConnectStatus.Controls.Add(btnRefresh, 2, 0);
            tableLayoutPanelConnectStatus.Dock = DockStyle.Top;
            tableLayoutPanelConnectStatus.Location = new Point(3, 59);
            tableLayoutPanelConnectStatus.Name = "tableLayoutPanelConnectStatus";
            tableLayoutPanelConnectStatus.Padding = new Padding(3, 3, 10, 3);
            tableLayoutPanelConnectStatus.RowCount = 1;
            tableLayoutPanelConnectStatus.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelConnectStatus.Size = new Size(872, 46);
            tableLayoutPanelConnectStatus.TabIndex = 4;
            // 
            // labelConnectStatus
            // 
            labelConnectStatus.AutoSize = true;
            labelConnectStatus.Dock = DockStyle.Fill;
            labelConnectStatus.Font = new Font("Microsoft YaHei UI", 10F);
            labelConnectStatus.Location = new Point(6, 3);
            labelConnectStatus.Name = "labelConnectStatus";
            labelConnectStatus.Size = new Size(103, 40);
            labelConnectStatus.TabIndex = 3;
            labelConnectStatus.Text = "连接状态 :";
            labelConnectStatus.TextAlign = ContentAlignment.MiddleRight;
            // 
            // labelConnectStatusValue
            // 
            labelConnectStatusValue.AutoSize = true;
            labelConnectStatusValue.Dock = DockStyle.Fill;
            labelConnectStatusValue.Font = new Font("Microsoft YaHei UI", 10F);
            labelConnectStatusValue.Location = new Point(115, 3);
            labelConnectStatusValue.Name = "labelConnectStatusValue";
            labelConnectStatusValue.Size = new Size(72, 40);
            labelConnectStatusValue.TabIndex = 4;
            labelConnectStatusValue.Text = "未连接";
            labelConnectStatusValue.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // btnRefresh
            // 
            btnRefresh.AutoSize = true;
            btnRefresh.Dock = DockStyle.Right;
            btnRefresh.Location = new Point(731, 6);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(128, 34);
            btnRefresh.TabIndex = 2;
            btnRefresh.Text = "刷新连接状态";
            btnRefresh.UseVisualStyleBackColor = true;
            // 
            // HomeControl
            // 
            AutoScaleDimensions = new SizeF(11F, 24F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(tableLayoutPanelMain);
            Name = "HomeControl";
            Size = new Size(878, 1017);
            flowLayoutPanelConnectControl.ResumeLayout(false);
            tableLayoutPanelMain.ResumeLayout(false);
            tableLayoutPanelMain.PerformLayout();
            groupBox_StorageInfo.ResumeLayout(false);
            tableLayoutPanel_StorageInfo.ResumeLayout(false);
            InnertableLayoutPanel_StorageInfo.ResumeLayout(false);
            InnertableLayoutPanel_StorageInfo.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pbStorageRing).EndInit();
            groupBox_BatteryInfo.ResumeLayout(false);
            tableLayoutPanel_BatteryInfo.ResumeLayout(false);
            InnertableLayoutPanel_BatteryInfo.ResumeLayout(false);
            InnertableLayoutPanel_BatteryInfo.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pbBatteryRing).EndInit();
            groupBox_DeviceInfo.ResumeLayout(false);
            tableLayoutPanel_DeviceInfo.ResumeLayout(false);
            InnertableLayoutPanel_DeviceInfo.ResumeLayout(false);
            InnertableLayoutPanel_DeviceInfo.PerformLayout();
            tableLayoutPanelConnectStatus.ResumeLayout(false);
            tableLayoutPanelConnectStatus.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private FlowLayoutPanel flowLayoutPanelConnectControl;
        private TableLayoutPanel tableLayoutPanelMain;
        private ComboBox cmbDevices;
        private Button btnReconnect;
        private Button btnDisconnect;
        private Button btnAddDevice;
        private Button btnDeleteDevice;
        private TableLayoutPanel tableLayoutPanel_DeviceInfo;
        private TableLayoutPanel InnertableLayoutPanel_DeviceInfo;
        private Label lblDeviceName;
        private Label lblDeviceModel;
        private Label lblSysVersion;
        private Label lblApiVersion;
        private Label lblCpuArch;
        private Label lblResolution;
        private Label lblDeviceNameValue;
        private Label lblDeviceModelValue;
        private Label lblSysVersionValue;
        private Label lblApiVersionValue;
        private Label lblCpuArchValue;
        private Label lblResolutionValue;
        private GroupBox groupBox_DeviceInfo;
        private GroupBox groupBox_BatteryInfo;
        private GroupBox groupBox_StorageInfo;
        private TableLayoutPanel tableLayoutPanel_BatteryInfo;
        private TableLayoutPanel InnertableLayoutPanel_BatteryInfo;
        private Label lblBattery;
        private Label lblBatteryValue;
        private Label lblVoltage;
        private Label lblVoltageValue;
        private Label lblChargeStatus;
        private Label lblChargeStatusValue;
        private Label lblHealthStatus;
        private Label lblHealthStatusValue;
        private TableLayoutPanel tableLayoutPanel_StorageInfo;
        private TableLayoutPanel InnertableLayoutPanel_StorageInfo;
        private Label lblTotalStorage;
        private Label lblTotalStorageValue;
        private Label lblUsedStorage;
        private Label lblUsedStorageValue;
        private Label lblFreeStorage;
        private Label lblFreeStorageValue;
        private PictureBox pbBatteryRing;
        private PictureBox pbStorageRing;
        private TableLayoutPanel tableLayoutPanelConnectStatus;
        private Button btnRefresh;
        private Label labelConnectStatus;
        private Label labelConnectStatusValue;
    }
}
