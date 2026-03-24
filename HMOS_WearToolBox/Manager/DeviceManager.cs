using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace HMOS_WearToolBox.Manager
{
    public static class DeviceManager
    {
        private static List<DeviceInfo> devices = new List<DeviceInfo>();
        private static string dataPath = Path.Combine(Application.UserAppDataPath, "devices.json");

        static DeviceManager()
        {
            Load();
        }

        public static void Load()
        {
            if (File.Exists(dataPath))
            {
                string json = File.ReadAllText(dataPath);
                devices = JsonSerializer.Deserialize<List<DeviceInfo>>(json) ?? new List<DeviceInfo>();
            }
        }

        public static void Save()
        {
            string json = JsonSerializer.Serialize(devices, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(dataPath, json);
        }

        public static List<DeviceInfo> GetDevices() => devices;

        public static void AddDevice(DeviceInfo device)
        {
            devices.Add(device);
            Save();
        }

        public static void RemoveDevice(string id)
        {
            devices.RemoveAll(d => d.Id == id);
            Save();
        }

        public static void UpdateDevice(DeviceInfo device)
        {
            int index = devices.FindIndex(d => d.Id == device.Id);
            if (index >= 0)
            {
                devices[index] = device;
                Save();
            }
        }
    }
}