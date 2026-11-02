#nullable disable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using HMOS_WearToolBox.Models;

namespace HMOS_WearToolBox.Helper
{
    /// <summary>
    /// 快照的落盘管理。目录可配置，配置存在 %APPDATA%\HMOS_WearToolBox\settings.json。
    /// </summary>
    public static class SnapshotStore
    {
        private static readonly JsonSerializerOptions WriteOptions = new JsonSerializerOptions { WriteIndented = true };
        private static readonly object Sync = new object();
        private static string folderOverride;
        private static bool loaded;

        /// <summary>配置目录：%APPDATA%\HMOS_WearToolBox（不带公司名和版本号，避免出现 HMOS_WearToolBox\HMOS_WearToolBox\1.0.0-beta2 这种重复）。</summary>
        public static string BaseFolder
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "HMOS_WearToolBox");
            }
        }

        public static string SettingsPath
        {
            get { return Path.Combine(BaseFolder, "settings.json"); }
        }

        public static string DefaultFolder
        {
            get { return Path.Combine(BaseFolder, "snapshots"); }
        }

        /// <summary>真正生效的快照目录：用户改过就用用户的，否则用默认。</summary>
        public static string Folder
        {
            get
            {
                lock (Sync)
                {
                    EnsureLoaded();
                    if (!string.IsNullOrEmpty(folderOverride) && folderOverride.Trim().Length > 0)
                        return folderOverride;
                    return DefaultFolder;
                }
            }
        }

        /// <summary>旧版本用 Application.UserAppDataPath（%APPDATA%\公司\产品\版本），目录名重复，需要迁移。</summary>
        private static string LegacyFolder
        {
            get
            {
                try { return Path.Combine(Application.UserAppDataPath, "snapshots"); }
                catch { return null; }
            }
        }

        private sealed class StoreSettings
        {
            public string SnapshotFolder { get; set; }
        }

        private static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var settings = JsonSerializer.Deserialize<StoreSettings>(File.ReadAllText(SettingsPath));
                    if (settings != null) folderOverride = settings.SnapshotFolder;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("读取快照设置失败: " + ex.Message);
            }

            // 只有用户没指定过目录时，才把旧目录里的快照搬到新的默认目录
            if (string.IsNullOrEmpty(folderOverride)) MigrateLegacyFolder();
        }

        private static void MigrateLegacyFolder()
        {
            try
            {
                string legacy = LegacyFolder;
                if (string.IsNullOrEmpty(legacy)) return;
                if (string.Equals(Path.GetFullPath(legacy).TrimEnd('\\'), Path.GetFullPath(DefaultFolder).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)) return;
                if (!Directory.Exists(legacy)) return;

                string[] files = Directory.GetFiles(legacy, "*.json");
                foreach (string file in files)
                {
                    string target = Path.Combine(DefaultFolder, Path.GetFileName(file));
                    if (File.Exists(target)) continue;
                    Directory.CreateDirectory(DefaultFolder);
                    File.Move(file, target);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("迁移旧快照目录失败: " + ex.Message);
            }
        }

        public static void SetFolder(string path)
        {
            lock (Sync)
            {
                EnsureLoaded();
                string normalized = string.IsNullOrEmpty(path) ? null : path.Trim();
                if (normalized != null && normalized.Length == 0) normalized = null;

                folderOverride = normalized;
                try
                {
                    Directory.CreateDirectory(BaseFolder);
                    var settings = new StoreSettings { SnapshotFolder = normalized };
                    File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, WriteOptions));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("保存快照设置失败: " + ex.Message);
                }
            }
        }

        public static string PathFor(string id)
        {
            return Path.Combine(Folder, id + ".json");
        }

        public static void Save(AppSnapshot snapshot)
        {
            string folder = Folder;
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, snapshot.Id + ".json");
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(snapshot, WriteOptions));
            if (File.Exists(path)) File.Delete(path);
            File.Move(temp, path);
        }

        public static List<AppSnapshot> LoadAll()
        {
            var list = new List<AppSnapshot>();
            string folder = Folder;
            if (!Directory.Exists(folder)) return list;

            foreach (string file in Directory.GetFiles(folder, "*.json"))
            {
                try
                {
                    var snapshot = JsonSerializer.Deserialize<AppSnapshot>(File.ReadAllText(file));
                    if (snapshot == null) continue;
                    if (string.IsNullOrEmpty(snapshot.Id)) snapshot.Id = Path.GetFileNameWithoutExtension(file);
                    list.Add(snapshot);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("读取快照失败 " + file + ": " + ex.Message);
                }
            }

            list.Sort((a, b) => b.CreatedAt.CompareTo(a.CreatedAt));
            return list;
        }

        public static void Delete(string id)
        {
            try
            {
                string path = PathFor(id);
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("删除快照失败 " + id + ": " + ex.Message);
            }
        }

        public static long SizeOf(string id)
        {
            try
            {
                var info = new FileInfo(PathFor(id));
                return info.Exists ? info.Length : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>把 fromFolder 里的快照复制到 toFolder（已存在同名文件就跳过），返回复制了几份。</summary>
        public static int CopySnapshots(string fromFolder, string toFolder)
        {
            int copied = 0;
            try
            {
                if (!Directory.Exists(fromFolder)) return 0;
                Directory.CreateDirectory(toFolder);
                foreach (string file in Directory.GetFiles(fromFolder, "*.json"))
                {
                    string target = Path.Combine(toFolder, Path.GetFileName(file));
                    if (File.Exists(target)) continue;
                    File.Copy(file, target, false);
                    copied++;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("复制快照失败: " + ex.Message);
            }
            return copied;
        }
    }
}
