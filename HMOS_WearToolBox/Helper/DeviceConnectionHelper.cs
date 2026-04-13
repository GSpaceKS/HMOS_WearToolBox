using System.Linq;
using System.Threading.Tasks;
using HMOS_WearToolBox.Helper;
using HMOS_WearToolBox.Manager;

namespace HMOS_WearToolBox.Helper
{
    public static class DeviceConnectionHelper
    {
        public static bool CheckAndUpdateConnectionStatus()
        {
            var devices = DeviceManager.GetDevices();
            if (devices.Count == 0) return false;

            // 复制列表以避免枚举时集合被修改
            var devicesSnapshot = devices.ToList();
            string targets = HdcHelper.RunHdcCommand("list targets");
            bool anyOnline = false;

            foreach (var device in devicesSnapshot)
            {
                string ipPart = device.IpAddress.Split(':')[0];
                bool isOnline = targets.Contains(ipPart);
                anyOnline = anyOnline || isOnline;
                if (device.IsConnected != isOnline)
                {
                    device.IsConnected = isOnline;
                    DeviceManager.UpdateDevice(device);
                }
            }
            return anyOnline;
        }

        public static async Task<bool> CheckAndUpdateConnectionStatusAsync()
        {
            return await Task.Run(CheckAndUpdateConnectionStatus);
        }
    }
}