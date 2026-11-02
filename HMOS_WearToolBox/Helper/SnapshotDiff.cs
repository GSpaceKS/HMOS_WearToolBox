#nullable disable
using System;
using System.Collections.Generic;
using HMOS_WearToolBox.Models;

namespace HMOS_WearToolBox.Helper
{
    public enum ChangeKind
    {
        Unchanged,
        Added,
        Removed,
        Modified
    }

    public class FieldChange
    {
        public string Label { get; set; } = "";
        public string OldValue { get; set; } = "";
        public string NewValue { get; set; } = "";
        public bool Changed { get; set; }
    }

    public class AppDiff
    {
        public string BundleName { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public ChangeKind Kind { get; set; }
        public AppEntry Old { get; set; }
        public AppEntry New { get; set; }
        public List<FieldChange> Fields { get; set; } = new List<FieldChange>();
        public List<string> PermissionAdded { get; set; } = new List<string>();
        public List<string> PermissionRemoved { get; set; } = new List<string>();
        public List<string> PermissionStateChanged { get; set; } = new List<string>();
        public bool SignatureChanged { get; set; }
        public bool VersionChanged { get; set; }
        public bool OnlyPermissionStateChanged { get; set; }
        public int ChangeScore { get; set; }

        public AppEntry Current { get { return New ?? Old; } }

        public string KindText
        {
            get
            {
                switch (Kind)
                {
                    case ChangeKind.Added: return "新装";
                    case ChangeKind.Removed: return "卸载";
                    case ChangeKind.Modified: return "变更";
                    default: return "无变化";
                }
            }
        }

        /// <summary>列表里显示的摘要，例如「版本 2.1.2 → 2.1.3 · 权限 +2 -1」。</summary>
        public string ChangeSummary
        {
            get
            {
                if (Kind == ChangeKind.Added) return "本次快照中新出现";
                if (Kind == ChangeKind.Removed) return "本次快照中已消失";

                var parts = new List<string>();
                if (VersionChanged)
                {
                    string oldVersion = Old != null ? Old.VersionText : "—";
                    string newVersion = New != null ? New.VersionText : "—";
                    parts.Add("版本 " + oldVersion + " → " + newVersion);
                }
                if (SignatureChanged) parts.Add("签名变更");

                foreach (FieldChange field in Fields)
                {
                    if (!field.Changed) continue;
                    if (field.Label == "版本" || field.Label == "签名指纹") continue;
                    parts.Add(field.Label + " " + field.OldValue + " → " + field.NewValue);
                }

                if (PermissionAdded.Count > 0) parts.Add("权限 +" + PermissionAdded.Count);
                if (PermissionRemoved.Count > 0) parts.Add("权限 -" + PermissionRemoved.Count);
                if (PermissionStateChanged.Count > 0) parts.Add("授权状态 " + PermissionStateChanged.Count + " 项");

                if (parts.Count == 0) return "无变化";
                return string.Join(" · ", parts);
            }
        }
    }

