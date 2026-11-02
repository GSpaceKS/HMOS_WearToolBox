using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace HMOS_WearToolBox.DataParsers
{
    public class StorageInfo
    {
        public long Total { get; set; } = 0;
        public long Used { get; set; } = 0;
        public long Free { get; set; } = 0;
    }

    public static class StorageParser
    {
        /// <summary>df 的大小字段，例如 7.4G / 512M / 480000。</summary>
        private static readonly Regex SizeField =
            new Regex(@"^\d+(\.\d+)?[KMGTP]?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static StorageInfo Parse(string? output)
        {
            StorageInfo info = new StorageInfo();
            if (string.IsNullOrWhiteSpace(output))
                return info;

            string[] lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // 第一轮：只认挂载点正好是 /data 的那一行（df 的最后一列就是挂载点），最精确
            foreach (string line in lines)
            {
                if (!line.Contains("/data", StringComparison.Ordinal))
                    continue;

                if (TryParseMountLine(line, requireExactMount: true, info))
                    return info;
            }

            // 第二轮：放宽到「包含 /data 的行」，兼容设备路径很长被 df 折行等情况
            foreach (string line in lines)
            {
                if (!line.Contains("/data", StringComparison.Ordinal))
                    continue;

                if (TryParseMountLine(line, requireExactMount: false, info))
                    return info;
            }

            return info;
        }

        /// <summary>
        /// 从一行 df 输出里取出「总容量 / 已用 / 可用」三个字段。
        /// </summary>
        private static bool TryParseMountLine(string line, bool requireExactMount, StorageInfo info)
        {
            string[] fields = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length == 0)
                return false;

            if (requireExactMount && fields[^1] != "/data")
                return false;

            // 按「字段」而不是「子串」匹配：原来的正则会在 /dev/block/mmcblk0p12 里
            // 从 p12 的 "12" 开始匹配，把 12 当成总容量，解析出完全错误的结果。
            for (int i = 0; i + 2 < fields.Length; i++)
            {
                if (!SizeField.IsMatch(fields[i]) ||
                    !SizeField.IsMatch(fields[i + 1]) ||
                    !SizeField.IsMatch(fields[i + 2]))
                {
                    continue;
                }

                info.Total = ParseSize(fields[i]);
                info.Used = ParseSize(fields[i + 1]);
                info.Free = ParseSize(fields[i + 2]);
                return true;
            }

            return false;
        }

        private static long ParseSize(string sizeStr)
        {
            if (string.IsNullOrWhiteSpace(sizeStr))
                return 0;

            sizeStr = sizeStr.Trim();

            long multiplier = 1;  // 没有单位时按 KB 看待
            char unit = sizeStr[^1];

            if (!char.IsDigit(unit))
            {
                multiplier = char.ToUpperInvariant(unit) switch
                {
                    'K' => 1L,
                    'M' => 1024L,
                    'G' => 1024L * 1024,
                    'T' => 1024L * 1024 * 1024,
                    'P' => 1024L * 1024 * 1024 * 1024,
                    _ => 0L
                };

                if (multiplier == 0)
                    return 0;

                sizeStr = sizeStr.Substring(0, sizeStr.Length - 1);
            }

            // 用 InvariantCulture：df 输出的小数点固定是 '.'，
            // 跟随系统区域（例如德语区用 ','）会解析失败。
            return double.TryParse(sizeStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                ? (long)(value * multiplier)
                : 0;
        }
    }
}
