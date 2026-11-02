#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HMOS_WearToolBox.Helper
{
    public sealed class RenameRecord
    {
        /// <summary>界面显示的名字，来自安装包文件名。</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>改名前的原始包名。</summary>
        public string OriginalBundle { get; set; } = string.Empty;
        /// <summary>当初安装的 HAP 文件名，方便用户对账。</summary>
        public string HapFile { get; set; } = string.Empty;
        public string InstalledAt { get; set; } = string.Empty;
    }

    /// <summary>记录本工具改过名的应用：新包名 → 原名 / 原包名。</summary>
    public static class RenameStore
    {
        private static readonly object Sync = new object();
        private static Dictionary<string, RenameRecord> cache;

        /// <summary>存储根目录。正常运行留空，用 %APPDATA%\HMOS_WearToolBox；自测可指到临时目录。</summary>
        public static string StoreFolder { get; set; }

        private static string Folder
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(StoreFolder)) return StoreFolder;
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "HMOS_WearToolBox");
            }
        }

        public static string FilePath
        {
            get { return Path.Combine(Folder, "renames.json"); }
        }

        public static Dictionary<string, RenameRecord> Load()
        {
            lock (Sync)
            {
                if (cache != null) return cache;

                var map = new Dictionary<string, RenameRecord>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    if (File.Exists(FilePath))
                    {
                        string text = File.ReadAllText(FilePath, Encoding.UTF8);
                        var loaded = JsonSerializer.Deserialize<Dictionary<string, RenameRecord>>(text);
                        if (loaded != null)
                            foreach (var kv in loaded)
                                if (kv.Value != null) map[kv.Key] = kv.Value;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("读取 renames.json 失败: " + ex.Message);
                }

                cache = map;
                return cache;
            }
        }

        public static bool TryGet(string bundleName, out RenameRecord record)
        {
            record = null;
            if (string.IsNullOrWhiteSpace(bundleName)) return false;
            RenameRecord found;
            if (!Load().TryGetValue(bundleName, out found) || found == null) return false;
            record = found;
            return !string.IsNullOrWhiteSpace(found.Name);
        }

        /// <summary>安装成功后登记一条；同一个新包名重复安装就覆盖。</summary>
        public static void Remember(string newBundle, string displayName, string originalBundle, string hapFile)
        {
            if (string.IsNullOrWhiteSpace(newBundle)) return;

            lock (Sync)
            {
                var map = new Dictionary<string, RenameRecord>(Load(), StringComparer.OrdinalIgnoreCase);
                map[newBundle] = new RenameRecord
                {
                    Name = string.IsNullOrWhiteSpace(displayName) ? string.Empty : displayName.Trim(),
                    OriginalBundle = originalBundle ?? string.Empty,
                    HapFile = hapFile ?? string.Empty,
                    InstalledAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                try
                {
                    Directory.CreateDirectory(Folder);
                    string json = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
                    string temp = FilePath + ".tmp";
                    File.WriteAllText(temp, json, new UTF8Encoding(false));
                    if (File.Exists(FilePath)) File.Delete(FilePath);
                    File.Move(temp, FilePath);
                    cache = map;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("写入 renames.json 失败: " + ex.Message);
                }
            }
        }

        /// <summary>从安装包文件名里取出显示名，取不到返回空串。</summary>
        public static string NameFromHapFile(string hapPath)
        {
            if (string.IsNullOrWhiteSpace(hapPath)) return string.Empty;

            string name = Path.GetFileNameWithoutExtension(hapPath);
            if (string.IsNullOrWhiteSpace(name)) return string.Empty;

            name = Regex.Replace(name, @"[-_\s]v?\d+(\.\d+)+.*$", string.Empty);
            name = name.Trim().TrimEnd('-', '_', ' ');
            return name;
        }
    }
}
