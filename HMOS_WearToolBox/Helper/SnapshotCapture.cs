#nullable disable
using System;
using System.Collections.Generic;
using System.Text.Json;
using HMOS_WearToolBox.Models;

namespace HMOS_WearToolBox.Helper
{
    /// <summary>从设备采集全量应用信息生成快照。</summary>
    public static class SnapshotCapture
    {
        private const string Marker = "@@@";

        /// <summary>每批读取多少个应用。手表上循环 24 次约 5 秒，分批既能限时也能报进度。</summary>
        private const int BatchSize = 24;

        private static readonly string[] DeviceParams =
        {
            "const.product.model",
            "const.product.name",
            "const.ohos.boot.sn",
            "const.ohos.fullname",
            "const.product.os.dist.name",
            "const.product.os.version",
            "const.product.software.version",
            "const.ohos.apiversion"
        };

        /// <summary>采集整机应用信息（20~60 秒，须在后台线程执行）。</summary>
        public static AppSnapshot Capture(IProgress<string> progress)
        {
            var snap = new AppSnapshot
            {
                Id = DateTime.Now.ToString("yyyyMMdd-HHmmss"),
                CreatedAt = DateTime.Now
            };

            Report(progress, "检查设备连接…");
            SelfSignService.EnsureDevice(text => System.Diagnostics.Debug.WriteLine(text));

            Report(progress, "读取设备信息…");
            Dictionary<string, string> values = QueryAll(DeviceParams);
            string model = Clean(Value(values, "const.product.model"));
            string product = Clean(Value(values, "const.product.name"));
            snap.DeviceName = string.IsNullOrWhiteSpace(product)
                ? model
                : (string.IsNullOrWhiteSpace(model) || string.Equals(product, model, StringComparison.OrdinalIgnoreCase)
                    ? product
                    : product + " (" + model + ")");
            snap.Serial = Clean(Value(values, "const.ohos.boot.sn"));
            snap.OsVersion = Value(values, "const.ohos.fullname");
            if (string.IsNullOrWhiteSpace(snap.OsVersion))
            {
                snap.OsVersion = (Value(values, "const.product.os.dist.name") + " " + Value(values, "const.product.os.version")).Trim();
            }
            snap.SoftwareVersion = Value(values, "const.product.software.version");

            int api;
            if (int.TryParse(Value(values, "const.ohos.apiversion"), out api)) snap.ApiVersion = api;

            Report(progress, "获取应用清单…");
            string listOutput = HdcHelper.RunHdcCommand("shell bm dump -a");
            List<string> bundles = ParseBundleList(listOutput);
            if (bundles.Count == 0)
            {
                throw new InvalidOperationException(
                    "设备没有返回应用清单，请确认手表已经连接、亮屏并解锁。" +
                    (string.IsNullOrWhiteSpace(listOutput) ? "" : "\nhdc 返回：" + Shorten(listOutput)));
            }

            int done = 0;

            for (int start = 0; start < bundles.Count; start += BatchSize)
            {
                List<string> chunk = bundles.GetRange(start, Math.Min(BatchSize, bundles.Count - start));
                Report(progress, "读取应用信息  " + (start + 1) + " - " + (start + chunk.Count) + " / " + bundles.Count);

                string output;
                try
                {
                    output = HdcHelper.RunHdcCommand(BuildDumpCommand(chunk), HdcHelper.LongRunningTimeoutMs);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("批量读取应用信息失败: " + ex.Message);
                    continue;
                }

                Dictionary<string, string> sections = SplitSections(output);
                foreach (string bundle in chunk)
                {
                    string body;
                    if (!sections.TryGetValue(bundle, out body)) continue;

                    try
                    {
                        AppEntry entry = ParseBundle(body, bundle);
                        if (entry == null) continue;

                        entry.DisplayName = AppNameResolver.Resolve(bundle);
                        snap.Apps.Add(entry);
                        done++;
                    }
                    catch (Exception ex)
                    {
                        // 单个应用读不出来不该让整次快照失败
                        System.Diagnostics.Debug.WriteLine("读取 " + bundle + " 失败: " + ex.Message);
                    }
                }
            }

            if (snap.Apps.Count == 0)
            {
                throw new InvalidOperationException(
                    "设备返回了应用清单（" + bundles.Count + " 个），但一个应用信息都没能解析出来。" +
                    "请把 hdc 版本和手表型号反馈给作者。");
            }

            Report(progress, "已读取 " + done + " 个应用");
            snap.Apps.Sort((a, b) => string.Compare(a.BundleName, b.BundleName, StringComparison.OrdinalIgnoreCase));
            return snap;
        }

        private static string Shorten(string text)
        {
            string oneLine = text.Replace("\r", " ").Replace("\n", " ").Trim();
            return oneLine.Length <= 160 ? oneLine : oneLine.Substring(0, 160) + "…";
        }

