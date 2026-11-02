using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HMOS_WearToolBox.Helper
{
    public sealed class HapInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public string BundleName { get; set; } = string.Empty;
        public string VersionName { get; set; } = string.Empty;
        public int VersionCode { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public int MinApiVersion { get; set; }
        public int TargetApiVersion { get; set; }
        public List<string> DeviceTypes { get; set; } = new List<string>();

        /// <summary>入口 Ability 名（module.abilities[0].name），安装完用来在手表上拉起。</summary>
        public string MainAbility { get; set; } = string.Empty;
        public long FileSize { get; set; }

        public string DeviceTypesText => DeviceTypes.Count == 0 ? "未声明" : string.Join(", ", DeviceTypes);

        public string SizeText =>
            FileSize <= 0 ? "—" : (FileSize / 1024.0 / 1024.0).ToString("0.0") + " MB";
    }

    /// <summary>解析与重打包 HAP：只改 bundleName 与 deviceTypes。</summary>
    public static class HapPatcher
    {
        /// <summary>超过这个体积的条目一律当成二进制，不做文本替换，避免把大 so 读进内存。</summary>
        private const long MaxTextEntryBytes = 8L * 1024 * 1024;

        private static readonly Regex DeviceTypesRegex =
            new Regex("\"deviceTypes\"\\s*:\\s*\\[([^\\]]*)\\]", RegexOptions.Compiled);

        private static readonly Regex DeviceTypeListRegex =
            new Regex("\"deviceType\"\\s*:\\s*\\[([^\\]]*)\\]", RegexOptions.Compiled);

        /// <summary>
        /// 读取 HAP 的 module.json，取出包名、版本、模块名与设备类型。
        /// </summary>
        public static HapInfo ReadInfo(string hapPath)
        {
            if (!File.Exists(hapPath))
                throw new FileNotFoundException("找不到 HAP 文件。", hapPath);

            using ZipArchive zip = ZipFile.OpenRead(hapPath);
            ZipArchiveEntry? entry = zip.GetEntry("module.json");
            if (entry == null)
                throw new InvalidDataException("这不是标准的 HAP：压缩包里没有 module.json。");

            string json;
            using (Stream stream = entry.Open())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                json = reader.ReadToEnd();
            }

            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;

            HapInfo info = new HapInfo
            {
                FilePath = hapPath,
                FileSize = new FileInfo(hapPath).Length
            };

            if (root.TryGetProperty("app", out JsonElement app))
            {
                info.BundleName = ReadString(app, "bundleName");
                info.VersionName = ReadString(app, "versionName");
                info.VersionCode = ReadInt(app, "versionCode");
                info.MinApiVersion = ReadInt(app, "minAPIVersion");
                info.TargetApiVersion = ReadInt(app, "targetAPIVersion");
            }

            if (root.TryGetProperty("module", out JsonElement module))
            {
                info.ModuleName = ReadString(module, "name");
                if (module.TryGetProperty("abilities", out JsonElement abilities) &&
                    abilities.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement ability in abilities.EnumerateArray())
                    {
                        string name = ReadString(ability, "name");
                        if (name.Length > 0)
                        {
                            info.MainAbility = name;
                            break;
                        }
                    }
                }
                if (module.TryGetProperty("deviceTypes", out JsonElement types) &&
                    types.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement item in types.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                            info.DeviceTypes.Add(item.GetString() ?? string.Empty);
                    }
                }
            }

            return info;
        }

        /// <summary>
        /// 在原包名后追加 6 位随机小写字母，得到一个大概率未被占用的新包名。
        /// </summary>
        public static string SuggestBundleName(string bundleName, int length = 6)
        {
            const string alphabet = "abcdefghijklmnopqrstuvwxyz";
            StringBuilder builder = new StringBuilder(bundleName);
            for (int i = 0; i < length; i++)
                builder.Append(alphabet[Random.Shared.Next(alphabet.Length)]);
            return builder.ToString();
        }

        /// <summary>
        /// 重新打包：替换包名、补 deviceTypes，其余条目原样复制。
        /// </summary>
        public static void Repack(
            HapInfo info,
            string newBundleName,
            IReadOnlyList<string> extraDeviceTypes,
            string targetHap,
            Action<string> log)
        {
            string sourceHap = info.FilePath;
            string oldBundleName = info.BundleName;

            if (string.Equals(oldBundleName, newBundleName, StringComparison.Ordinal))
                throw new InvalidOperationException("新包名和原包名相同，不需要重打包。");

            string? folder = Path.GetDirectoryName(targetHap);
            if (!string.IsNullOrEmpty(folder))
                Directory.CreateDirectory(folder);
            if (File.Exists(targetHap))
                File.Delete(targetHap);

            int replacedFiles = 0;
            int addedTypes = 0;
            int abcPatched = 0;
            int abcStrings = 0;

            using (ZipArchive source = ZipFile.OpenRead(sourceHap))
            using (FileStream output = new FileStream(targetHap, FileMode.Create, FileAccess.Write, FileShare.None))
            using (ZipArchive target = new ZipArchive(output, ZipArchiveMode.Create))
            {
                foreach (ZipArchiveEntry entry in source.Entries)
                {
                    // 目录条目不需要写进新包，CreateEntry 会按路径自动建目录结构
                    if (entry.FullName.EndsWith("/", StringComparison.Ordinal))
                        continue;

                    // HAP 的条目必须用正斜杠：设备按 ets/modules.abc 这样的路径查找，
                    // 用反斜杠写进去会导致 JS 字节码与资源索引全部找不到。
                    string entryName = entry.FullName.Replace('\\', '/');

                    // 原始 HAP 的条目基本都是 stored（CompressedLength == Length），resources.index
                    // 会被系统直接 mmap，压成 deflate 之后 ResourceManager 会报 Parse ResHeader failed。
                    // 所以这里默认按 stored 写，只有原本就是 deflate 的条目才继续压缩。
                    CompressionLevel level = entry.CompressedLength == entry.Length
                        ? CompressionLevel.NoCompression
                        : CompressionLevel.Optimal;

                    ZipArchiveEntry created = target.CreateEntry(entryName, level);
                    try { created.LastWriteTime = entry.LastWriteTime; } catch { }

                    using Stream input = entry.Open();
                    using Stream outStream = created.Open();

                    if (IsTextEntry(entry))
                    {
                        string text;
                        using (StreamReader reader = new StreamReader(input, Encoding.UTF8, true, 4096, true))
                        {
                            text = reader.ReadToEnd();
                        }

                        string patched = text;
                        if (patched.IndexOf(oldBundleName, StringComparison.Ordinal) >= 0)
                        {
                            patched = patched.Replace(oldBundleName, newBundleName);
                            replacedFiles++;
                        }

                        if (entry.FullName == "module.json")
                        {
                            patched = AddDeviceTypes(patched, DeviceTypesRegex, extraDeviceTypes, ref addedTypes);
                        }
                        else if (entry.FullName == "pack.info")
                        {
                            patched = AddDeviceTypes(patched, DeviceTypeListRegex, extraDeviceTypes, ref addedTypes);
                        }

                        byte[] bytes = new UTF8Encoding(false).GetBytes(patched);
                        outStream.Write(bytes, 0, bytes.Length);
                    }
                    else if (entryName.EndsWith(".abc", StringComparison.OrdinalIgnoreCase))
                    {
                        using (MemoryStream buffer = new MemoryStream())
                        {
                            input.CopyTo(buffer);
                            byte[] abcBytes = buffer.ToArray();
                            int patchedCount = 0;
                            byte[] patchedAbc = PatchAbcBundleName(abcBytes, oldBundleName, newBundleName, ref patchedCount);
                            if (patchedCount > 0)
                            {
                                abcPatched++;
                                abcStrings += patchedCount;
                                log("已改写 " + entryName + " 里的 " + patchedCount + " 处包名字符串");
                            }
                            outStream.Write(patchedAbc, 0, patchedAbc.Length);
                        }
                    }
                    else
                    {
                        input.CopyTo(outStream);
                    }
                }
            }

            log($"已替换包名：{oldBundleName} → {newBundleName}（{replacedFiles} 个文件）");
            if (abcPatched > 0)
                log("已同步 " + abcPatched + " 个字节码文件里的 " + abcStrings + " 处包名字符串");
            if (addedTypes > 0)
                log("已补充 " + addedTypes + " 处 deviceTypes：" + string.Join(", ", extraDeviceTypes));
            log($"新 HAP：{targetHap}");
        }

        private static string AddDeviceTypes(string text, Regex pattern, IReadOnlyList<string> types, ref int counter)
        {
            MatchCollection matches = pattern.Matches(text);
            if (matches.Count == 0)
                return text;

            StringBuilder builder = new StringBuilder(text.Length + 32);
            int lastIndex = 0;

            foreach (Match match in matches)
            {
                string inner = match.Groups[1].Value;
                List<string> missing = new List<string>();
                foreach (string type in types)
                {
                    if (inner.IndexOf("\"" + type + "\"", StringComparison.OrdinalIgnoreCase) < 0)
                        missing.Add("\"" + type + "\"");
                }

                builder.Append(text, lastIndex, match.Index - lastIndex);
                if (missing.Count == 0)
                {
                    builder.Append(match.Value);
                }
                else
                {
                    string merged = inner.TrimEnd().TrimEnd(',');
                    builder.Append(text, match.Index, match.Groups[1].Index - match.Index);
                    builder.Append(merged);
                    builder.Append(',');
                    builder.Append(string.Join(",", missing));
                    builder.Append(']');
                    counter += missing.Count;
                }

                lastIndex = match.Index + match.Length;
            }

            builder.Append(text, lastIndex, text.Length - lastIndex);
            return builder.ToString();
        }

        /// <summary>同步修改 ets/*.abc 字符串表里的包名，否则改名后运行时会加载失败。</summary>
        private static byte[] PatchAbcBundleName(byte[] data, string oldName, string newName, ref int patchedStrings)
        {
            if (data.Length < 64 ||
                data[0] != (byte)'P' || data[1] != (byte)'A' || data[2] != (byte)'N' || data[3] != (byte)'D' || data[4] != (byte)'A')
                return data;

            byte[] oldBytes = Encoding.UTF8.GetBytes(oldName);
            // 字符串表每项前面是 1 字节前缀：utf16 字节长度 + 1（含结尾的 0）
            int oldPrefix = oldName.Length * 2 + 1;
            int newPrefix = newName.Length * 2 + 1;
            if (oldBytes.Length == 0 || oldPrefix >= 0x80 || newPrefix >= 0x80)
                return data;

            List<int> ids = new List<int>();
            for (int i = 0; i + oldBytes.Length + 2 <= data.Length; i++)
            {
                if (data[i] != (byte)oldPrefix)
                    continue;

                bool match = true;
                for (int j = 0; j < oldBytes.Length; j++)
                {
                    if (data[i + 1 + j] != oldBytes[j])
                    {
                        match = false;
                        break;
                    }
                }

                if (match && data[i + 1 + oldBytes.Length] == 0)
                    ids.Add(i);
            }

            if (ids.Count == 0)
                return data;

            byte[] newBytes = Encoding.UTF8.GetBytes(newName);
            List<byte> tail = new List<byte>(newBytes.Length + 8);
            tail.Add((byte)newPrefix);
            tail.AddRange(newBytes);
            tail.Add(0);

            int newOffset = data.Length;
            byte[] result = new byte[data.Length + tail.Count];
            Buffer.BlockCopy(data, 0, result, 0, data.Length);
            Buffer.BlockCopy(tail.ToArray(), 0, result, data.Length, tail.Count);

            int patched = 0;
            foreach (int id in ids)
            {
                for (int i = 0; i + 4 <= data.Length; i++)
                {
                    if (result[i] == (byte)(id & 0xFF) &&
                        result[i + 1] == (byte)((id >> 8) & 0xFF) &&
                        result[i + 2] == (byte)((id >> 16) & 0xFF) &&
                        result[i + 3] == (byte)((id >> 24) & 0xFF))
                    {
                        result[i] = (byte)(newOffset & 0xFF);
                        result[i + 1] = (byte)((newOffset >> 8) & 0xFF);
                        result[i + 2] = (byte)((newOffset >> 16) & 0xFF);
                        result[i + 3] = (byte)((newOffset >> 24) & 0xFF);
                        patched++;
                    }
                }
            }

            if (patched == 0)
                return data;

            // header: [0..4) magic, [4..8) 保留, [8..12) adler32, [12..16) 版本, [16..20) 文件长度
            WriteUInt32(result, 16, (uint)result.Length);
            WriteUInt32(result, 8, Adler32(result, 12));
            patchedStrings += ids.Count;
            return result;
        }

        private static void WriteUInt32(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
        }

        private static uint Adler32(byte[] buffer, int start)
        {
            const uint mod = 65521;
            uint a = 1;
            uint b = 0;
            for (int i = start; i < buffer.Length; i++)
            {
                a = (a + buffer[i]) % mod;
                b = (b + a) % mod;
            }
            return (b << 16) | a;
        }

        private static bool IsTextEntry(ZipArchiveEntry entry)
        {
            if (entry.Length > MaxTextEntryBytes || entry.Length == 0)
                return false;

            string name = entry.FullName;
            return name.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                   name.EndsWith(".info", StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadString(JsonElement element, string name)
        {
            if (element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String)
                return value.GetString() ?? string.Empty;
            return string.Empty;
        }

        private static int ReadInt(JsonElement element, string name)
        {
            if (element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt32(out int result))
                return result;
            return 0;
        }
    }
}
