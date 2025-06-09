using FG_Scada_2025.Models;

namespace FG_Scada_2025.Helpers
{
    public static class StatusHelper
    {
        public static Color GetStatusColor(SiteStatus status)
        {
            if (status.HasFault)
                return Colors.Red;
            else if (status.HasAlarm)
                return Colors.Orange;
            else
                return Colors.Green;
        }

        public static Color GetSensorStatusColor(SensorStatus status)
        {
            return status switch
            {
                SensorStatus.Normal => Colors.Green,
                SensorStatus.AlarmLevel1 => Colors.Orange,
                SensorStatus.AlarmLevel2 => Colors.Red,
                SensorStatus.LineOpenFault => Colors.Red,
                SensorStatus.LineShortFault => Colors.Red,
                SensorStatus.DetectorError => Colors.Red,
                SensorStatus.DetectorDisabled => Colors.Gray,
                _ => Colors.Gray
            };
        }

        public static string GetStatusText(SiteStatus status)
        {
            if (status.HasFault)
                return "FAULT";
            else if (status.HasAlarm)
                return "ALARM";
            else
                return "NORMAL";
        }

        public static string GetSensorStatusText(SensorStatus status)
        {
            return status switch
            {
                SensorStatus.Normal => "Normal",
                SensorStatus.AlarmLevel1 => "Alarm L1",
                SensorStatus.AlarmLevel2 => "Alarm L2",
                SensorStatus.LineOpenFault => "Line Open",
                SensorStatus.LineShortFault => "Line Short",
                SensorStatus.DetectorError => "Error",
                SensorStatus.DetectorDisabled => "Disabled",
                _ => "Unknown"
            };
        }

        public static (bool HasAlarm, bool HasFault) GetCountyStatus(List<Site> sites)
        {
            bool hasAlarm = sites.Any(s => s.Status.HasAlarm);
            bool hasFault = sites.Any(s => s.Status.HasFault);
            return (hasAlarm, hasFault);
        }

        public static (bool HasAlarm, bool HasFault) GetSiteStatus(List<Sensor> sensors)
        {
            bool hasAlarm = sensors.Any(s => s.CurrentValue.Status == SensorStatus.AlarmLevel1 ||
                                           s.CurrentValue.Status == SensorStatus.AlarmLevel2);
            bool hasFault = sensors.Any(s => s.CurrentValue.Status == SensorStatus.LineOpenFault ||
                                           s.CurrentValue.Status == SensorStatus.LineShortFault ||
                                           s.CurrentValue.Status == SensorStatus.DetectorError);
            return (hasAlarm, hasFault);
        }
    }
}