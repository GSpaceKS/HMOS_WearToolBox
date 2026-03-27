using System;

namespace HMOS_WearToolBox.Manager
{
    public class DeviceInfo
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "未知设备";
        public string Model { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public DateTime LastConnected { get; set; }
        public bool IsConnected { get; set; }
        public bool IsNew { get; set; } = true;
    }
}