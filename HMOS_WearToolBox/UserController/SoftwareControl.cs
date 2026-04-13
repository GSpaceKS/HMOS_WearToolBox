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
    /// 软件管理控件，提供设备应用列表的查看、安装和卸载功能。
    /// </summary>
    public partial class SoftwareControl : UserControl
    {
        // 上一次的连接状态，用于检测连接变化
        private bool lastConnectionState = false;
        // 上一次刷新时的连接状态，用于避免重复刷新
        private bool? lastRefreshConnectionState = null;
        // 是否正在刷新应用列表，防止并发刷新
        private bool isRefreshing = false;
        // 应用包名到显示名称的映射（从嵌入资源加载）
        private Dictionary<string, string> appNameMap = new Dictionary<string, string>();

        /// <summary>
        /// 初始化软件管理控件。
        /// </summary>
        public SoftwareControl()
        {
            InitializeComponent();

            // 加载应用名称映射文件
            LoadAppNameMap();

            // 当父控件变化时，可能控件被重新显示，需要刷新连接状态
            this.ParentChanged += SoftwareControl_ParentChanged;

            // 配置 ListView 显示列
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

            // 进度条样式初始化为块状，值为0
            progressBarTask.Style = ProgressBarStyle.Blocks;
            progressBarTask.Value = 0;

            // 绑定事件
            this.Load += SoftwareControl_Load;
            this.VisibleChanged += SoftwareControl_VisibleChanged;
            btnRefresh.Click += btnRefresh_Click;
            btnInstall.Click += BtnInstall_Click;
            btnUninstall.Click += BtnUninstall_Click;
        }

        /// <summary>
        /// 从嵌入的资源中加载应用名称映射（JSON 格式）。
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
                System.Diagnostics.Debug.WriteLine($"加载应用名称映射失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 控件加载时更新连接状态。
        /// </summary>
        private async void SoftwareControl_Load(object sender, EventArgs e)
        {
            await UpdateConnectStatusAsync();
        }

        /// <summary>
        /// 父控件改变时，如果控件变为可见，则更新连接状态并可能刷新应用列表。
        /// </summary>
        private async void SoftwareControl_ParentChanged(object sender, EventArgs e)
        {
            if (this.Parent != null && this.Visible)
            {
                bool connected = await UpdateConnectStatusAsync();
                if (connected && (!lastRefreshConnectionState.HasValue || lastRefreshConnectionState.Value != connected))
                {
                    _ = RefreshAppListAsync();
                    lastRefreshConnectionState = connected;
                }
            }
        }

        /// <summary>
        /// 控件可见性改变时，如果变为可见且已连接，则刷新应用列表。
        /// </summary>
        private async void SoftwareControl_VisibleChanged(object sender, EventArgs e)
        {
            bool connected = await UpdateConnectStatusAsync();

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
        /// 外部调用的刷新连接状态方法，用于手动更新连接状态和界面。
        /// </summary>
        public async void RefreshConnectionStatus()
        {
            bool becameConnected = await UpdateConnectStatusAsync();

            if (this.Visible && IsDeviceConnected() && becameConnected)
            {
                _ = RefreshAppListAsync();
                lastRefreshConnectionState = true;
            }
        }

        /// <summary>
        /// 强制刷新应用列表（例如重连后调用）。
        /// </summary>
        public void ForceRefresh()
        {
            if (this.Visible && IsDeviceConnected())
            {
                _ = RefreshAppListAsync();
            }
        }

        /// <summary>
        /// 更新连接状态并返回是否变为已连接。
        /// </summary>
        private async Task<bool> UpdateConnectStatusAsync()
        {
            bool connected = await IsDeviceConnectedAsync();
            bool becameConnected = !lastConnectionState && connected;
            lastConnectionState = connected;

            // 使用 InvokeIfRequired 安全更新 UI
            this.InvokeIfRequired(() =>
            {
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
            });

            return becameConnected;
        }

        /// <summary>
        /// 异步检查设备连接状态。
        /// </summary>
        private async Task<bool> IsDeviceConnectedAsync()
        {
            return await Task.Run(() => IsDeviceConnected());
        }

        /// <summary>
        /// 同步检查设备连接状态（通过 DeviceConnectionHelper）。
        /// </summary>
        private bool IsDeviceConnected()
        {
            try
            {
                return DeviceConnectionHelper.CheckAndUpdateConnectionStatus();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"连接状态检查异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 刷新按钮点击事件：刷新应用列表。
        /// </summary>
        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            if (!await IsDeviceConnectedAsync())
            {
                MessageBox.Show("请先连接设备", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            await RefreshAppListAsync();
        }

        /// <summary>
        /// 安装按钮点击事件：选择 HAP 文件并安装到设备。
        /// </summary>
        private async void BtnInstall_Click(object sender, EventArgs e)
        {
            if (!await IsDeviceConnectedAsync())
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

                // 开始安装，禁用 UI 操作
                this.Cursor = Cursors.WaitCursor;
                btnInstall.Enabled = false;
                btnRefresh.Enabled = false;
                btnUninstall.Enabled = false;

                this.InvokeIfRequired(() =>
                {
                    progressBarTask.Style = ProgressBarStyle.Marquee;  // 安装过程中使用滚动条
                    progressBarTask.Value = 0;
                });

                try
                {
                    // 推送文件到设备
                    string remotePath = "/data/local/tmp/" + Path.GetFileName(filePath);
                    string pushResult = await Task.Run(() => HdcHelper.RunHdcCommand($"file send {filePath} {remotePath}"));
                    if (!pushResult.Contains("success") && !pushResult.Contains("Success"))
                    {
                        MessageBox.Show($"文件推送失败：{pushResult}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.InvokeIfRequired(() =>
                        {
                            progressBarTask.Style = ProgressBarStyle.Blocks;
                            progressBarTask.Value = 0;
                        });
                        return;
                    }

                    // 执行安装命令
                    string installResult = await Task.Run(() => HdcHelper.RunHdcCommand($"install {remotePath}"));
                    if (installResult.Contains("success") || installResult.Contains("Success"))
                    {
                        MessageBox.Show("安装成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.InvokeIfRequired(() =>
                        {
                            progressBarTask.Style = ProgressBarStyle.Blocks;
                            progressBarTask.Value = 100;
                        });
                        await Task.Delay(500);
                        this.InvokeIfRequired(() => progressBarTask.Value = 0);
                        await RefreshAppListAsync();   // 刷新应用列表
                    }
                    else
                    {
                        MessageBox.Show($"安装失败：{installResult}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        this.InvokeIfRequired(() =>
                        {
                            progressBarTask.Style = ProgressBarStyle.Blocks;
                            progressBarTask.Value = 0;
                        });
                    }

                    // 清理临时文件
                    await Task.Run(() => HdcHelper.RunHdcCommand($"shell rm {remotePath}"));
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"安装出错：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.InvokeIfRequired(() =>
                    {
                        progressBarTask.Style = ProgressBarStyle.Blocks;
                        progressBarTask.Value = 0;
                    });
                }
                finally
                {
                    this.Cursor = Cursors.Default;
                    btnInstall.Enabled = true;
                    btnRefresh.Enabled = true;
                    btnUninstall.Enabled = true;
                }
            }
        }

        /// <summary>
        /// 卸载按钮点击事件：卸载选中的应用。
        /// </summary>
        private async void BtnUninstall_Click(object sender, EventArgs e)
        {
            if (!await IsDeviceConnectedAsync())
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

            // 开始卸载，禁用 UI 操作
            this.Cursor = Cursors.WaitCursor;
            btnUninstall.Enabled = false;
            btnRefresh.Enabled = false;
            btnInstall.Enabled = false;

            this.InvokeIfRequired(() =>
            {
                progressBarTask.Style = ProgressBarStyle.Marquee;
                progressBarTask.Value = 0;
            });

            try
            {
                string result = await Task.Run(() => HdcHelper.RunHdcCommand($"uninstall {packageName}"));
                if (result.Contains("success") || result.Contains("Success"))
                {
                    MessageBox.Show("卸载成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.InvokeIfRequired(() =>
                    {
                        progressBarTask.Style = ProgressBarStyle.Blocks;
                        progressBarTask.Value = 100;
                    });
                    await Task.Delay(500);
                    this.InvokeIfRequired(() => progressBarTask.Value = 0);
                    await RefreshAppListAsync();   // 刷新应用列表
                }
                else
                {
                    MessageBox.Show($"卸载失败：{result}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.InvokeIfRequired(() =>
                    {
                        progressBarTask.Style = ProgressBarStyle.Blocks;
                        progressBarTask.Value = 0;
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"卸载出错：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.InvokeIfRequired(() =>
                {
                    progressBarTask.Style = ProgressBarStyle.Blocks;
                    progressBarTask.Value = 0;
                });
            }
            finally
            {
                this.Cursor = Cursors.Default;
                btnUninstall.Enabled = true;
                btnRefresh.Enabled = true;
                btnInstall.Enabled = true;
            }
        }

        /// <summary>
        /// 异步刷新应用列表，从设备获取所有已安装应用并显示。
        /// </summary>
        private DateTime _lastRefreshTime = DateTime.MinValue;
        private async Task RefreshAppListAsync()
        {
            if (isRefreshing) return;
            if ((DateTime.Now - _lastRefreshTime).TotalSeconds < 3)
                return;
            _lastRefreshTime = DateTime.Now;
            isRefreshing = true;

            if (!await IsDeviceConnectedAsync())
            {
                ClearAppList();
                isRefreshing = false;
                return;
            }

            // 显示进度条为滚动状态
            this.InvokeIfRequired(() =>
            {
                progressBarTask.Style = ProgressBarStyle.Marquee;
                progressBarTask.Value = 0;
            });

            this.Cursor = Cursors.WaitCursor;
            btnRefresh.Enabled = false;
            btnInstall.Enabled = false;
            btnUninstall.Enabled = false;

            try
            {
                string output = await Task.Run(() => HdcHelper.RunHdcCommand("shell bm dump -a"));
                var apps = ParseAppList(output);
                DisplayApps(apps);

                // 完成刷新，将进度条设为完成
                this.InvokeIfRequired(() =>
                {
                    progressBarTask.Style = ProgressBarStyle.Blocks;
                    progressBarTask.Value = 100;
                });
                await Task.Delay(500);
                this.InvokeIfRequired(() => progressBarTask.Value = 0);

                if (await IsDeviceConnectedAsync())
                    lastRefreshConnectionState = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取应用列表失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.InvokeIfRequired(() =>
                {
                    progressBarTask.Style = ProgressBarStyle.Blocks;
                    progressBarTask.Value = 0;
                });
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
        /// 解析应用列表输出，支持两种格式（bm dump -a 或 pm list packages）。
        /// </summary>
        private List<AppInfo> ParseAppList(string output)
        {
            var apps = new List<AppInfo>();
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // 检查输出是否包含键值对格式（BundleName:）
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
                // 旧格式：每个应用一行，以 "ID:" 分隔
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

            // 如果仍未获取到任何应用，尝试使用 pm list packages -3 获取第三方应用列表
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

            // 使用映射表将包名转换为可读的应用名称
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
        /// 将应用列表显示在 ListView 中。
        /// </summary>
        private void DisplayApps(List<AppInfo> apps)
        {
            // 安全更新 ListView
            this.InvokeIfRequired(() =>
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
            });
        }

        /// <summary>
        /// 清空应用列表显示。
        /// </summary>
        public void ClearAppList()
        {
            this.InvokeIfRequired(() => listViewApps.Items.Clear());
        }

        /// <summary>
        /// 内部应用信息类，用于存储应用名称和包名。
        /// </summary>
        private class AppInfo
        {
            public string AppName { get; set; } = "";
            public string PackageName { get; set; } = "";
        }
    }

    /// <summary>
    /// 为 Control 提供扩展方法，用于安全地跨线程调用 UI 操作。
    /// </summary>
    public static class ControlExtensions
    {
        /// <summary>
        /// 如果当前线程不是控件的创建线程，则使用 BeginInvoke 执行操作；否则直接执行。
        /// </summary>
        public static void InvokeIfRequired(this Control control, Action action)
        {
            if (control.InvokeRequired)
            {
                // 如果控件句柄尚未创建，则延迟到句柄创建后执行
                if (!control.IsHandleCreated)
                {
                    control.HandleCreated += (s, e) => control.BeginInvoke(action);
                    return;
                }
                control.BeginInvoke(action);
            }
            else
            {
                action();
            }
        }
    }
}