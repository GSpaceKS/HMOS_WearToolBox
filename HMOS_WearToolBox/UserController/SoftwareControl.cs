#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using HMOS_WearToolBox.Helper;
using HMOS_WearToolBox.Manager;

namespace HMOS_WearToolBox.UserController
{
    /// <summary>
    /// 软件管理控件，提供应用列表刷新、安装和卸载功能。
    /// </summary>
    public partial class SoftwareControl : UserControl
    {
        // 上一次的设备连接状态（用于检测状态变化）
        private bool lastConnectionState = false;
        // 记录上次刷新时的连接状态（避免重复刷新）
        private bool? lastRefreshConnectionState = null;
        // 防止并发刷新操作
        private bool isRefreshing = false;

        // 应用名称映射表（包名 -> 显示名称）
        private Dictionary<string, string> appNameMap = new Dictionary<string, string>();

        public SoftwareControl()
        {
            InitializeComponent();

            LoadAppNameMap(); // 加载预定义的应用名称映射

            this.ParentChanged += SoftwareControl_ParentChanged;

            // 配置 ListView 的列
            if (listViewApps.Columns.Count >= 2)
            {
                listViewApps.Columns[0].Text = "应用名称";
                listViewApps.Columns[0].Width = 250;
                listViewApps.Columns[0].TextAlign = HorizontalAlignment.Center;
                listViewApps.Columns[1].Text = "包名";
                listViewApps.Columns[1].Width = 450;
                if (listViewApps.Columns.Count > 2)
                    listViewApps.Columns.RemoveAt(2);
            }

            listViewApps.View = View.Details;

            // 初始化进度条样式为块状，初始值为0
            progressBarTask.Style = ProgressBarStyle.Blocks;
            progressBarTask.Value = 0;

            // 注册事件处理程序
            this.Load += SoftwareControl_Load;
            this.VisibleChanged += SoftwareControl_VisibleChanged;
            btnRefresh.Click += btnRefresh_Click;
            btnInstall.Click += BtnInstall_Click;
            btnUninstall.Click += BtnUninstall_Click;
        }

