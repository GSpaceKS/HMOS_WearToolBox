using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace HMOS_WearToolBox.Manager
{
    /// <summary>
    /// 设备列表的持久化管理（devices.json）。
    /// 所有对外接口都加了锁：刷新定时器、连接检测、界面事件会从不同线程访问它。
    /// </summary>
    public static class DeviceManager
    {
        private static readonly object _sync = new object();
        /// <summary>配置目录固定在 %APPDATA%\HMOS_WearToolBox，不随版本号变化。</summary>
        private static readonly string dataFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HMOS_WearToolBox");

        private static readonly string dataPath = Path.Combine(dataFolder, "devices.json");
        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions { WriteIndented = true };
        private static List<DeviceInfo> devices = new List<DeviceInfo>();

        static DeviceManager()
        {
            lock (_sync)
            {
                LoadInternal();
            }
        }

        public static void Load()
        {
            lock (_sync)
            {
                LoadInternal();
            }
        }

        /// <summary>
        /// 老版本把 devices.json 放在 %APPDATA%\HMOS_WearToolBox\<版本号>\ 下，升级后路径就变了。
        /// 这里把所有能读到的旧文件合并（按地址去重，同地址保留 LastConnected 更新的那条），
        /// 避免升级丢设备列表，也避免挑错某一份只剩一台设备的旧文件。
        /// </summary>
        private static void MigrateLegacyFile()
        {
            try
            {
                // 旧文件在 %APPDATA%\HMOS_WearToolBox\<版本号>\devices.json，比 dataFolder 低一层，
                // 所以必须递归找（GetDirectories 只列直接子目录，会漏掉它们）。
                List<string> candidates = Directory.GetFiles(dataFolder, "devices.json", SearchOption.AllDirectories)
                    .Where(path => !string.Equals(path, dataPath, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (candidates.Count == 0)
                    return;

                var merged = new List<DeviceInfo>();
                foreach (string candidate in candidates)
                {
                    List<DeviceInfo>? list;
                    try
                    {
                        list = JsonSerializer.Deserialize<List<DeviceInfo>>(File.ReadAllText(candidate));
                    }
                    catch
                    {
                        continue;
                    }

                    if (list == null)
                        continue;

                    foreach (DeviceInfo device in list)
                    {
                        int index = merged.FindIndex(d => string.Equals(d.IpAddress, device.IpAddress, StringComparison.OrdinalIgnoreCase));
                        if (index < 0)
                            merged.Add(device);
                        else if (device.LastConnected > merged[index].LastConnected)
                            merged[index] = device;
                    }
                }

                if (merged.Count == 0)
                    return;

                // 调用方已持有 _sync，直接改字段再落盘（LoadInternal 之后就照着它走）
                devices = merged;
                SaveInternal();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"迁移设备列表失败: {ex.Message}");
            }
        }

        private static void LoadInternal()
        {
            try
            {
                Directory.CreateDirectory(dataFolder);

                if (!File.Exists(dataPath))
                {
                    MigrateLegacyFile();
                    if (!File.Exists(dataPath))
                        return;
                }

                string json = File.ReadAllText(dataPath);
                devices = JsonSerializer.Deserialize<List<DeviceInfo>>(json) ?? new List<DeviceInfo>();

                // 清除所有设备的 New 标记（重新启动后不再显示 NEW）
                if (devices.Any(d => d.IsNew))
                {
                    foreach (DeviceInfo d in devices)
                        d.IsNew = false;

                    SaveInternal();
                }
            }
            catch (Exception ex)
            {
                // 配置文件损坏时不要让程序启动失败，退回空列表
                Debug.WriteLine($"读取设备列表失败: {ex.Message}");
                devices = new List<DeviceInfo>();
            }
        }

        public static void Save()
        {
            lock (_sync)
            {
                SaveInternal();
            }
        }

        /// <summary>写入磁盘。调用方必须已经持有 <see cref="_sync"/>。</summary>
        private static void SaveInternal()
        {
            try
            {
                // 先写临时文件再替换：直接覆盖写，若写到一半进程被结束，会留下损坏的 json
                string json = JsonSerializer.Serialize(devices, jsonOptions);
                string tempPath = dataPath + ".tmp";
                File.WriteAllText(tempPath, json);
                File.Move(tempPath, dataPath, overwrite: true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"保存设备列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 返回设备列表的副本。元素本身仍是同一批对象引用，
        /// 因此调用方修改某个设备的属性后再调 <see cref="UpdateDevice"/> 依然生效；
        /// 但没法通过返回值直接改到内部集合，增删必须走 Add / Remove / Update。
        /// </summary>
        public static List<DeviceInfo> GetDevices()
        {
            lock (_sync)
            {
                return new List<DeviceInfo>(devices);
            }
        }

        public static void AddDevice(DeviceInfo device)
        {
            lock (_sync)
            {
                devices.Add(device);
                SaveInternal();
            }
        }

        public static void RemoveDevice(string id)
        {
            lock (_sync)
            {
                devices.RemoveAll(d => d.Id == id);
                SaveInternal();
            }
        }

        public static void UpdateDevice(DeviceInfo device)
        {
            lock (_sync)
            {
                int index = devices.FindIndex(d => d.Id == device.Id);
                if (index >= 0)
                {
                    devices[index] = device;
                    SaveInternal();
                }
            }
        }
    }
}