    public static class SnapshotDiff
    {
        public static List<AppDiff> Compare(AppSnapshot baseline, AppSnapshot current)
        {
            var result = new List<AppDiff>();
            var currentMap = new Dictionary<string, AppEntry>(StringComparer.OrdinalIgnoreCase);
            var baselineMap = new Dictionary<string, AppEntry>(StringComparer.OrdinalIgnoreCase);

            if (current != null)
                foreach (AppEntry entry in current.Apps) currentMap[entry.BundleName] = entry;
            if (baseline != null)
                foreach (AppEntry entry in baseline.Apps) baselineMap[entry.BundleName] = entry;

            var allBundles = new List<string>();
            foreach (string bundle in baselineMap.Keys) allBundles.Add(bundle);
            foreach (string bundle in currentMap.Keys)
                if (!baselineMap.ContainsKey(bundle)) allBundles.Add(bundle);

            foreach (string bundle in allBundles)
            {
                AppEntry oldEntry;
                AppEntry newEntry;
                baselineMap.TryGetValue(bundle, out oldEntry);
                currentMap.TryGetValue(bundle, out newEntry);

                var diff = new AppDiff
                {
                    BundleName = bundle,
                    Old = oldEntry,
                    New = newEntry,
                    DisplayName = (newEntry ?? oldEntry).DisplayName
                };

                if (oldEntry == null)
                {
                    diff.Kind = ChangeKind.Added;
                    diff.ChangeScore = 100;
                    diff.Fields = BuildFields(null, newEntry);
                }
                else if (newEntry == null)
                {
                    diff.Kind = ChangeKind.Removed;
                    diff.ChangeScore = 100;
                    diff.Fields = BuildFields(oldEntry, null);
                }
                else
                {
                    diff.Fields = BuildFields(oldEntry, newEntry);
                    diff.VersionChanged = oldEntry.VersionCode != newEntry.VersionCode
                        || !string.Equals(oldEntry.VersionName, newEntry.VersionName, StringComparison.Ordinal);
                    diff.SignatureChanged = !string.IsNullOrEmpty(oldEntry.Fingerprint)
                        && !string.IsNullOrEmpty(newEntry.Fingerprint)
                        && !string.Equals(oldEntry.Fingerprint, newEntry.Fingerprint, StringComparison.OrdinalIgnoreCase);

                    ComparePermissions(oldEntry, newEntry, diff);

                    int changed = 0;
                    foreach (FieldChange field in diff.Fields)
                        if (field.Changed) changed++;

                    bool permissionChanged = diff.PermissionAdded.Count > 0 || diff.PermissionRemoved.Count > 0;
                    bool structureChanged = changed > 0 || diff.SignatureChanged;
                    diff.Kind = (structureChanged || permissionChanged || diff.PermissionStateChanged.Count > 0)
                        ? ChangeKind.Modified
                        : ChangeKind.Unchanged;

                    diff.OnlyPermissionStateChanged = diff.Kind == ChangeKind.Modified
                        && !structureChanged
                        && !permissionChanged
                        && diff.PermissionStateChanged.Count > 0;

                    diff.ChangeScore = changed
                        + diff.PermissionAdded.Count + diff.PermissionRemoved.Count
                        + (diff.SignatureChanged ? 10 : 0)
                        + (diff.VersionChanged ? 5 : 0);
                }

                result.Add(diff);
            }

            // 有变化的排前面，无变化的沉底；同类之间按影响程度和名称排。
            result.Sort((a, b) =>
            {
                int byKind = Rank(a.Kind).CompareTo(Rank(b.Kind));
                if (byKind != 0) return byKind;
                int byScore = b.ChangeScore.CompareTo(a.ChangeScore);
                if (byScore != 0) return byScore;
                return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
            });
            return result;
        }

        private static int Rank(ChangeKind kind)
        {
            switch (kind)
            {
                case ChangeKind.Added: return 0;
                case ChangeKind.Removed: return 1;
                case ChangeKind.Modified: return 2;
                default: return 3;
            }
        }

        private static List<FieldChange> BuildFields(AppEntry oldEntry, AppEntry newEntry)
        {
            var fields = new List<FieldChange>();

            Add(fields, "版本",
                oldEntry != null ? oldEntry.VersionText : "—",
                newEntry != null ? newEntry.VersionText : "—");

            Add(fields, "Target API",
                oldEntry != null ? oldEntry.ApiTarget.ToString() : "—",
                newEntry != null ? newEntry.ApiTarget.ToString() : "—");

            Add(fields, "Compile SDK",
                oldEntry != null ? Dash(oldEntry.CompileSdk) : "—",
                newEntry != null ? Dash(newEntry.CompileSdk) : "—");

            Add(fields, "签名指纹",
                oldEntry != null ? Dash(oldEntry.Fingerprint) : "—",
                newEntry != null ? Dash(newEntry.Fingerprint) : "—");

            Add(fields, "应用类型",
                oldEntry != null ? oldEntry.TypeText : "—",
                newEntry != null ? newEntry.TypeText : "—");

            Add(fields, "分发类型",
                oldEntry != null ? Dash(oldEntry.Distribution) : "—",
                newEntry != null ? Dash(newEntry.Distribution) : "—");

            Add(fields, "签名类型",
                oldEntry != null ? Dash(oldEntry.ProvisionType) : "—",
                newEntry != null ? Dash(newEntry.ProvisionType) : "—");

            Add(fields, "CPU ABI",
                oldEntry != null ? Dash(oldEntry.CpuAbi) : "—",
                newEntry != null ? Dash(newEntry.CpuAbi) : "—");

            Add(fields, "UID",
                oldEntry != null ? oldEntry.Uid.ToString() : "—",
                newEntry != null ? newEntry.Uid.ToString() : "—");

            Add(fields, "模块数",
                oldEntry != null ? oldEntry.ModuleCount.ToString() : "—",
                newEntry != null ? newEntry.ModuleCount.ToString() : "—");

            Add(fields, "能力(Ability)",
                oldEntry != null ? oldEntry.Abilities.Count.ToString() : "—",
                newEntry != null ? newEntry.Abilities.Count.ToString() : "—");

            Add(fields, "依赖 HSP",
                oldEntry != null ? JoinOrDash(oldEntry.Dependencies) : "—",
                newEntry != null ? JoinOrDash(newEntry.Dependencies) : "—");

            Add(fields, "支持的设备类型",
                oldEntry != null ? JoinOrDash(oldEntry.DeviceTypes) : "—",
                newEntry != null ? JoinOrDash(newEntry.DeviceTypes) : "—");

            Add(fields, "组织",
                oldEntry != null ? Dash(oldEntry.Organization) : "—",
                newEntry != null ? Dash(newEntry.Organization) : "—");

            Add(fields, "厂商",
                oldEntry != null ? Dash(oldEntry.Vendor) : "—",
                newEntry != null ? Dash(newEntry.Vendor) : "—");

            Add(fields, "更新时间",
                oldEntry != null ? oldEntry.UpdateText : "—",
                newEntry != null ? newEntry.UpdateText : "—");

            Add(fields, "安装时间",
                oldEntry != null ? AppEntry.FormatTime(oldEntry.InstallTime) : "—",
                newEntry != null ? AppEntry.FormatTime(newEntry.InstallTime) : "—");

            Add(fields, "代码路径",
                oldEntry != null ? Dash(oldEntry.CodePath) : "—",
                newEntry != null ? Dash(newEntry.CodePath) : "—");

            Add(fields, "权限数",
                oldEntry != null ? oldEntry.Permissions.Count.ToString() : "—",
                newEntry != null ? newEntry.Permissions.Count.ToString() : "—");

            return fields;
        }

