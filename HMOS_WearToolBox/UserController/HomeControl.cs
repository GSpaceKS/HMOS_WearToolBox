#nullable disable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using HMOS_WearToolBox.Helper;
using HMOS_WearToolBox.Manager;
using HMOS_WearToolBox.DataParsers;
using WinFormsTimer = System.Windows.Forms.Timer;

namespace HMOS_WearToolBox.UserController
{
    /// <summary>
    /// 主界面控件，显示设备信息（电量、存储、系统版本等），管理设备连接和刷新。
    /// </summary>
    public partial class HomeControl : UserControl
    {
        private WinFormsTimer refreshTimer;
        private int batteryPercent = 0;
        private int storagePercent = 0;
        private bool isRefreshing = false;

        /// <summary>
        /// 初始化 HomeControl 控件，设置事件处理程序，加载设备列表，启动自动刷新定时器（根据设置）。
        /// </summary>
        public HomeControl()
        {
            InitializeComponent();

            // 下拉框比按钮矮，FlowLayoutPanel 默认按上边距对齐，看上去会「高一截」。
            // 这里按高度差补一段上边距，让下拉框和四个按钮垂直居中对齐（不同 DPI 下自动算）。
            int comboOffset = Math.Max(0, (btnReconnect.Height - cmbDevices.Height) / 2);
            Padding comboMargin = cmbDevices.Margin;
            cmbDevices.Margin = new Padding(comboMargin.Left, comboMargin.Top + comboOffset, comboMargin.Right, comboMargin.Bottom);

            // 绑定按钮和下拉框事件
            btnReconnect.Click += BtnReconnect_Click;
            btnDisconnect.Click += BtnDisconnect_Click;
            btnAddDevice.Click += BtnAddDevice_Click;
            btnDeleteDevice.Click += BtnDeleteDevice_Click;
            cmbDevices.MouseClick += CmbDevices_MouseClick;
            btnRefresh.Click += BtnRefresh_Click;
            cmbDevices.SelectedIndexChanged += (s, e) => OnSelectedDeviceChanged();

            // 环形进度条重绘事件
            pbBatteryRing.Paint += PbBatteryRing_Paint;
            pbStorageRing.Paint += PbStorageRing_Paint;
            pbBatteryRing.Resize += (s, e) => pbBatteryRing.Invalidate();
            pbStorageRing.Resize += (s, e) => pbStorageRing.Invalidate();

            // 启动时同步设备连接状态
            DeviceConnectionHelper.CheckAndUpdateConnectionStatus();
            LoadDevices();

            // 如果有已连接的设备，主动刷新一次完整信息
            if (cmbDevices.SelectedItem != null)
            {
                var devices = DeviceManager.GetDevices();
                int idx = cmbDevices.SelectedIndex;
                if (idx >= 0 && idx < devices.Count && devices[idx].IsConnected)
                {
                    _ = RefreshDeviceInfoAsync(fullRefresh: true);
                }
            }

            // 初始化自动刷新定时器
            int intervalSeconds = Properties.Settings.Default.AutoRefreshInterval;
            if (intervalSeconds <= 0) intervalSeconds = 180;
            refreshTimer = new WinFormsTimer();
            refreshTimer.Interval = intervalSeconds * 1000;
            refreshTimer.Tick += async (s, e) => await RefreshDeviceInfoAsync(fullRefresh: false);

            // 根据自动更新开关状态和控件可见性决定是否启动定时器
            if (Properties.Settings.Default.AutoUpdateEnabled && this.Visible)
                refreshTimer.Start();

            UpdateReconnectButtonState();
        }

        /// <summary>
        /// 设置自动刷新间隔（秒），并重启定时器（如果正在运行）。
        /// </summary>
        public void SetRefreshInterval(int seconds)
        {
            if (refreshTimer != null)
            {
                refreshTimer.Interval = seconds * 1000;
                if (refreshTimer.Enabled)
                {
                    refreshTimer.Stop();
                    refreshTimer.Start();
                }
            }
        }

        /// <summary>
        /// 根据当前选中的设备是否存在，更新“重连”按钮的启用状态。
        /// </summary>
        private void UpdateReconnectButtonState()
        {
            btnReconnect.Enabled = cmbDevices.SelectedItem != null;
        }

