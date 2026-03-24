using System;
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
        public static StorageInfo Parse(string output)
        {
            var info = new StorageInfo();
            if (string.IsNullOrWhiteSpace(output)) return info;

            // 按行分割，寻找包含 /data 的行（挂载点）
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("/data"))
                {
                    // 匹配三个大小字段，支持单位 G、M、K
                    var match = Regex.Match(line, @"\b([\d\.]+[GMK]?)\s+([\d\.]+[GMK]?)\s+([\d\.]+[GMK]?)");
                    if (match.Success)
                    {
                        info.Total = ParseSize(match.Groups[1].Value);
                        info.Used = ParseSize(match.Groups[2].Value);
                        info.Free = ParseSize(match.Groups[3].Value);
                        break;
                    }
                }
            }
            return info;
        }

        private static long ParseSize(string sizeStr)
        {
            sizeStr = sizeStr.Trim();
            if (sizeStr.EndsWith("G", StringComparison.OrdinalIgnoreCase))
            {
                double val = double.Parse(sizeStr.Substring(0, sizeStr.Length - 1));
                return (long)(val * 1024 * 1024); // 转为 KB
            }
            else if (sizeStr.EndsWith("M", StringComparison.OrdinalIgnoreCase))
            {
                double val = double.Parse(sizeStr.Substring(0, sizeStr.Length - 1));
                return (long)(val * 1024);
            }
            else if (sizeStr.EndsWith("K", StringComparison.OrdinalIgnoreCase))
            {
                double val = double.Parse(sizeStr.Substring(0, sizeStr.Length - 1));
                return (long)val;
            }
            else
            {
                if (double.TryParse(sizeStr, out double val))
                    return (long)val;
                return 0;
            }
        }
    }
}