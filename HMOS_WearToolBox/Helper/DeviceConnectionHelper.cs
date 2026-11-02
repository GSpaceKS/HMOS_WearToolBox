using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using HMOS_WearToolBox.Manager;

namespace HMOS_WearToolBox.Helper
{
    /// <summary>
    /// 设备在线状态检测。检测结果带短时缓存，
    /// 避免每次按键、每次切换页面都去启动一个 hdc 进程。
    /// </summary>
    public static class DeviceConnectionHelper
    {
        private const int CacheMs = 500;

        private static readonly object _lock = new object();
        private static DateTime _lastCheckTime = DateTime.MinValue;
        private static bool _cachedOnline;

        /// <summary>
        /// 最近一次检测到的在线状态，不会触发新的检测。
        /// 适合放在按键、鼠标事件这种高频路径上。
        /// </summary>
        public static bool LastKnownOnline
        {
            get { lock (_lock) return _cachedOnline; }
        }

        public static void InvalidateCache()
        {
            lock (_lock) _lastCheckTime = DateTime.MinValue;
        }

        /// <summary>
        /// 检测所有已保存设备的在线状态，回写 <see cref="DeviceInfo.IsConnected"/>，并返回是否至少有一台在线。
        /// </summary>
        public static bool CheckAndUpdateConnectionStatus()
        {
            lock (_lock)
            {
                if ((DateTime.UtcNow - _lastCheckTime).TotalMilliseconds < CacheMs)
                    return _cachedOnline;
            }

            bool anyOnline = false;

            try
            {
                List<DeviceInfo> devices = DeviceManager.GetDevices();
                if (devices.Count > 0)
                {
                    // 本地探测，给一个短超时即可，不要用 60 秒的默认值把界面拖住
                    List<string> targets = HdcHelper.ListTargets(HdcHelper.QuickTimeoutMs);

                    foreach (DeviceInfo device in devices)
                    {
                        bool isOnline = HdcHelper.IsTargetOnline(targets, device.IpAddress);
                        anyOnline = anyOnline || isOnline;

                        if (device.IsConnected != isOnline)
                        {
                            device.IsConnected = isOnline;
                            if (isOnline)
                                device.LastConnected = DateTime.Now;

                            DeviceManager.UpdateDevice(device);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // hdc 缺失或超时不应该让调用方崩掉，按「未连接」处理
                Debug.WriteLine($"连接状态检测失败: {ex.Message}");
                anyOnline = false;
            }

            lock (_lock)
            {
                _cachedOnline = anyOnline;
                _lastCheckTime = DateTime.UtcNow;
            }

            return anyOnline;
        }

        /// <summary>
        /// 异步版本，避免在 UI 线程上等待 hdc。
        /// </summary>
        public static async Task<bool> CheckAndUpdateConnectionStatusAsync()
        {
            return await Task.Run(CheckAndUpdateConnectionStatus);
        }
    }
}