        private static void Report(IProgress<string> progress, string text)
        {
            if (progress != null) progress.Report(text);
        }

        /// <summary>过滤 param get 读不到时返回的报错文本。</summary>
        private static string Clean(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            if (value.IndexOf("fail!", StringComparison.OrdinalIgnoreCase) >= 0) return "";
            if (value.IndexOf("errNum", StringComparison.OrdinalIgnoreCase) >= 0) return "";
            if (value.StartsWith("Get parameter", StringComparison.OrdinalIgnoreCase)) return "";
            return value.Trim();
        }

        private static string Value(Dictionary<string, string> values, string key)
        {
            string value;
            return values.TryGetValue(key, out value) ? value : "";
        }

        /// <summary>一次 shell 调用读完所有参数：for p in …; do echo @@@$p; param get $p; done</summary>
        private static Dictionary<string, string> QueryAll(string[] names)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            string command = "shell \"for p in " + string.Join(" ", names) + "; do echo " + Marker + "$p; param get $p; done\"";
            try
            {
                foreach (KeyValuePair<string, string> pair in SplitSections(HdcHelper.RunHdcCommand(command, HdcHelper.DefaultTimeoutMs)))
                    map[pair.Key] = pair.Value;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("读取设备参数失败: " + ex.Message);
            }
            return map;
        }

        private static string BuildDumpCommand(List<string> bundles)
        {
            return "shell \"for b in " + string.Join(" ", bundles) + "; do echo " + Marker + "$b; bm dump -n $b; done\"";
        }

