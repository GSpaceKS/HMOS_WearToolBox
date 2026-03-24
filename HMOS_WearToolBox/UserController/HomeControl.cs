#nullable disable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
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
    /// 首页控件，展示设备信息、电池和存储状态，并提供设备管理功能。
    /// </summary>
    public partial class HomeControl : UserControl
    {
        // 定时刷新设备信息的计时器
        private WinFormsTimer refreshTimer;
        // 当前电池电量百分比（用于绘制环形图）
        private int batteryPercent = 0;
        // 当前存储使用率百分比（用于绘制环形图）
        private int storagePercent = 0;
        // 防止并发刷新操作
        private bool isRefreshing = false;

        public HomeControl()
        {
            InitializeComponent();

            // 注册控件事件
            btnReconnect.Click += BtnReconnect_Click;
            btnDisconnect.Click += BtnDisconnect_Click;
            btnAddDevice.Click += BtnAddDevice_Click;
            btnDeleteDevice.Click += BtnDeleteDevice_Click;
            cmbDevices.MouseClick += CmbDevices_MouseClick;

            // 自定义绘制环形进度条
            pbBatteryRing.Paint += PbBatteryRing_Paint;
            pbStorageRing.Paint += PbStorageRing_Paint;
            pbBatteryRing.Resize += (s, e) => pbBatteryRing.Invalidate();
            pbStorageRing.Resize += (s, e) => pbStorageRing.Invalidate();

            // 加载已保存的设备列表
            LoadDevices();

            // 初始化定时器，每3分钟刷新一次设备信息
            refreshTimer = new WinFormsTimer();
            refreshTimer.Interval = 180000; // 3分钟
            refreshTimer.Tick += async (s, e) => await RefreshDeviceInfoAsync();
            refreshTimer.Start();
        }

        /// <summary>
        /// 从 DeviceManager 加载设备列表并填充到下拉框中。
        /// </summary>
        private void LoadDevices()
        {
            cmbDevices.Items.Clear();
            var devices = DeviceManager.GetDevices();
            if (devices.Count == 0)
            {
                cmbDevices.Text = string.Empty;
                cmbDevices.SelectedIndex = -1;
                return;
            }

            foreach (var device in devices)
            {
                cmbDevices.Items.Add($"{device.Name} ({device.IpAddress})");
            }
            cmbDevices.SelectedIndex = 0;
        }

        /// <summary>
        /// 添加设备按钮点击事件：输入 IP 地址并尝试连接。
        /// </summary>
        private void BtnAddDevice_Click(object sender, EventArgs e)
        {
            string ip = Microsoft.VisualBasic.Interaction.InputBox("请输入设备 IP 地址 (例如 192.168.1.100:5555)", "添加设备", "192.168.");
            if (string.IsNullOrWhiteSpace(ip)) return;

            string result = HdcHelper.RunHdcCommand($"tconn {ip}");
            Thread.Sleep(500);
            string devices = HdcHelper.RunHdcCommand("list targets");
            if (devices.Contains(ip.Split(':')[0]))
            {
                var device = new DeviceInfo
                {
                    Name = "新设备",
                    IpAddress = ip,
                    LastConnected = DateTime.Now,
                    IsConnected = true
                };
                DeviceManager.AddDevice(device);
                LoadDevices();
                _ = RefreshDeviceInfoAsync();
                MessageBox.Show("连接成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"连接失败：{result}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 删除设备按钮点击事件：从下拉框选中设备并删除。
        /// </summary>
        private void BtnDeleteDevice_Click(object sender, EventArgs e)
        {
            if (cmbDevices.SelectedItem == null) return;

            int index = cmbDevices.SelectedIndex;
            var device = DeviceManager.GetDevices()[index];

            if (MessageBox.Show($"确定要删除设备 {device.Name} 吗？", "确认删除", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            DeviceManager.RemoveDevice(device.Id);
            LoadDevices();
            ResetUI();

            if (cmbDevices.Items.Count > 0)
            {
                _ = RefreshDeviceInfoAsync();
            }
        }

        /// <summary>
        /// 重连按钮点击事件：尝试重新连接当前选中的设备，并显示等待窗口。
        /// </summary>
        private async void BtnReconnect_Click(object sender, EventArgs e)
        {
            if (cmbDevices.SelectedItem == null) return;
            int index = cmbDevices.SelectedIndex;
            var device = DeviceManager.GetDevices()[index];

            // 创建等待窗口，用于显示重连进度
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
                Text = "正在尝试重连设备并获取数据...",
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

            var workTask = Task.Run(async () =>
            {
                try
                {
                    string result = HdcHelper.RunHdcCommand($"tconn {device.IpAddress}");
                    if (cts.Token.IsCancellationRequested) return false;
                    if (!result.Contains("Connect OK")) return false;

                    device.IsConnected = true;
                    device.LastConnected = DateTime.Now;
                    DeviceManager.UpdateDevice(device);

                    await RefreshDeviceInfoAsync();
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
                MessageBox.Show("重连设备失败，请检查设备状态", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool success = await workTask;
            if (!success)
            {
                MessageBox.Show("重连设备失败，请检查设备状态", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 断开连接按钮点击事件：断开当前设备连接并清空 UI。
        /// </summary>
        private void BtnDisconnect_Click(object sender, EventArgs e)
        {
            if (cmbDevices.SelectedItem == null) return;
            HdcHelper.RunHdcCommand("kill");
            int index = cmbDevices.SelectedIndex;
            var device = DeviceManager.GetDevices()[index];
            device.IsConnected = false;
            DeviceManager.UpdateDevice(device);
            ResetUI();
            MessageBox.Show("已断开", "提示");

            var mainForm = this.ParentForm as MainForm;
            mainForm?.ClearSoftwareList();
        }

        /// <summary>
        /// 设备下拉框鼠标点击事件（右键删除快捷方式）。
        /// </summary>
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

        /// <summary>
        /// 异步刷新设备信息，包括名称、型号、系统版本、电池和存储等。
        /// </summary>
        private async Task RefreshDeviceInfoAsync()
        {
            if (isRefreshing) return;
            if (cmbDevices.SelectedItem == null) return;

            int index = cmbDevices.SelectedIndex;
            var device = DeviceManager.GetDevices()[index];
            if (!device.IsConnected)
            {
                ResetUI();
                return;
            }

            // 验证设备是否在线（通过 list targets）
            string targets = await Task.Run(() => HdcHelper.RunHdcCommand("list targets"));
            if (!targets.Contains(device.IpAddress.Split(':')[0]))
            {
                device.IsConnected = false;
                DeviceManager.UpdateDevice(device);
                ResetUI();
                MessageBox.Show("设备已断开", "提示");
                return;
            }

            isRefreshing = true;
            this.Cursor = Cursors.WaitCursor;

            try
            {
                // 并行获取各项设备信息
                string rawName = await Task.Run(() => HdcHelper.RunHdcCommand("shell param get const.product.name"));
                string rawModel = await Task.Run(() => HdcHelper.RunHdcCommand("shell param get const.product.model"));
                string rawSysVer = await Task.Run(() => HdcHelper.RunHdcCommand("shell param get const.product.software.version"));
                string rawApiVer = await Task.Run(() => HdcHelper.RunHdcCommand("shell param get const.ohos.apiversion"));
                string rawCpuArch = await Task.Run(() => HdcHelper.RunHdcCommand("shell param get const.product.cpu.abilist"));
                string resolutionOutput = await Task.Run(() => HdcHelper.RunHdcCommand("shell hidumper -s RenderService -a screen"));
                string batteryOutput = await Task.Run(() => HdcHelper.RunHdcCommand("shell hidumper -s BatteryService -a -i"));
                string storageOutput = await Task.Run(() => HdcHelper.RunHdcCommand("shell df -h"));

                // 提取实际值（去掉命令输出中的前缀）
                string name = ExtractParamValue(rawName);
                string model = ExtractParamValue(rawModel);
                string sysVersion = ExtractParamValue(rawSysVer);
                string apiVersion = ExtractParamValue(rawApiVer);
                string cpuArch = ExtractParamValue(rawCpuArch);

                // 更新界面（必须在 UI 线程）
                this.Invoke(new Action(() =>
                {
                    lblDeviceNameValue.Text = name;
                    lblDeviceModelValue.Text = model;
                    lblSysVersionValue.Text = sysVersion;
                    lblApiVersionValue.Text = apiVersion;
                    lblCpuArchValue.Text = cpuArch;

                    string resolution = ExtractResolution(resolutionOutput);
                    lblResolutionValue.Text = resolution;

                    var batteryInfo = BatteryParser.Parse(batteryOutput);
                    UpdateBatteryUI(batteryInfo);

                    var storageInfo = StorageParser.Parse(storageOutput);
                    UpdateStorageUI(storageInfo);
                }));

                // 如果获取到的设备名称与已保存的不同，则更新设备名称并刷新下拉列表
                if (!string.IsNullOrWhiteSpace(name) && name != "未知" && name != device.Name)
                {
                    device.Name = name;
                    DeviceManager.UpdateDevice(device);
                    LoadDevices(); // 刷新下拉列表
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
        /// 从原始输出中提取参数值（格式为 "key: value"）。
        /// </summary>
        private string ExtractParamValue(string output)
        {
            if (string.IsNullOrWhiteSpace(output)) return "未知";
            int colonIndex = output.IndexOf(':');
            if (colonIndex >= 0 && colonIndex < output.Length - 1)
            {
                return output.Substring(colonIndex + 1).Trim();
            }
            return output.Trim();
        }

        /// <summary>
        /// 从屏幕分辨率输出中提取 "物理分辨率=宽x高" 部分。
        /// </summary>
        private string ExtractResolution(string output)
        {
            var match = System.Text.RegularExpressions.Regex.Match(output, @"physical resolution=(\d+x\d+)");
            return match.Success ? match.Groups[1].Value : "未知";
        }

        /// <summary>
        /// 更新电池相关 UI 并触发环形图重绘。
        /// </summary>
        private void UpdateBatteryUI(BatteryInfo info)
        {
            lblBatteryValue.Text = $"{info.Capacity}%";
            lblVoltageValue.Text = $"{info.Voltage / 1000000.0:F3} V";
            lblChargeStatusValue.Text = GetChargeStatusText(info.ChargingStatus);
            lblHealthStatusValue.Text = GetHealthStatusText(info.HealthState);

            batteryPercent = info.Capacity;
            pbBatteryRing.Invalidate();
        }

        /// <summary>
        /// 将充电状态代码转换为显示文本。
        /// </summary>
        private string GetChargeStatusText(int status)
        {
            return status switch
            {
                0 => "未充电",
                1 => "正在充电",
                2 => "已充满",
                _ => "未知"
            };
        }

        /// <summary>
        /// 将电池健康状态代码转换为显示文本。
        /// </summary>
        private string GetHealthStatusText(int health)
        {
            return health switch
            {
                0 => "良好",
                1 => "过热",
                2 => "过压",
                3 => "电压低",
                4 => "故障",
                _ => "未知"
            };
        }

        /// <summary>
        /// 更新存储相关 UI 并触发环形图重绘。
        /// </summary>
        private void UpdateStorageUI(StorageInfo info)
        {
            double totalGB = info.Total / (1024.0 * 1024);
            double usedGB = info.Used / (1024.0 * 1024);
            double freeGB = info.Free / (1024.0 * 1024);

            lblTotalStorageValue.Text = $"{totalGB:F2} GB";
            lblUsedStorageValue.Text = $"{usedGB:F2} GB";
            lblFreeStorageValue.Text = $"{freeGB:F2} GB";

            if (totalGB > 0)
            {
                storagePercent = (int)((usedGB / totalGB) * 100);
                pbStorageRing.Invalidate();
            }
            else
            {
                storagePercent = 0;
                pbStorageRing.Invalidate();
            }
        }

        /// <summary>
        /// 重置所有 UI 显示为默认值（未连接状态）。
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

        /// <summary>
        /// 电池环形进度条的自定义绘制。
        /// </summary>
        private void PbBatteryRing_Paint(object sender, PaintEventArgs e)
        {
            DrawRing(e.Graphics, pbBatteryRing.Width, pbBatteryRing.Height, batteryPercent, Color.LimeGreen);
        }

        /// <summary>
        /// 存储环形进度条的自定义绘制。
        /// </summary>
        private void PbStorageRing_Paint(object sender, PaintEventArgs e)
        {
            DrawRing(e.Graphics, pbStorageRing.Width, pbStorageRing.Height, storagePercent, Color.DodgerBlue);
        }

        /// <summary>
        /// 绘制环形进度条的通用方法。
        /// </summary>
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

            // 绘制进度圆弧
            float angle = 360f * percent / 100f;
            using (Pen progressPen = new Pen(progressColor, ringWidth))
            {
                progressPen.StartCap = LineCap.Round;
                progressPen.EndCap = LineCap.Round;
                using (Pen shadowPen = new Pen(Color.FromArgb(50, 0, 0, 0), ringWidth))
                {
                    shadowPen.StartCap = LineCap.Round;
                    shadowPen.EndCap = LineCap.Round;
                    g.DrawArc(shadowPen, rect, -90, angle);
                }
                g.DrawArc(progressPen, rect, -90, angle);
            }

            // 绘制中心百分比文本
            string text = $"{percent}%";
            using (Font font = new Font("Segoe UI", 12, FontStyle.Bold))
            {
                SizeF textSize = g.MeasureString(text, font);
                float x = (width - textSize.Width) / 2;
                float y = (height - textSize.Height) / 2;
                using (Brush shadowBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
                {
                    g.DrawString(text, font, shadowBrush, x + 1, y + 1);
                }
                using (Brush textBrush = new SolidBrush(Color.White))
                {
                    g.DrawString(text, font, textBrush, x, y);
                }
            }
        }

        /// <summary>
        /// 控件可见性改变时，同步控制定时器的启用状态（仅当可见时定时刷新）。
        /// </summary>
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (refreshTimer != null)
                refreshTimer.Enabled = this.Visible;
        }
    }
}