        private void OnSelectedDeviceChanged()
        {
            UpdateUIForCurrentDevice();
        }

        /// <summary>
        /// 根据当前选中的设备状态（是否已连接）更新 UI 控件。
        /// </summary>
        private void UpdateUIForCurrentDevice()
        {
            if (cmbDevices.SelectedItem == null)
            {
                labelConnectStatusValue.Text = "未连接";
                labelConnectStatusValue.ForeColor = Color.Red;
                btnRefresh.Enabled = false;
                btnReconnect.Enabled = false;
                btnDisconnect.Enabled = false;
                ResetUI();
                return;
            }

            int index = cmbDevices.SelectedIndex;
            var devices = DeviceManager.GetDevices();
            if (index >= 0 && index < devices.Count)
            {
                var device = devices[index];
                if (device.IsConnected)
                {
                    labelConnectStatusValue.Text = "已连接";
                    labelConnectStatusValue.ForeColor = Color.Green;
                    btnRefresh.Enabled = true;
                    btnReconnect.Enabled = true;
                    btnDisconnect.Enabled = true;
                }
                else
                {
                    labelConnectStatusValue.Text = "未连接";
                    labelConnectStatusValue.ForeColor = Color.Red;
                    btnRefresh.Enabled = false;
                    btnReconnect.Enabled = true;
                    btnDisconnect.Enabled = false;
                    ResetUI();
                }
            }
        }

        private void LoadDevices()
        {
            cmbDevices.Items.Clear();
            var devices = DeviceManager.GetDevices();
            if (devices.Count == 0)
            {
                cmbDevices.Text = string.Empty;
                cmbDevices.SelectedIndex = -1;
                UpdateReconnectButtonState();
                UpdateUIForCurrentDevice(); // 无设备时更新 UI
                return;
            }

            foreach (var device in devices)
            {
                string displayName = device.IsNew
                    ? $"(NEW) {device.Name} ({device.IpAddress})"
                    : $"{device.Name} ({device.IpAddress})";
                cmbDevices.Items.Add(displayName);
            }
            cmbDevices.SelectedIndex = 0;
            UpdateReconnectButtonState();
            UpdateUIForCurrentDevice(); // 确保 UI 与当前选中设备的状态一致
        }

