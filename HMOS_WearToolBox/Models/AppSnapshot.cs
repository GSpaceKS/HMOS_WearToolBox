#nullable disable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace HMOS_WearToolBox.Models
{
    /// <summary>
    /// 单个应用的快照条目。只保存比对需要的字段，故意丢掉 reqPermissionDetails 等大字段，
    /// 让整机快照体积控制在 100KB 以内（原始 bm dump 全量约 3.9MB）。
    /// </summary>
    public class AppEntry
    {
        public string BundleName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string VersionName { get; set; } = "";
        public long VersionCode { get; set; }
        public int ApiTarget { get; set; }
        public string CompileSdk { get; set; } = "";
        public string Fingerprint { get; set; } = "";
        public int Uid { get; set; }
        public bool IsSystem { get; set; }
        public string Distribution { get; set; } = "";
        public string ProvisionType { get; set; } = "";
        public string CpuAbi { get; set; } = "";
        public string Organization { get; set; } = "";
        public string Vendor { get; set; } = "";
        public long InstallTime { get; set; }
        public long UpdateTime { get; set; }
        public long FirstInstallTime { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
        public List<int> PermissionStates { get; set; } = new List<int>();
        public List<string> Abilities { get; set; } = new List<string>();
        public List<string> Dependencies { get; set; } = new List<string>();
        public List<string> DeviceTypes { get; set; } = new List<string>();
        public int ModuleCount { get; set; }
        public string CodePath { get; set; } = "";

        [JsonIgnore]
        public string TypeText { get { return IsSystem ? "系统" : "第三方"; } }

        [JsonIgnore]
        public string VersionText
        {
            get { return VersionCode > 0 ? VersionName + " (" + VersionCode + ")" : VersionName; }
        }

        [JsonIgnore]
        public string UpdateText { get { return FormatTime(UpdateTime); } }

        [JsonIgnore]
        public string ShortFingerprint { get { return Shorten(Fingerprint); } }

        public static string FormatTime(long ms)
        {
            if (ms <= 0) return "—";
            try { return DateTimeOffset.FromUnixTimeMilliseconds(ms).ToLocalTime().ToString("yyyy-MM-dd HH:mm"); }
            catch { return ms.ToString(); }
        }

        public static string Shorten(string s)
        {
            if (string.IsNullOrEmpty(s)) return "—";
            return s.Length > 16 ? s.Substring(0, 16) + "…" : s;
        }
    }

    public class AppSnapshot
    {
        public int SchemaVersion { get; set; } = 1;
        public string Id { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string DeviceName { get; set; } = "";
        public string Serial { get; set; } = "";
        public string OsVersion { get; set; } = "";
        public string SoftwareVersion { get; set; } = "";
        public int ApiVersion { get; set; }
        public List<AppEntry> Apps { get; set; } = new List<AppEntry>();

        [JsonIgnore]
        public int SystemCount
        {
            get
            {
                int n = 0;
                foreach (var a in Apps) if (a.IsSystem) n++;
                return n;
            }
        }

        [JsonIgnore]
        public int ThirdCount { get { return Apps.Count - SystemCount; } }

        [JsonIgnore]
        public string TimeText { get { return CreatedAt.ToString("yyyy-MM-dd HH:mm"); } }
    }
}
