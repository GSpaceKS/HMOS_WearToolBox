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
        public static BatteryInfo Parse(string output)
        {
            var info = new BatteryInfo();

            var match = Regex.Match(output, @"capacity:\s*(\d+)");
            if (match.Success) info.Capacity = int.Parse(match.Groups[1].Value);

            match = Regex.Match(output, @"voltage:\s*(\d+)");
            if (match.Success) info.Voltage = int.Parse(match.Groups[1].Value);

            match = Regex.Match(output, @"chargingStatus:\s*(\d+)");
            if (match.Success) info.ChargingStatus = int.Parse(match.Groups[1].Value);

            match = Regex.Match(output, @"healthState:\s*(\d+)");
            if (match.Success) info.HealthState = int.Parse(match.Groups[1].Value);

            match = Regex.Match(output, @"temperature:\s*(\d+)");
            if (match.Success) info.Temperature = int.Parse(match.Groups[1].Value);

            return info;
        }
    }
}