        /// <summary>
        /// 添加设备按钮点击事件：弹出输入框输入 IP 地址，尝试连接，若成功则保存设备。
        /// </summary>
        private async void BtnAddDevice_Click(object sender, EventArgs e)
        {
            string ip = Microsoft.VisualBasic.Interaction.InputBox("请输入设备 IP 地址 (例如 192.168.1.100:5555)", "添加设备", "192.168.");
            if (string.IsNullOrWhiteSpace(ip)) return;

            ip = ip.Trim();

            // 检查是否已存在
            var existing = DeviceManager.GetDevices().FirstOrDefault(d => d.IpAddress == ip);
            if (existing != null)
            {
                MessageBox.Show("该设备已存在，请勿重复添加。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 原来这里在 UI 线程上 Thread.Sleep(500) 再跑两次阻塞式 hdc，界面会完全卡住
            this.Cursor = Cursors.WaitCursor;
            btnAddDevice.Enabled = false;

            try
            {
                string result = await Task.Run(() => HdcHelper.RunHdcCommand($"tconn {ip}"));
                await Task.Delay(500);
                List<string> targets = await Task.Run(() => HdcHelper.ListTargets());

                if (HdcHelper.IsTargetOnline(targets, ip))
                {
                    var device = new DeviceInfo
                    {
                        Name = "新设备",
                        IpAddress = ip,
                        LastConnected = DateTime.Now,
                        IsConnected = true,
                        IsNew = true
                    };
                    DeviceManager.AddDevice(device);
                    DeviceConnectionHelper.InvalidateCache();
                    LoadDevices();
                    ShowAuthorizationDialog(device);
                }
                else
                {
                    MessageBox.Show($"连接失败：{result}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"连接失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                btnAddDevice.Enabled = true;
            }
        }

        /// <summary>
        /// 删除设备按钮点击事件：从管理器中删除当前选中的设备，并断开连接。
        /// </summary>
        private async void BtnDeleteDevice_Click(object sender, EventArgs e)
        {
            if (cmbDevices.SelectedItem == null) return;
            int index = cmbDevices.SelectedIndex;
            var device = DeviceManager.GetDevices()[index];

            if (MessageBox.Show($"确定要删除设备 {device.Name} 吗？", "确认删除", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            // 如果设备正在连接，先断开（避免残留连接）
            if (device.IsConnected)
            {
                // 断开是「尽力而为」：失败也要继续删除，否则用户就删不掉了
                await RunHdcQuietly($"tconn {device.IpAddress} -remove");
            }

            // 从管理器中删除
            DeviceManager.RemoveDevice(device.Id);

            // 同步剩余设备的连接状态（确保 DeviceManager 中的 IsConnected 正确）
            DeviceConnectionHelper.InvalidateCache();
            DeviceConnectionHelper.CheckAndUpdateConnectionStatus();

            // 重新加载设备列表（内部会触发 UI 更新）
            LoadDevices();

            // 如果还有设备，刷新一次完整信息（让数据保持最新）
            if (cmbDevices.Items.Count > 0)
            {
                _ = RefreshDeviceInfoAsync(fullRefresh: true);
            }
        }

        /// <summary>
        /// 重连按钮点击事件：如果设备已连接则刷新信息，否则尝试重新连接设备。
        /// </summary>
        private async void BtnReconnect_Click(object sender, EventArgs e)
        {
            if (cmbDevices.SelectedItem == null) return;
            int index = cmbDevices.SelectedIndex;
            var device = DeviceManager.GetDevices()[index];

            if (device.IsConnected)
            {
                await RefreshDeviceInfoAsync(fullRefresh: true);
                return;
            }

            // 创建等待窗体，显示连接进度
            var waitingForm = new Form
            {
                Text = "重连设备",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                ControlBox = false,
                StartPosition = FormStartPosition.CenterParent,
                TopMost = true,
                Padding = new Padding(25)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label lblMsg = new Label
            {
                Text = "正在重连设备并获取数据中...",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 10F)
            };

            Button btnCancel = new Button
            {
                Text = "取消",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Anchor = AnchorStyles.None,
                Margin = new Padding(10, 5, 10, 10),
                Font = new Font("Microsoft YaHei UI", 9F)
            };

            layout.Controls.Add(lblMsg, 0, 0);
            layout.Controls.Add(btnCancel, 0, 1);
            waitingForm.Controls.Add(layout);

            var cts = new CancellationTokenSource();
            btnCancel.Click += (s, e2) => { cts.Cancel(); waitingForm.Close(); };
            waitingForm.Show(this);

            // 执行重连操作（在后台线程）
            var workTask = Task.Run(() =>
            {
                try
                {
                    HdcHelper.RunHdcCommand($"tconn {device.IpAddress} -remove");
                    Thread.Sleep(300);
                    string result = HdcHelper.RunHdcCommand($"tconn {device.IpAddress}");
                    if (cts.Token.IsCancellationRequested) return false;
                    if (!result.Contains("Connect OK")) return false;

                    List<string> targets = HdcHelper.ListTargets();
                    if (!HdcHelper.IsTargetOnline(targets, device.IpAddress)) return false;

                    device.IsConnected = true;
                    device.LastConnected = DateTime.Now;
                    DeviceManager.UpdateDevice(device);
                    return true;
                }
                catch
                {
                    return false;
                }
            }, cts.Token);

            var timeoutTask = Task.Delay(10000);
            var completedTask = await Task.WhenAny(workTask, timeoutTask);
            waitingForm.Close();

            if (cts.IsCancellationRequested) return;

            if (completedTask == timeoutTask)
            {
                cts.Cancel();
                MessageBox.Show("重连设备超时，请检查设备状态", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool success = await workTask;
            if (success)
            {
                DeviceConnectionHelper.CheckAndUpdateConnectionStatus();
                await RefreshDeviceInfoAsync(fullRefresh: true);
                UpdateUIForCurrentDevice();
                (this.ParentForm as MainForm)?.ForceRefreshSoftware();
            }
            else
            {
                MessageBox.Show("重连设备失败，请检查设备是否在线", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnDisconnect_Click(object sender, EventArgs e)
        {
            if (cmbDevices.SelectedItem == null) return;
            int index = cmbDevices.SelectedIndex;
            var device = DeviceManager.GetDevices()[index];

            this.Cursor = Cursors.WaitCursor;
            try
            {
                await RunHdcQuietly($"tconn {device.IpAddress} -remove");
                await Task.Delay(500);

                List<string> targets = await Task.Run(() => HdcHelper.ListTargets());
                bool stillConnected = HdcHelper.IsTargetOnline(targets, device.IpAddress);

                device.IsConnected = false;
                DeviceManager.UpdateDevice(device);
                DeviceConnectionHelper.InvalidateCache();

                cmbDevices.SelectedIndex = -1;
                cmbDevices.Text = string.Empty;
                UpdateReconnectButtonState();
                UpdateUIForCurrentDevice();

                if (stillConnected)
                {
                    MessageBox.Show("设备未能完全断开，请检查网络或重启软件。", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show("已断开", "提示");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"断开失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }

            var mainForm = this.ParentForm as MainForm;
            mainForm?.ClearSoftwareList();
        }

        private void CmbDevices_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && cmbDevices.SelectedItem != null)
            {
                int index = cmbDevices.SelectedIndex;
                var device = DeviceManager.GetDevices()[index];
                if (MessageBox.Show($"删除设备 {device.Name} 吗？", "确认", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    DeviceManager.RemoveDevice(device.Id);
                    LoadDevices();
                }
            }
        }

        private async void BtnRefresh_Click(object sender, EventArgs e)
        {
            if (cmbDevices.SelectedItem == null) return;
            bool isOnline = await DeviceConnectionHelper.CheckAndUpdateConnectionStatusAsync();
            UpdateUIForCurrentDevice();
            if (isOnline)
            {
                await RefreshDeviceInfoAsync(fullRefresh: true);
            }
            else
            {
                MessageBox.Show("设备未连接", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ShowAuthorizationDialog(DeviceInfo device)
        {
            Form authForm = new Form
            {
                Text = "设备授权",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                ControlBox = false,
                StartPosition = FormStartPosition.CenterParent,
                TopMost = true,
                Padding = new Padding(20)
            };

            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            Label label = new Label
            {
                Text = "请在手表端授权（始终信任）",
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 10F),
                Margin = new Padding(5)
            };

            TableLayoutPanel buttonContainer = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 3,
                RowCount = 1,
                Dock = DockStyle.Fill,
                Margin = new Padding(5)
            };
            buttonContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            buttonContainer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            buttonContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            FlowLayoutPanel buttonPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill
            };
            Button btnAuth = new Button { Text = "我已授权", AutoSize = true, Margin = new Padding(5) };
            Button btnCancel = new Button { Text = "取消连接", AutoSize = true, Margin = new Padding(5) };
            buttonPanel.Controls.Add(btnAuth);
            buttonPanel.Controls.Add(btnCancel);

            buttonContainer.Controls.Add(new Panel(), 0, 0);
            buttonContainer.Controls.Add(buttonPanel, 1, 0);
            buttonContainer.Controls.Add(new Panel(), 2, 0);

            mainLayout.Controls.Add(label, 0, 0);
            mainLayout.Controls.Add(buttonContainer, 0, 1);
            authForm.Controls.Add(mainLayout);

            btnAuth.Click += async (s, e) =>
            {
                authForm.Close();

                var infoForm = new Form
                {
                    Text = "获取信息",
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    ControlBox = false,
                    StartPosition = FormStartPosition.CenterParent,
                    TopMost = true
                };
                Label infoLabel = new Label
                {
                    Text = "正在获取手表信息...",
                    AutoSize = true,
                    Padding = new Padding(20),
                    Font = new Font("Microsoft YaHei UI", 10F)
                };
                infoForm.Controls.Add(infoLabel);

                var refreshTask = RefreshDeviceInfoAsync(fullRefresh: true);
                infoForm.Show(this);
                await refreshTask;
                infoForm.Close();
                MessageBox.Show("已获取信息", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            btnCancel.Click += async (s, e) =>
            {
                authForm.Close();
                await RunHdcQuietly($"tconn {device.IpAddress} -remove");
                device.IsConnected = false;
                DeviceManager.UpdateDevice(device);
                DeviceConnectionHelper.InvalidateCache();
                ResetUI();
                UpdateUIForCurrentDevice();
                MessageBox.Show("用户已取消连接！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            authForm.ShowDialog(this);
        }

        /// <summary>
        /// 把更新 UI 的委托安全地派发到 UI 线程；控件已销毁时直接忽略，
        /// 避免刷新途中关窗时抛 ObjectDisposedException。
        /// </summary>
        private void RunOnUi(Action action)
        {
            if (IsDisposed || !IsHandleCreated)
                return;

            if (InvokeRequired)
                BeginInvoke(action);
            else
                action();
        }

        /// <summary>
        /// 执行一条 hdc 命令，失败只记录不抛出。
        /// 用于「断开连接」这类尽力而为的操作 —— 失败也不该阻断后面的界面流程。
        /// </summary>
        private static async Task<string> RunHdcQuietly(string arguments, int timeoutMs = HdcHelper.QuickTimeoutMs)
        {
            try
            {
                return await Task.Run(() => HdcHelper.RunHdcCommand(arguments, timeoutMs));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"hdc {arguments} 失败: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 刷新设备信息，可指定是否进行完整刷新（包括设备名称、型号等静态信息）。
        /// </summary>
        private async Task RefreshDeviceInfoAsync(bool fullRefresh = true)
        {
            if (isRefreshing) return;
            if (cmbDevices.SelectedItem == null) return;

            int index = cmbDevices.SelectedIndex;
            var device = DeviceManager.GetDevices()[index];
            if (!device.IsConnected)
            {
                UpdateUIForCurrentDevice();
                return;
            }

            // 检查设备是否仍在列表中
            bool stillOnline;
            try
            {
                List<string> targets = await Task.Run(() => HdcHelper.ListTargets(HdcHelper.QuickTimeoutMs));
                stillOnline = HdcHelper.IsTargetOnline(targets, device.IpAddress);
            }
            catch (Exception ex)
            {
                // 这个方法由 async void 事件处理器调用，异常逃出去会直接崩掉进程
                MessageBox.Show($"无法与 hdc 通信：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!stillOnline)
            {
                device.IsConnected = false;
                DeviceManager.UpdateDevice(device);
                DeviceConnectionHelper.InvalidateCache();
                UpdateUIForCurrentDevice();
                MessageBox.Show("设备已断开", "提示");
                return;
            }

            isRefreshing = true;
            this.Cursor = Cursors.WaitCursor;

            try
            {
                if (fullRefresh)
                {
                    // 获取静态信息：设备名、型号、系统版本等
                    string rawName = await Task.Run(() => HdcHelper.RunHdcCommand("shell param get const.product.name"));
                    string rawModel = await Task.Run(() => HdcHelper.RunHdcCommand("shell param get const.product.model"));
                    string rawSysVer = await Task.Run(() => HdcHelper.RunHdcCommand("shell param get const.product.software.version"));
                    string rawApiVer = await Task.Run(() => HdcHelper.RunHdcCommand("shell param get const.ohos.apiversion"));
                    string rawCpuArch = await Task.Run(() => HdcHelper.RunHdcCommand("shell param get const.product.cpu.abilist"));
                    string resolutionOutput = await Task.Run(() => HdcHelper.RunHdcCommand("shell hidumper -s RenderService -a screen"));
                    string batteryOutput = await Task.Run(() => HdcHelper.RunHdcCommand("shell hidumper -s BatteryService -a -i"));
                    string storageOutput = await Task.Run(() => HdcHelper.RunHdcCommand("shell df -h"));

                    string name = ExtractParamValue(rawName);
                    string model = ExtractParamValue(rawModel);
                    string sysVersion = ExtractParamValue(rawSysVer);
                    string apiVersion = ExtractParamValue(rawApiVer);
                    string cpuArch = ExtractParamValue(rawCpuArch);
                    string resolution = ExtractResolution(resolutionOutput);
                    var batteryInfo = BatteryParser.Parse(batteryOutput);
                    var storageInfo = StorageParser.Parse(storageOutput);

                    RunOnUi(() =>
                    {
                        lblDeviceNameValue.Text = name;
                        lblDeviceModelValue.Text = model;
                        lblSysVersionValue.Text = sysVersion;
                        lblApiVersionValue.Text = apiVersion;
                        lblCpuArchValue.Text = cpuArch;
                        lblResolutionValue.Text = resolution;
                        UpdateBatteryUI(batteryInfo);
                        UpdateStorageUI(storageInfo);
                        UpdateUIForCurrentDevice();
                    });

                    // 如果获取到的设备名与已保存的不同，则更新设备名称
                    if (!string.IsNullOrWhiteSpace(name) && name != "未知" && name != device.Name)
                    {
                        device.Name = name;
                        DeviceManager.UpdateDevice(device);
                        LoadDevices();
                    }
                }
                else
                {
                    // 仅刷新动态信息：系统版本、电量、存储
                    string rawSysVer = await Task.Run(() => HdcHelper.RunHdcCommand("shell param get const.product.software.version"));
                    string rawApiVer = await Task.Run(() => HdcHelper.RunHdcCommand("shell param get const.ohos.apiversion"));
                    string batteryOutput = await Task.Run(() => HdcHelper.RunHdcCommand("shell hidumper -s BatteryService -a -i"));
                    string storageOutput = await Task.Run(() => HdcHelper.RunHdcCommand("shell df -h"));

                    string sysVersion = ExtractParamValue(rawSysVer);
                    string apiVersion = ExtractParamValue(rawApiVer);
                    var batteryInfo = BatteryParser.Parse(batteryOutput);
                    var storageInfo = StorageParser.Parse(storageOutput);

                    RunOnUi(() =>
                    {
                        lblSysVersionValue.Text = sysVersion;
                        lblApiVersionValue.Text = apiVersion;
                        UpdateBatteryUI(batteryInfo);
                        UpdateStorageUI(storageInfo);
                        UpdateUIForCurrentDevice();
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"刷新失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                isRefreshing = false;
            }
        }

        /// <summary>
        /// 从 "key: value" 格式的输出中提取值部分。
        /// </summary>
        private string ExtractParamValue(string output)
        {
            if (string.IsNullOrWhiteSpace(output)) return "未知";
            int colonIndex = output.IndexOf(':');
            if (colonIndex >= 0 && colonIndex < output.Length - 1)
                return output.Substring(colonIndex + 1).Trim();
            return output.Trim();
        }

        /// <summary>
        /// 从 RenderService 输出中提取屏幕分辨率。
        /// </summary>
        private string ExtractResolution(string output)
        {
            var match = System.Text.RegularExpressions.Regex.Match(output, @"physical resolution=(\d+x\d+)");
            return match.Success ? match.Groups[1].Value : "未知";
        }

        private void UpdateBatteryUI(BatteryInfo info)
        {
            lblBatteryValue.Text = $"{info.Capacity}%";
            lblVoltageValue.Text = $"{info.Voltage / 1000000.0:F3} V";
            lblChargeStatusValue.Text = GetChargeStatusText(info.ChargingStatus);
            lblHealthStatusValue.Text = GetHealthStatusText(info.HealthState);
            batteryPercent = info.Capacity;
            pbBatteryRing.Invalidate();
        }

        private string GetChargeStatusText(int status) => status switch
        {
            0 => "未充电",
            1 => "正在充电",
            2 => "已充满",
            _ => "未知"
        };

        private string GetHealthStatusText(int health) => health switch
        {
            0 => "良好",
            1 => "过热",
            2 => "过压",
            3 => "电压低",
            4 => "故障",
            _ => "未知"
        };

        private void UpdateStorageUI(StorageInfo info)
        {
            double totalGB = info.Total / (1024.0 * 1024);
            double usedGB = info.Used / (1024.0 * 1024);
            double freeGB = info.Free / (1024.0 * 1024);

            lblTotalStorageValue.Text = $"{totalGB:F2} GB";
            lblUsedStorageValue.Text = $"{usedGB:F2} GB";
            lblFreeStorageValue.Text = $"{freeGB:F2} GB";

            if (totalGB > 0)
                storagePercent = (int)((usedGB / totalGB) * 100);
            else
                storagePercent = 0;
            pbStorageRing.Invalidate();
        }

        /// <summary>
        /// 重置所有 UI 显示为默认值（用于未连接状态）。
        /// </summary>
        private void ResetUI()
        {
            lblDeviceNameValue.Text = "未知";
            lblDeviceModelValue.Text = "未知";
            lblResolutionValue.Text = "未知";
            lblSysVersionValue.Text = "未知";
            lblApiVersionValue.Text = "未知";
            lblCpuArchValue.Text = "未知";

            lblBatteryValue.Text = "N/A %";
            lblVoltageValue.Text = "N/A V";
            lblChargeStatusValue.Text = "未知";
            lblHealthStatusValue.Text = "未知";
            batteryPercent = 0;
            pbBatteryRing.Invalidate();

            lblTotalStorageValue.Text = "N/A GB";
            lblUsedStorageValue.Text = "N/A GB";
            lblFreeStorageValue.Text = "N/A GB";
            storagePercent = 0;
            pbStorageRing.Invalidate();
        }

        private void PbBatteryRing_Paint(object sender, PaintEventArgs e) => DrawRing(e.Graphics, pbBatteryRing.Width, pbBatteryRing.Height, batteryPercent, Color.LimeGreen);

        private void PbStorageRing_Paint(object sender, PaintEventArgs e) => DrawRing(e.Graphics, pbStorageRing.Width, pbStorageRing.Height, storagePercent, Color.DodgerBlue);

        private void DrawRing(Graphics g, int width, int height, int percent, Color progressColor)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            int size = Math.Min(width, height);
            int ringWidth = 12;
            Rectangle rect = new Rectangle(ringWidth / 2, ringWidth / 2, size - ringWidth, size - ringWidth);

            // 绘制背景圆环
            using (Pen backPen = new Pen(Color.FromArgb(80, 80, 80), ringWidth))
            {
                backPen.StartCap = LineCap.Round;
                backPen.EndCap = LineCap.Round;
                g.DrawEllipse(backPen, rect);
            }

            // 绘制进度弧线
            float angle = 360f * percent / 100f;
            using (Pen progressPen = new Pen(progressColor, ringWidth))
            {
                progressPen.StartCap = LineCap.Round;
                progressPen.EndCap = LineCap.Round;
                // 绘制阴影效果（半透明黑色弧线）
                using (Pen shadowPen = new Pen(Color.FromArgb(50, 0, 0, 0), ringWidth))
                {
                    shadowPen.StartCap = LineCap.Round;
                    shadowPen.EndCap = LineCap.Round;
                    g.DrawArc(shadowPen, rect, -90, angle);
                }
                g.DrawArc(progressPen, rect, -90, angle);
            }

            // 绘制百分比文本
            string text = $"{percent}%";
            using (Font font = new Font("Segoe UI", 12, FontStyle.Bold))
            {
                SizeF textSize = g.MeasureString(text, font);
                float x = (width - textSize.Width) / 2;
                float y = (height - textSize.Height) / 2;
                using (Brush shadowBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
                    g.DrawString(text, font, shadowBrush, x + 1, y + 1);
                using (Brush textBrush = new SolidBrush(Color.White))
                    g.DrawString(text, font, textBrush, x, y);
            }
        }

        public void SetAutoUpdateEnabled(bool enabled)
        {
            if (refreshTimer != null)
            {
                if (enabled)
                {
                    // 如果控件可见，则启动定时器
                    if (this.Visible)
                        refreshTimer.Start();
                }
                else
                {
                    refreshTimer.Stop();
                }
            }
        }

        /// <summary>
        /// 当控件的可见性改变时，根据自动更新设置启动或停止定时器。
        /// </summary>
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (refreshTimer != null && Properties.Settings.Default.AutoUpdateEnabled)
                refreshTimer.Enabled = this.Visible;
        }
    }
}