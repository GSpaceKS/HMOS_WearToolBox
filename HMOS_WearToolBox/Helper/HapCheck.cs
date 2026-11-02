using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace HMOS_WearToolBox.Helper
{
    public enum CheckLevel
    {
        Fatal,
        HighRisk,
        Info
    }

    public sealed class CheckFinding
    {
        public CheckLevel Level { get; }
        public string Title { get; }
        public List<string> Details { get; } = new List<string>();

        public CheckFinding(CheckLevel level, string title)
        {
            Level = level;
            Title = title;
        }

        public string Marker
        {
            get
            {
                if (Level == CheckLevel.Fatal) return "🔴";
                if (Level == CheckLevel.HighRisk) return "🟡";
                return "🔵";
            }
        }
    }

    /// <summary>改名体检：扫一遍重打包产物，列出没改干净的地方。</summary>
    public static class HapCheck
    {
        private const int MaxListed = 5;

        public static bool HasFatal(IEnumerable<CheckFinding> findings)
        {
            foreach (CheckFinding finding in findings)
                if (finding.Level == CheckLevel.Fatal)
                    return true;
            return false;
        }

        /// <summary>把体检结果排成「🔴 致命 / 🟡 高风险 / 🔵 信息」三段，空段不输出。</summary>
        public static string Format(IList<CheckFinding> findings)
        {
            if (findings.Count == 0)
                return "✅ 体检通过：没有发现旧包名残留。";

            StringBuilder builder = new StringBuilder();
            AppendSection(builder, findings, CheckLevel.Fatal,
                "🔴 致命（会导致安装/签名失败）", "→ 必须修复后才能继续");
            AppendSection(builder, findings, CheckLevel.HighRisk,
                "🟡 高风险（可安装，但运行时可能异常）", "→ 建议逐项确认或人工修复");
            AppendSection(builder, findings, CheckLevel.Info,
                "🔵 信息（潜在影响，无法静态判定）", "→ 一般可以忽略，出现异常时再回头看这一节");
            return builder.ToString().TrimEnd();
        }

        private static void AppendSection(StringBuilder builder, IList<CheckFinding> findings,
            CheckLevel level, string header, string tail)
        {
            bool any = false;
            foreach (CheckFinding finding in findings)
            {
                if (finding.Level != level) continue;

                if (!any)
                {
                    builder.AppendLine(header);
                    any = true;
                }

                builder.AppendLine("   - " + finding.Title);
                foreach (string detail in finding.Details)
                    builder.AppendLine("     " + detail);
            }

            if (any)
            {
                builder.AppendLine("   " + tail);
                builder.AppendLine();
            }
        }

        /// <summary>扫描重打包产物，返回按严重程度排好的问题清单。</summary>
        public static List<CheckFinding> Inspect(string hapPath, string oldBundle, string newBundle)
        {
            List<CheckFinding> findings = new List<CheckFinding>();

            if (!File.Exists(hapPath))
            {
                findings.Add(new CheckFinding(CheckLevel.Fatal, "找不到重打包后的 HAP：" + hapPath));
                return findings;
            }

            if (newBundle.Length > 63)
            {
                findings.Add(new CheckFinding(CheckLevel.Fatal,
                    "新包名长度 " + newBundle.Length + " 超过 63 个字符，系统不接受。"));
            }

            byte[] oldBytes = Encoding.UTF8.GetBytes(oldBundle);
            byte[] newBytes = Encoding.UTF8.GetBytes(newBundle);

            List<string> textLeftovers = new List<string>();
            List<string> abcDetails = new List<string>();
            int abcHits = 0;
            List<string> soDetails = new List<string>();
            int soHits = 0;
            List<string> otherDetails = new List<string>();
            int otherHits = 0;
            int prefixHits = 0;
            List<string> prefixDetails = new List<string>();

            using (ZipArchive zip = ZipFile.OpenRead(hapPath))
            {
                foreach (ZipArchiveEntry entry in zip.Entries)
                {
                    if (entry.FullName.EndsWith("/", StringComparison.Ordinal)) continue;
                    if (entry.Length <= 0 || entry.Length > 256L * 1024 * 1024) continue;

                    string name = entry.FullName.Replace('\\', '/');
                    byte[] data = ReadAll(entry);

                    if (name == "module.json")
                        CheckModuleJson(data, newBundle, findings);
                    else if (name == "pack.info")
                        CheckPackInfo(data, newBundle, findings);

                    // 新包名是在旧包名后面加 6 位随机字母，所以新名字的字节里天然包含旧名字，
                    // 扫描时必须把这类命中排除掉，否则每个改过名的应用都会报「有残留」。
                    List<int> oldStringOffsets = FindAll(data, oldBytes);
                    List<int> indexes = new List<int>();
                    foreach (int offset in oldStringOffsets)
                    {
                        if (!StartsWithNewName(data, offset, newBytes))
                            indexes.Add(offset);
                    }

                    if (indexes.Count > 0)
                    {
                        if (IsPlainText(name))
                        {
                            textLeftovers.Add(name + "（" + indexes.Count + " 处）");
                        }
                        else if (name.EndsWith(".abc", StringComparison.OrdinalIgnoreCase))
                        {
                            foreach (int index in indexes)
                            {
                                // 我们是「追加新串 + 改引用」，旧字符串本身还留在表里；
                                // 只要没任何地方引用它，就是无害残留，不报。
                                if (CountReferences(data, index - 1) == 0) continue;
                                abcHits++;
                                AddOffset(abcDetails, data, index, oldBytes.Length, name);
                            }
                        }
                        else if (name.EndsWith(".so", StringComparison.OrdinalIgnoreCase))
                        {
                            soHits += indexes.Count;
                            int before = soDetails.Count;
                            foreach (int index in indexes)
                            {
                                if (soDetails.Count - before >= MaxListed) break;
                                soDetails.Add("位置: " + name + " offset 0x" + index.ToString("X") +
                                              "（" + Guess(data, index, oldBytes.Length) + "）");
                            }
                            if (soDetails.Count - before == 0)
                                soDetails.Add("位置: " + name + "（共 " + indexes.Count + " 处）");
                        }
                        else
                        {
                            otherHits += indexes.Count;
                            if (otherDetails.Count < MaxListed)
                                otherDetails.Add(name + "（" + indexes.Count + " 处）");
                        }
                    }

                    // .abc 里「旧包名被拆成几段」的痕迹：很可能是运行期拼出来的包名，静态改不了
                    if (name.EndsWith(".abc", StringComparison.OrdinalIgnoreCase) && oldBundle.Length > 6)
                    {
                        byte[] fragment = Encoding.UTF8.GetBytes(oldBundle.Substring(0, oldBundle.Length - 4));
                        List<int> fragmentIndexes = FindAll(data, fragment);
                        foreach (int index in fragmentIndexes)
                        {
                            // 落在旧包名那条字符串表项里的命中不算（那是原串本身，不是拼接产物）
                            bool insideOldString = false;
                            foreach (int start in oldStringOffsets)
                            {
                                if (index >= start && index < start + oldBytes.Length)
                                {
                                    insideOldString = true;
                                    break;
                                }
                            }
                            if (insideOldString) continue;
                            if (StartsWithNewName(data, index, newBytes)) continue;
                            prefixHits++;
                            if (prefixDetails.Count < MaxListed)
                                prefixDetails.Add("位置: " + name + " offset 0x" + index.ToString("X") +
                                                  "（疑似拼接构造的包名）");
                        }
                    }

                    // 产物里应该已经换成新名字了，一处都没有说明替换根本没生效
                    if (name == "module.json" && FindAll(data, newBytes).Count == 0)
                    {
                        findings.Add(new CheckFinding(CheckLevel.Fatal,
                            "module.json 里找不到新包名 \"" + newBundle + "\"，重打包没有生效。"));
                    }
                }
            }

            if (textLeftovers.Count > 0)
            {
                CheckFinding finding = new CheckFinding(CheckLevel.Fatal,
                    "以下文本文件里仍残留旧包名 \"" + oldBundle + "\"：");
                foreach (string item in textLeftovers)
                    finding.Details.Add("- " + item);
                findings.Add(finding);
            }

            if (abcHits > 0)
            {
                CheckFinding finding = new CheckFinding(CheckLevel.HighRisk,
                    "字节码里发现 " + abcHits + " 处旧包名引用（不是整条字符串表项，没法就地替换）：");
                foreach (string item in abcDetails)
                    finding.Details.Add(item);
                findings.Add(finding);
            }

            if (soHits > 0)
            {
                CheckFinding finding = new CheckFinding(CheckLevel.Info,
                    "原生库（.so）里发现 " + soHits + " 处旧包名，静态无法判断是否影响运行：");
                foreach (string item in soDetails)
                    finding.Details.Add(item);
                findings.Add(finding);
            }

            if (prefixHits > 0)
            {
                CheckFinding finding = new CheckFinding(CheckLevel.Info,
                    "检测到 " + prefixHits + " 处旧包名前缀片段，可能是运行期拼接出来的包名：");
                foreach (string item in prefixDetails)
                    finding.Details.Add(item);
                findings.Add(finding);
            }

            if (otherHits > 0)
            {
                CheckFinding finding = new CheckFinding(CheckLevel.Info,
                    "其它二进制文件里发现 " + otherHits + " 处旧包名：");
                foreach (string item in otherDetails)
                    finding.Details.Add("- " + item);
                findings.Add(finding);
            }

            return findings;
        }

        private static void CheckModuleJson(byte[] data, string newBundle, List<CheckFinding> findings)
        {
            string json = Encoding.UTF8.GetString(data);
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("app", out JsonElement app) &&
                    app.TryGetProperty("bundleName", out JsonElement bundle))
                {
                    string actual = bundle.GetString() ?? string.Empty;
                    if (!string.Equals(actual, newBundle, StringComparison.Ordinal))
                    {
                        findings.Add(new CheckFinding(CheckLevel.Fatal,
                            "module.json 的 bundleName = \"" + actual + "\" ≠ \"" + newBundle + "\""));
                    }
                }
            }
            catch (JsonException ex)
            {
                findings.Add(new CheckFinding(CheckLevel.Fatal,
                    "module.json 解析失败：" + ex.Message));
            }
        }

        private static void CheckPackInfo(byte[] data, string newBundle, List<CheckFinding> findings)
        {
            string json = Encoding.UTF8.GetString(data);
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("summary", out JsonElement summary) &&
                    summary.TryGetProperty("app", out JsonElement app) &&
                    app.TryGetProperty("bundleName", out JsonElement bundle))
                {
                    string actual = bundle.GetString() ?? string.Empty;
                    if (!string.Equals(actual, newBundle, StringComparison.Ordinal))
                    {
                        findings.Add(new CheckFinding(CheckLevel.Fatal,
                            "pack.info 的 bundleName = \"" + actual + "\" ≠ \"" + newBundle + "\""));
                    }
                }
            }
            catch (JsonException ex)
            {
                findings.Add(new CheckFinding(CheckLevel.Fatal,
                    "pack.info 解析失败：" + ex.Message));
            }
        }

        private static void AddOffset(List<string> details, byte[] data, int index, int length,
            string entryName)
        {
            if (details.Count >= MaxListed) return;

            string snippet = PrintableRun(data, index, 80);
            string line = "位置: " + entryName + " offset 0x" + index.ToString("X") +
                          "（" + Guess(data, index, length) + "）";
            if (snippet.Length > 0)
                line += Environment.NewLine + "       内容: " + snippet;
            details.Add(line);
        }

        /// <summary>从指定位置往后取一段可读字符，用来让人一眼看出这串东西是干什么的。</summary>
        private static string PrintableRun(byte[] data, int index, int maxLength)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = index; i < data.Length && builder.Length < maxLength; i++)
            {
                byte value = data[i];
                if (value == 0) break;
                if (value < 32 || value > 126) break;
                builder.Append((char)value);
            }
            return builder.ToString();
        }

        /// <summary>看一眼包名前后各 120 字节，猜这东西大概是干什么用的。</summary>
        private static string Guess(byte[] data, int index, int length)
        {
            int start = Math.Max(0, index - 120);
            int end = Math.Min(data.Length, index + length + 120);
            string window = Encoding.ASCII.GetString(data, start, end - start);

            if (index > 0 && data[index - 1] == (byte)(length * 2 + 1))
                return "字符串表项，疑似模块记录里的 bundleName";
            if (index + length < data.Length)
            {
                byte next = data[index + length];
                if (next == (byte)'/' || next == (byte)'.' || next == (byte)':')
                    return "疑似「包名/模块/页面」复合串，可能是卡片或路由地址";
            }
            if (window.IndexOf("bundleName", StringComparison.Ordinal) >= 0)
                return "疑似与 bundleName 字段相邻，可能用于资源路径拼接或比较";
            if (window.IndexOf("hilog", StringComparison.OrdinalIgnoreCase) >= 0 ||
                window.IndexOf("tag", StringComparison.OrdinalIgnoreCase) >= 0)
                return "疑似日志字符串，可忽略";
            return "上下文不明确";
        }

        private static bool IsPlainText(string name)
        {
            if (name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".json5", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".plist", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".cfg", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".info", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        /// <summary>这个位置往后是不是新包名（说明它是新名字的开头，不是旧名字残留）。</summary>
        private static bool StartsWithNewName(byte[] data, int index, byte[] newBytes)
        {
            if (newBytes.Length == 0 || index < 0 || index + newBytes.Length > data.Length)
                return false;
            for (int i = 0; i < newBytes.Length; i++)
                if (data[index + i] != newBytes[i])
                    return false;
            return true;
        }

        private static byte[] ReadAll(ZipArchiveEntry entry)
        {
            using Stream stream = entry.Open();
            using MemoryStream buffer = new MemoryStream();
            stream.CopyTo(buffer);
            return buffer.ToArray();
        }

        private static List<int> FindAll(byte[] data, byte[] pattern)
        {
            List<int> hits = new List<int>();
            if (pattern.Length == 0 || data.Length < pattern.Length) return hits;

            int limit = data.Length - pattern.Length;
            for (int i = 0; i <= limit; i++)
            {
                if (data[i] != pattern[0]) continue;
                bool match = true;
                for (int j = 1; j < pattern.Length; j++)
                {
                    if (data[i + j] != pattern[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match) hits.Add(i);
            }
            return hits;
        }

        /// <summary>数一下文件里有多少个 4 字节小端整数等于 value（.abc 用绝对偏移当字符串 id）。</summary>
        private static int CountReferences(byte[] data, int value)
        {
            if (value <= 0) return 0;

            byte b0 = (byte)(value & 0xFF);
            byte b1 = (byte)((value >> 8) & 0xFF);
            byte b2 = (byte)((value >> 16) & 0xFF);
            byte b3 = (byte)((value >> 24) & 0xFF);

            int count = 0;
            for (int i = 0; i + 4 <= data.Length; i++)
            {
                if (data[i] != b0) continue;
                if (data[i + 1] != b1 || data[i + 2] != b2 || data[i + 3] != b3) continue;
                count++;
            }
            return count;
        }
    }
}
