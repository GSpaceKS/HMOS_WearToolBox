using System.Text.RegularExpressions;

namespace HMOS_WearToolBox.DataParsers
{
    public class DeviceBasicInfo
    {
        public string Name { get; set; } = "未知";
        public string Model { get; set; } = "未知";
        public string SysVersion { get; set; } = "未知";
        public string ApiVersion { get; set; } = "未知";
        public string CpuArch { get; set; } = "未知";
    }

    public static class DeviceInfoParser
    {
        public static DeviceBasicInfo Parse(string output)
        {
            var info = new DeviceBasicInfo();
            if (string.IsNullOrWhiteSpace(output)) return info;

            // 按行分割，去除空行
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length >= 5)
            {
                info.Name = lines[0].Trim();
                info.Model = lines[1].Trim();
                info.SysVersion = lines[2].Trim();
                info.ApiVersion = lines[3].Trim();
                info.CpuArch = lines[4].Trim();
                return info;
            }

            // 降级：正则匹配（兼容不同输出）
            var match = Regex.Match(output, @"const\.product\.name\s*:\s*(.+)");
            if (match.Success) info.Name = match.Groups[1].Value.Trim();
            match = Regex.Match(output, @"const\.product\.model\s*:\s*(.+)");
            if (match.Success) info.Model = match.Groups[1].Value.Trim();
            match = Regex.Match(output, @"const\.product\.software\.version\s*:\s*(.+)");
            if (match.Success) info.SysVersion = match.Groups[1].Value.Trim();
            match = Regex.Match(output, @"const\.ohos\.apiversion\s*:\s*(.+)");
            if (match.Success) info.ApiVersion = match.Groups[1].Value.Trim();
            match = Regex.Match(output, @"const\.product\.cpu\.abilist\s*:\s*(.+)");
            if (match.Success) info.CpuArch = match.Groups[1].Value.Trim();

            return info;
        }
    }
}