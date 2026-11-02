#nullable disable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using HMOS_WearToolBox.Helper;
using HMOS_WearToolBox.Manager;

namespace HMOS_WearToolBox.UserController
{
    public partial class SoftwareControl : UserControl
    {
        private Button btnSelfSign;

        private bool lastConnectionState;
        private bool? lastRefreshConnectionState;
        private bool isRefreshing;

        public SoftwareControl()
        {
            InitializeComponent();

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
            btnUninstall.Click += BtnUninstall_Click;

            // 「自签安装」：第三方 HAP 的包名被别人占着，必须换包名 + 用自己 AGC 的调试证书重签
            btnSelfSign = new Button
            {
                Text = "自签安装"
            };
            flowLayoutPanelControl.Controls.Add(btnSelfSign);
            btnSelfSign.Click += BtnSelfSign_Click;

            // 三个工具栏按钮必须同尺寸：AutoSize 会让「自签安装」按字体实际高度撑开，
            // 而 Designer 里那两个是固定 112x34，缩放后就会一个高一个矮。
            StyleToolbarButton(btnRefresh);
            StyleToolbarButton(btnUninstall);
            StyleToolbarButton(btnSelfSign);

            // 没连手表时不让点：安装的最后一步必须走到设备上，先关掉省得白填一遍表单。
            btnSelfSign.Enabled = false;
        }

        private static void StyleToolbarButton(Button button)
        {
            button.AutoSize = false;
            button.Size = new Size(112, 34);
            button.Margin = new Padding(3, 3, 3, 3);
            button.UseVisualStyleBackColor = true;
        }

        private void SetProgressBusy()
        {
            this.InvokeIfRequired(() =>
            {
                progressBarTask.Style = ProgressBarStyle.Marquee;
                progressBarTask.Value = 0;
            });
        }

        private void SetProgressIdle()
        {
            this.InvokeIfRequired(() =>
            {
                progressBarTask.Style = ProgressBarStyle.Blocks;
                progressBarTask.Value = 0;
            });
        }

        private async Task FlashProgressCompleteAsync()
        {
            this.InvokeIfRequired(() =>
            {
                progressBarTask.Style = ProgressBarStyle.Blocks;
                progressBarTask.Value = 100;
            });
            await Task.Delay(500);
            this.InvokeIfRequired(() => progressBarTask.Value = 0);
        }

        private async void SoftwareControl_Load(object sender, EventArgs e)
        {
            await UpdateConnectStatusAsync();
        }

        private async void SoftwareControl_VisibleChanged(object sender, EventArgs e)
        {
            if (!Visible) return;

            bool connected = await UpdateConnectStatusAsync();
            if (connected && lastRefreshConnectionState != true)
            {
                lastRefreshConnectionState = true;
                await RefreshAppListAsync();
            }
        }

        public async void RefreshConnectionStatus()
        {
            bool becameConnected = await UpdateConnectStatusAsync();

            if (this.Visible && IsDeviceConnected() && becameConnected)
            {
                _ = RefreshAppListAsync();
                lastRefreshConnectionState = true;
            }
        }

        public void ForceRefresh()
        {
            if (this.Visible && IsDeviceConnected())
            {
                _ = RefreshAppListAsync();
            }
        }

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
                    btnUninstall.Enabled = true;
                    btnSelfSign.Enabled = true;
                }
                else
                {
                    lblConnectStatus.Text = "未连接";
                    lblConnectStatus.ForeColor = System.Drawing.Color.Red;
                    btnRefresh.Enabled = false;
                    btnUninstall.Enabled = false;
                    btnSelfSign.Enabled = false;
                    ClearAppList();
                }
            });

            return becameConnected;
        }

        private async Task<bool> IsDeviceConnectedAsync()
        {
            return await Task.Run(() => IsDeviceConnected());
        }

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

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            if (!await IsDeviceConnectedAsync())
            {
                MessageBox.Show("请先连接设备", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            await RefreshAppListAsync();
        }

        private async void BtnSelfSign_Click(object sender, EventArgs e)
        {
            using (SelfSignForm form = new SelfSignForm())
            {
                form.ShowDialog(this);
            }

            if (await IsDeviceConnectedAsync())
                await RefreshAppListAsync();
        }

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

            SetProgressBusy();

            try
            {
                string result = await Task.Run(() => HdcHelper.RunHdcCommand($"uninstall {packageName}"));
                if (result.Contains("success") || result.Contains("Success"))
                {
                    MessageBox.Show("卸载成功", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await FlashProgressCompleteAsync();
                    await RefreshAppListAsync();
                }
                else
                {
                    MessageBox.Show($"卸载失败：{result}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SetProgressIdle();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"卸载出错：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetProgressIdle();
            }
            finally
            {
                this.Cursor = Cursors.Default;
                btnUninstall.Enabled = true;
                btnRefresh.Enabled = true;
            }
        }

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

            SetProgressBusy();

            this.Cursor = Cursors.WaitCursor;
            btnRefresh.Enabled = false;
            btnUninstall.Enabled = false;

            try
            {
                string output = await Task.Run(() => HdcHelper.RunHdcCommand("shell bm dump -a"));
                var apps = await ParseAppListAsync(output);
                DisplayApps(apps);

                await FlashProgressCompleteAsync();

                if (await IsDeviceConnectedAsync())
                    lastRefreshConnectionState = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取应用列表失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetProgressIdle();
            }
            finally
            {
                this.Cursor = Cursors.Default;
                btnRefresh.Enabled = true;
                btnUninstall.Enabled = true;
                isRefreshing = false;
            }
        }

        private async Task<List<AppInfo>> ParseAppListAsync(string output)
        {
            var apps = new List<AppInfo>();
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // bm dump -a 的输出是「ID: N」分段，每段后面跟一个包名
            bool foundFirst = false;
            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (!foundFirst && trimmed.StartsWith("ID:"))
                {
                    foundFirst = true;
                    continue;
                }
                if (foundFirst && trimmed.Length > 0 && !trimmed.StartsWith("ID:"))
                    apps.Add(new AppInfo { PackageName = trimmed, AppName = trimmed });
            }

            // 如果仍未获取到任何应用，尝试使用 pm list packages -3 获取第三方应用列表
            if (apps.Count == 0)
            {
                // 这里必须放到后台线程：本方法是 await 之后的续体，默认跑在 UI 线程上，
                // 直接调用会阻塞界面（原来的写法就是这样）。
                string packageOutput = await Task.Run(() => HdcHelper.RunHdcCommand("shell pm list packages -3"));
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

            foreach (var app in apps)
                app.AppName = AppNameResolver.Resolve(app.PackageName);

            return apps;
        }

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

        public void ClearAppList()
        {
            this.InvokeIfRequired(() => listViewApps.Items.Clear());
        }

        private class AppInfo
        {
            public string AppName { get; set; } = "";
            public string PackageName { get; set; } = "";
        }
    }

    public static class ControlExtensions
    {
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