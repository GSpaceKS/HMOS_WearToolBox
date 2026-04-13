using System.Linq;
using System.Threading.Tasks;
using HMOS_WearToolBox.Helper;
using HMOS_WearToolBox.Manager;

namespace HMOS_WearToolBox.Helper
{
    public static class DeviceConnectionHelper
    {
        private static DateTime _lastCheckTime = DateTime.MinValue;
        private static bool _cachedOnline = false;
        private static readonly object _lock = new object();

        public static bool CheckAndUpdateConnectionStatus()
        {
            lock (_lock)
            {
                // 缓存有效期 500ms
                if ((DateTime.Now - _lastCheckTime).TotalMilliseconds < 500)
                    return _cachedOnline;
            }

            var devices = DeviceManager.GetDevices();
            if (devices.Count == 0) return false;

            string targets = HdcHelper.RunHdcCommand("list targets");
            bool anyOnline = false;
            foreach (var device in devices)
            {
                bool isOnline = targets.Contains(device.IpAddress.Split(':')[0]);
                anyOnline = anyOnline || isOnline;
                if (device.IsConnected != isOnline)
                {
                    device.IsConnected = isOnline;
                    DeviceManager.UpdateDevice(device);
                }
            }

            lock (_lock)
            {
                _cachedOnline = anyOnline;
                _lastCheckTime = DateTime.Now;
            }
            return anyOnline;
        }

        public static async Task<bool> CheckAndUpdateConnectionStatusAsync()
        {
            return await Task.Run(CheckAndUpdateConnectionStatus);
        }
    }
}