        /// <summary>
        /// 从嵌入的资源中加载应用名称映射文件（app_names.json）。
        /// </summary>
        private void LoadAppNameMap()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(r => r.EndsWith("app_names.json", StringComparison.OrdinalIgnoreCase));
                if (resourceName != null)
                {
                    using (var stream = assembly.GetManifestResourceStream(resourceName))
                    using (var reader = new StreamReader(stream))
                    {
                        string json = reader.ReadToEnd();
                        var map = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                        if (map != null)
                            appNameMap = map;
                    }
                }
            }
            catch (Exception ex)
            {
                // 加载失败不影响主功能，仅记录异常
                System.Diagnostics.Debug.WriteLine($"加载应用名称映射失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 控件加载完成时调用，仅更新连接状态（不刷新应用列表）。
        /// </summary>
        private async void SoftwareControl_Load(object sender, EventArgs e)
        {
            UpdateConnectStatus();
        }

        /// <summary>
        /// 当控件被添加到父容器时触发，若控件可见且设备已连接则刷新列表。
        /// </summary>
        private void SoftwareControl_ParentChanged(object sender, EventArgs e)
        {
            if (this.Parent != null && this.Visible)
            {
                bool connected = UpdateConnectStatus();
                if (connected && (!lastRefreshConnectionState.HasValue || lastRefreshConnectionState.Value != connected))
                {
                    _ = RefreshAppListAsync();
                    lastRefreshConnectionState = connected;
                }
            }
        }

        /// <summary>
        /// 当控件的可见性发生变化时触发，若变为可见且设备已连接且状态未记录则刷新。
        /// </summary>
        private void SoftwareControl_VisibleChanged(object sender, EventArgs e)
        {
            bool connected = UpdateConnectStatus();

            if (this.Visible && connected)
            {
                if (!lastRefreshConnectionState.HasValue || lastRefreshConnectionState.Value != connected)
                {
                    _ = RefreshAppListAsync();
                    lastRefreshConnectionState = connected;
                }
            }
        }

        /// <summary>
        /// 外部调用此方法可更新连接状态并触发自动刷新（如果设备刚刚连接）。
        /// </summary>
        public void RefreshConnectionStatus()
        {
            bool becameConnected = UpdateConnectStatus();

            if (this.Visible && IsDeviceConnected() && becameConnected)
            {
                _ = RefreshAppListAsync();
                lastRefreshConnectionState = true;
            }
        }

        /// <summary>
        /// 更新连接状态 UI，返回当前是否已连接，并检测是否刚刚变为已连接。
        /// </summary>
        /// <returns>true 表示当前设备已连接，false 表示未连接</returns>
        private bool UpdateConnectStatus()
        {
            bool connected = IsDeviceConnected();
            bool becameConnected = !lastConnectionState && connected;
            lastConnectionState = connected;

            if (connected)
            {
                lblConnectStatus.Text = "已连接";
                lblConnectStatus.ForeColor = System.Drawing.Color.Green;
                btnRefresh.Enabled = true;
                btnInstall.Enabled = true;
                btnUninstall.Enabled = true;
            }
            else
            {
                lblConnectStatus.Text = "未连接";
                lblConnectStatus.ForeColor = System.Drawing.Color.Red;
                btnRefresh.Enabled = false;
                btnInstall.Enabled = false;
                btnUninstall.Enabled = false;
                ClearAppList();
            }

            return becameConnected;
        }

        /// <summary>
        /// 检测当前是否有设备处于已连接状态。
        /// </summary>
        private bool IsDeviceConnected()
        {
            var devices = DeviceManager.GetDevices();
            bool anyManagerConnected = devices.Any(d => d.IsConnected);
            if (!anyManagerConnected) return false;

            try
            {
                string targets = HdcHelper.RunHdcCommand("list targets");
                bool anyOnline = false;

                foreach (var device in devices)
                {
                    if (!device.IsConnected) continue;
                    string ipPart = device.IpAddress.Split(':')[0];
                    if (targets.Contains(ipPart))
                    {
                        anyOnline = true;
                    }
                    else
                    {
                        // 设备不在线，修正 DeviceManager 中的状态
                        device.IsConnected = false;
                        DeviceManager.UpdateDevice(device);
                    }
                }
                return anyOnline;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 刷新按钮点击事件，手动刷新应用列表。
        /// </summary>
        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            if (!IsDeviceConnected())
            {
                MessageBox.Show("请先连接设备", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            await RefreshAppListAsync();
        }

        /// <summary>
        /// 安装按钮点击事件，选择 HAP 文件并安装到设备。
        /// </summary>
        private async void BtnInstall_Click(object sender, EventArgs e)
        {
            if (!IsDeviceConnected())
            {
                MessageBox.Show("请先连接设备", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "选择要安装的 HAP 文件";
                ofd.Filter = "HAP文件|*.hap|所有文件|*.*";
                ofd.Multiselect = false;
                if (ofd.ShowDialog() != DialogResult.OK)
                    return;

                string filePath = ofd.FileName;
                if (MessageBox.Show($"确定要安装 {Path.GetFileName(filePath)} 吗？", "确认安装", MessageBoxButtons.YesNo) != DialogResult.Yes)
                    return;

                // 准备安装，禁用按钮并显示等待光标
                this.Cursor = Cursors.WaitCursor;
                btnInstall.Enabled = false;
                btnRefresh.Enabled = false;
                btnUninstall.Enabled = false;

                // 进度条设为不确定（表示进行中）
                this.Invoke(new Action(() =>
                {
                    progressBarTask.Style = ProgressBarStyle.Marquee;
                    progressBarTask.Value = 0;
                }));

                try
                {
                    // 推送文件到设备临时目录
                    string remotePath = "/data/local/tmp/" + Path.GetFileName(filePath);
                    string pushResult = await Task.Run(() => HdcHelper.RunHdcCommand($"file send {filePath} {remotePath}"));
                    if (!pushResult.Contains("success") && !pushResult.Contains("Success"))
                    {
                        MessageBox.Show($"文件推送失败：{pushResult}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.Invoke(new Action(() =>
                        {
                            progressBarTask.Style = ProgressBarStyle.Blocks;
                            progressBarTask.Value = 0;
                        }));
                        return;
                    }

                    // 执行安装命令
                    string installResult = await Task.Run(() => HdcHelper.RunHdcCommand($"install {remotePath}"));
                    if (installResult.Contains("success") || installResult.Contains("Success"))
                    {
                        MessageBox.Show("安装成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        // 成功：进度条显示100%后归零
                        this.Invoke(new Action(() =>
                        {
                            progressBarTask.Style = ProgressBarStyle.Blocks;
                            progressBarTask.Value = 100;
                        }));
                        await Task.Delay(500);
                        this.Invoke(new Action(() => progressBarTask.Value = 0));
                        await RefreshAppListAsync(); // 刷新应用列表
                    }
                    else
                    {
                        MessageBox.Show($"安装失败：{installResult}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.Invoke(new Action(() =>
                        {
                            progressBarTask.Style = ProgressBarStyle.Blocks;
                            progressBarTask.Value = 0;
                        }));
                    }

                    // 清理临时文件
                    await Task.Run(() => HdcHelper.RunHdcCommand($"shell rm {remotePath}"));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"安装出错：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Invoke(new Action(() =>
                    {
                        progressBarTask.Style = ProgressBarStyle.Blocks;
                        progressBarTask.Value = 0;
                    }));
                }
                finally
                {
                    // 恢复界面状态
                    this.Cursor = Cursors.Default;
                    btnInstall.Enabled = true;
                    btnRefresh.Enabled = true;
                    btnUninstall.Enabled = true;
                }
            }
        }

        /// <summary>
        /// 卸载按钮点击事件，卸载选中的应用。
        /// </summary>
        private async void BtnUninstall_Click(object sender, EventArgs e)
        {
            if (!IsDeviceConnected())
            {
                MessageBox.Show("请先连接设备", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (listViewApps.SelectedItems.Count == 0)
            {
                MessageBox.Show("请先选择要卸载的应用", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string packageName = listViewApps.SelectedItems[0].SubItems[1].Text;
            if (string.IsNullOrWhiteSpace(packageName))
            {
                MessageBox.Show("无法获取包名", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (MessageBox.Show($"确定要卸载 {packageName} 吗？", "确认卸载", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            // 准备卸载，禁用按钮并显示等待光标
            this.Cursor = Cursors.WaitCursor;
            btnUninstall.Enabled = false;
            btnRefresh.Enabled = false;
            btnInstall.Enabled = false;

            // 进度条设为不确定（表示进行中）
            this.Invoke(new Action(() =>
            {
                progressBarTask.Style = ProgressBarStyle.Marquee;
                progressBarTask.Value = 0;
            }));

            try
            {
                string result = await Task.Run(() => HdcHelper.RunHdcCommand($"uninstall {packageName}"));
                if (result.Contains("success") || result.Contains("Success"))
                {
                    MessageBox.Show("卸载成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    // 成功：进度条显示100%后归零
                    this.Invoke(new Action(() =>
                    {
                        progressBarTask.Style = ProgressBarStyle.Blocks;
                        progressBarTask.Value = 100;
                    }));
                    await Task.Delay(500);
                    this.Invoke(new Action(() => progressBarTask.Value = 0));
                    await RefreshAppListAsync(); // 刷新应用列表
                }
                else
                {
                    MessageBox.Show($"卸载失败：{result}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Invoke(new Action(() =>
                    {
                        progressBarTask.Style = ProgressBarStyle.Blocks;
                        progressBarTask.Value = 0;
                    }));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"卸载出错：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Invoke(new Action(() =>
                {
                    progressBarTask.Style = ProgressBarStyle.Blocks;
                    progressBarTask.Value = 0;
                }));
            }
            finally
            {
                // 恢复界面状态
                this.Cursor = Cursors.Default;
                btnUninstall.Enabled = true;
                btnRefresh.Enabled = true;
                btnInstall.Enabled = true;
            }
        }

        /// <summary>
        /// 异步刷新应用列表，从设备获取已安装应用并显示。
        /// </summary>
        private async Task RefreshAppListAsync()
        {
            if (isRefreshing) return;
            isRefreshing = true;

            if (!IsDeviceConnected())
            {
                ClearAppList();
                isRefreshing = false;
                return;
            }

            // 进度条设为不确定（表示正在加载）
            this.Invoke(new Action(() =>
            {
                progressBarTask.Style = ProgressBarStyle.Marquee;
                progressBarTask.Value = 0;
            }));

            this.Cursor = Cursors.WaitCursor;
            btnRefresh.Enabled = false;
            btnInstall.Enabled = false;
            btnUninstall.Enabled = false;

            try
            {
                string output = await Task.Run(() => HdcHelper.RunHdcCommand("shell bm dump -a"));
                var apps = ParseAppList(output);
                DisplayApps(apps);

                // 成功：进度条显示100%后归零
                this.Invoke(new Action(() =>
                {
                    progressBarTask.Style = ProgressBarStyle.Blocks;
                    progressBarTask.Value = 100;
                }));
                await Task.Delay(500);
                this.Invoke(new Action(() => progressBarTask.Value = 0));

                // 刷新成功后更新记录状态（当前已连接）
                if (IsDeviceConnected())
                    lastRefreshConnectionState = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取应用列表失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Invoke(new Action(() =>
                {
                    progressBarTask.Style = ProgressBarStyle.Blocks;
                    progressBarTask.Value = 0;
                }));
            }
            finally
            {
                this.Cursor = Cursors.Default;
                btnRefresh.Enabled = true;
                btnInstall.Enabled = true;
                btnUninstall.Enabled = true;
                isRefreshing = false;
            }
        }

        /// <summary>
        /// 解析 hdc 命令输出的应用列表，支持两种输出格式（键值对格式或简单列表）。
        /// </summary>
        /// <param name="output">hdc 命令的输出文本</param>
        /// <returns>应用信息列表</returns>
        private List<AppInfo> ParseAppList(string output)
        {
            var apps = new List<AppInfo>();
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            bool isKeyValueFormat = lines.Any(l => l.Contains("BundleName:"));

            if (isKeyValueFormat)
            {
                AppInfo current = null;
                foreach (var line in lines)
                {
                    if (line.StartsWith("BundleName:"))
                    {
                        if (current != null)
                            apps.Add(current);
                        current = new AppInfo();
                        current.PackageName = line.Substring("BundleName:".Length).Trim();
                    }
                    else if (line.StartsWith("Label:") && current != null)
                    {
                        current.AppName = line.Substring("Label:".Length).Trim();
                    }
                }
                if (current != null)
                    apps.Add(current);
            }
            else
            {
                bool foundFirst = false;
                foreach (var line in lines)
                {
                    string trimmed = line.Trim();
                    if (!foundFirst && trimmed.StartsWith("ID:"))
                    {
                        foundFirst = true;
                        continue;
                    }
                    if (foundFirst && !string.IsNullOrWhiteSpace(trimmed))
                    {
                        if (trimmed.StartsWith("ID:")) continue;
                        apps.Add(new AppInfo
                        {
                            PackageName = trimmed,
                            AppName = trimmed
                        });
                    }
                }
            }

            // 如果解析结果为空，尝试使用 pm list packages 命令作为备选
            if (apps.Count == 0)
            {
                string packageOutput = HdcHelper.RunHdcCommand("shell pm list packages -3");
                var packageLines = packageOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in packageLines)
                {
                    if (line.StartsWith("package:"))
                    {
                        string pkg = line.Substring("package:".Length).Trim();
                        apps.Add(new AppInfo { PackageName = pkg, AppName = pkg });
                    }
                }
            }

            // 应用名称映射：如果映射表中存在显示名称则使用，否则显示“未知”
            foreach (var app in apps)
            {
                if (appNameMap.TryGetValue(app.PackageName, out string displayName))
                    app.AppName = displayName;
                else
                    app.AppName = "未知";
            }

            return apps;
        }

        /// <summary>
        /// 在 ListView 中显示应用列表。
        /// </summary>
        /// <param name="apps">应用信息列表</param>
        private void DisplayApps(List<AppInfo> apps)
        {
            listViewApps.Items.Clear();
            foreach (var app in apps)
            {
                var item = new ListViewItem(app.AppName);
                item.SubItems.Add(app.PackageName);
                listViewApps.Items.Add(item);
            }
            if (apps.Count == 0)
            {
                listViewApps.Items.Add(new ListViewItem("未获取到应用列表"));
            }
        }

        /// <summary>
        /// 清空应用列表显示。
        /// </summary>
        public void ClearAppList()
        {
            listViewApps.Items.Clear();
        }

        /// <summary>
        /// 内部类：应用信息，包含显示名称和包名。
        /// </summary>
        private class AppInfo
        {
            public string AppName { get; set; } = "";
            public string PackageName { get; set; } = "";
        }
    }
}