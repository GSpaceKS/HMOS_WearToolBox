using System.Text.RegularExpressions;

namespace HMOS_WearToolBox.DataParsers
{
    public class BatteryInfo
    {
        public int Capacity { get; set; } = 0;
        public int Voltage { get; set; } = 0;
        public int ChargingStatus { get; set; } = 0;
        public int HealthState { get; set; } = 0;
        public int Temperature { get; set; } = 0;
    }

    public static class BatteryParser
    {
        public static BatteryInfo Parse(string? output)
        {
            BatteryInfo info = new BatteryInfo();
            if (string.IsNullOrWhiteSpace(output))
                return info;

            info.Capacity = ExtractInt(output, @"capacity:\s*(-?\d+)");
            info.Voltage = ExtractInt(output, @"voltage:\s*(-?\d+)");
            info.ChargingStatus = ExtractInt(output, @"chargingStatus:\s*(-?\d+)");
            info.HealthState = ExtractInt(output, @"healthState:\s*(-?\d+)");
            info.Temperature = ExtractInt(output, @"temperature:\s*(-?\d+)");

            return info;
        }

        /// <summary>
        /// 按模式提取整数。匹配不到、或字段不是合法数字时返回 0，
        /// 不用 int.Parse —— 设备固件版本不同时输出格式会变，抛异常会直接打断整次刷新。
        /// </summary>
        private static int ExtractInt(string output, string pattern)
        {
            Match match = Regex.Match(output, pattern);
            if (!match.Success)
                return 0;

            return int.TryParse(match.Groups[1].Value, out int value) ? value : 0;
        }
    }
}