        private static void Add(List<FieldChange> fields, string label, string oldValue, string newValue)
        {
            bool changed = !string.Equals(oldValue, newValue, StringComparison.Ordinal);

            // 新装/卸载时另一侧是「—」，此时再拿 0 或空串来比没有信息量
            // （UID 0、模块数 0、能力 0 之类），算作未变更，否则详情页全是「— → 0」。
            if (changed && IsVacant(oldValue) && IsVacant(newValue))
                changed = false;

            fields.Add(new FieldChange
            {
                Label = label,
                OldValue = oldValue,
                NewValue = newValue,
                Changed = changed
            });
        }

        private static bool IsVacant(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            string trimmed = value.Trim();
            return trimmed == "—" || trimmed == "0";
        }

        private static string Dash(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "—" : value;
        }

        private static string JoinOrDash(List<string> values)
        {
            if (values == null || values.Count == 0) return "—";
            return string.Join(", ", values.ToArray());
        }

        /// <summary>
        /// 权限差异分两类：申请列表本身变了（+/-），和用户授权状态变了（0/1）。
        /// 后者是用户自己在设置里点出来的，不算应用更新，界面上要单独分组。
        /// </summary>
        private static void ComparePermissions(AppEntry oldEntry, AppEntry newEntry, AppDiff diff)
        {
            var oldSet = new HashSet<string>(oldEntry.Permissions, StringComparer.OrdinalIgnoreCase);
            var newSet = new HashSet<string>(newEntry.Permissions, StringComparer.OrdinalIgnoreCase);

            foreach (string permission in newEntry.Permissions)
                if (!oldSet.Contains(permission)) diff.PermissionAdded.Add(permission);

            foreach (string permission in oldEntry.Permissions)
                if (!newSet.Contains(permission)) diff.PermissionRemoved.Add(permission);

            var oldStates = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < oldEntry.Permissions.Count && i < oldEntry.PermissionStates.Count; i++)
                oldStates[oldEntry.Permissions[i]] = oldEntry.PermissionStates[i];

            for (int i = 0; i < newEntry.Permissions.Count && i < newEntry.PermissionStates.Count; i++)
            {
                string permission = newEntry.Permissions[i];
                int oldState;
                if (!oldStates.TryGetValue(permission, out oldState)) continue;
                if (oldState == newEntry.PermissionStates[i]) continue;

                string oldText = oldState == 0 ? "未授予" : "已授予";
                string newText = newEntry.PermissionStates[i] == 0 ? "未授予" : "已授予";
                if (oldText == newText) continue;
                diff.PermissionStateChanged.Add(permission + "  " + oldText + " → " + newText);
            }
        }
    }
}