        /// <summary>把「@@名字 + 内容」形式的批量输出切成字典。</summary>
        private static Dictionary<string, string> SplitSections(string output)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrEmpty(output)) return map;

            string current = null;
            var buffer = new System.Text.StringBuilder();

            foreach (string raw in output.Replace("\r\n", "\n").Split('\n'))
            {
                string line = raw.TrimEnd();
                if (line.StartsWith(Marker, StringComparison.Ordinal))
                {
                    if (current != null) map[current] = buffer.ToString().Trim();
                    current = line.Substring(Marker.Length).Trim();
                    buffer.Length = 0;
                    continue;
                }

                if (current != null) buffer.Append(line).Append('\n');
            }

            if (current != null) map[current] = buffer.ToString().Trim();
            return map;
        }

        /// <summary>bm dump -a 的输出形如「ID: 1」/「ID: 2」… 后面每行一个包名。</summary>
        private static List<string> ParseBundleList(string output)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(output)) return list;

            foreach (string raw in output.Split(new[] { (char)13, (char)10 }, StringSplitOptions.RemoveEmptyEntries))
            {
                string line = raw.Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("ID:", StringComparison.OrdinalIgnoreCase)) continue;
                if (line.StartsWith("[", StringComparison.Ordinal)) continue;
                if (line.IndexOf(' ') >= 0) continue;
                if (list.Contains(line)) continue;
                list.Add(line);
            }
            return list;
        }

        /// <summary>把单个 bm dump -n 的 JSON 解析成 AppEntry，兼容被包了一层的输出。</summary>
        public static AppEntry ParseBundle(string json, string bundleName)
        {
            json = ExtractJson(json);
            if (json == null) return null;

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                JsonElement root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return null;

                JsonElement wrapped;
                if (root.TryGetProperty(bundleName, out wrapped)
                    && wrapped.ValueKind == JsonValueKind.Object
                    && wrapped.TryGetProperty("applicationInfo", out _))
                {
                    root = wrapped;
                }

                JsonElement app;
                if (!root.TryGetProperty("applicationInfo", out app) || app.ValueKind != JsonValueKind.Object)
                    app = root;

                var entry = new AppEntry();
                entry.BundleName = Str(root, "name");
                if (string.IsNullOrWhiteSpace(entry.BundleName)) entry.BundleName = bundleName;
                entry.VersionName = Str(root, "versionName");
                entry.VersionCode = Num(root, "versionCode");
                entry.InstallTime = Num(root, "installTime");
                entry.UpdateTime = Num(root, "updateTime");
                entry.FirstInstallTime = Num(root, "firstInstallTime");
                if (entry.FirstInstallTime <= 0) entry.FirstInstallTime = entry.InstallTime;
                entry.Uid = (int)Num(root, "uid");

                entry.CodePath = Str(app, "codePath");
                entry.Fingerprint = Str(app, "fingerprint");
                entry.CompileSdk = Str(app, "compileSdkVersion");
                entry.ApiTarget = DecodeApi((int)Num(app, "apiTargetVersion"));
                entry.IsSystem = Bool(app, "isSystemApp");
                entry.Distribution = Str(app, "appDistributionType");
                entry.ProvisionType = Str(app, "appProvisionType");
                entry.CpuAbi = Str(app, "cpuAbi");
                if (string.IsNullOrWhiteSpace(entry.CpuAbi)) entry.CpuAbi = Str(root, "cpuAbi");
                entry.Organization = Str(app, "organization");
                entry.Vendor = Str(app, "vendor");

                entry.Permissions = StrArray(root, "reqPermissions");
                entry.PermissionStates = IntArray(root, "reqPermissionStates");

                var abilities = new List<string>();
                var dependencies = new List<string>();
                var deviceTypes = new List<string>();

                JsonElement modules;
                if (root.TryGetProperty("hapModuleInfos", out modules) && modules.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement module in modules.EnumerateArray())
                    {
                        entry.ModuleCount++;

                        JsonElement abilityInfos;
                        if (module.TryGetProperty("abilityInfos", out abilityInfos) && abilityInfos.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement ability in abilityInfos.EnumerateArray())
                            {
                                string abilityName = Str(ability, "name");
                                if (!string.IsNullOrWhiteSpace(abilityName) && !abilities.Contains(abilityName))
                                    abilities.Add(abilityName);
                            }
                        }

                        JsonElement deps;
                        if (module.TryGetProperty("dependencies", out deps) && deps.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement dep in deps.EnumerateArray())
                            {
                                string depName = dep.ValueKind == JsonValueKind.String ? dep.GetString() : Str(dep, "bundleName");
                                if (!string.IsNullOrWhiteSpace(depName) && !dependencies.Contains(depName))
                                    dependencies.Add(depName);
                            }
                        }

                        JsonElement types;
                        if (module.TryGetProperty("deviceTypes", out types) && types.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement t in types.EnumerateArray())
                            {
                                string typeName = t.ValueKind == JsonValueKind.String ? t.GetString() : "";
                                if (!string.IsNullOrWhiteSpace(typeName) && !deviceTypes.Contains(typeName))
                                    deviceTypes.Add(typeName);
                            }
                        }
                    }
                }

                entry.Abilities = abilities;
                entry.Dependencies = dependencies;
                entry.DeviceTypes = deviceTypes;
                return entry;
            }
        }

        /// <summary>从 bm dump 输出里截出 JSON 部分。</summary>
        private static string ExtractJson(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            int start = text.IndexOf('{');
            int end = text.LastIndexOf('}');
            if (start < 0 || end <= start) return null;
            return text.Substring(start, end - start + 1);
        }

        /// <summary>把按位打包的 apiTargetVersion 还原成 API 号。</summary>
        public static int DecodeApi(int raw)
        {
            if (raw <= 0) return 0;
            return raw % 1000;
        }

        private static string Str(JsonElement parent, string name)
        {
            if (parent.ValueKind != JsonValueKind.Object) return "";
            JsonElement v;
            if (!parent.TryGetProperty(name, out v)) return "";
            if (v.ValueKind == JsonValueKind.String) return v.GetString() ?? "";
            if (v.ValueKind == JsonValueKind.Number) return v.ToString();
            if (v.ValueKind == JsonValueKind.True) return "true";
            if (v.ValueKind == JsonValueKind.False) return "false";
            return "";
        }

        private static long Num(JsonElement parent, string name)
        {
            if (parent.ValueKind != JsonValueKind.Object) return 0;
            JsonElement v;
            if (!parent.TryGetProperty(name, out v)) return 0;
            if (v.ValueKind != JsonValueKind.Number) return 0;
            long l;
            if (v.TryGetInt64(out l)) return l;
            double d;
            if (v.TryGetDouble(out d)) return (long)d;
            return 0;
        }

        private static bool Bool(JsonElement parent, string name)
        {
            if (parent.ValueKind != JsonValueKind.Object) return false;
            JsonElement v;
            if (!parent.TryGetProperty(name, out v)) return false;
            return v.ValueKind == JsonValueKind.True;
        }

        private static List<string> StrArray(JsonElement parent, string name)
        {
            var list = new List<string>();
            JsonElement arr;
            if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(name, out arr)) return list;
            if (arr.ValueKind != JsonValueKind.Array) return list;
            foreach (JsonElement item in arr.EnumerateArray())
                if (item.ValueKind == JsonValueKind.String) list.Add(item.GetString());
            return list;
        }

        private static List<int> IntArray(JsonElement parent, string name)
        {
            var list = new List<int>();
            JsonElement arr;
            if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(name, out arr)) return list;
            if (arr.ValueKind != JsonValueKind.Array) return list;
            foreach (JsonElement item in arr.EnumerateArray())
                if (item.ValueKind == JsonValueKind.Number) list.Add(item.GetInt32());
            return list;
        }
    }
}
