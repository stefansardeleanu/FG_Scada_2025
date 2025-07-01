using System.Collections.ObjectModel;
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
                SensorStatus.DetectorDisabled => Colors.Gray, // Disabled = Gray
                _ => Colors.LightGray
            };
        }

        public static Color GetSensorBackgroundColor(SensorStatus status)
        {
            return status switch
            {
                SensorStatus.Normal => Color.FromArgb("#e8f5e8"), // Light green
                SensorStatus.AlarmLevel1 => Color.FromArgb("#fff3cd"), // Light yellow
                SensorStatus.AlarmLevel2 => Color.FromArgb("#f8d7da"), // Light red
                SensorStatus.LineOpenFault => Color.FromArgb("#f8d7da"), // Light red
                SensorStatus.LineShortFault => Color.FromArgb("#f8d7da"), // Light red
                SensorStatus.DetectorError => Color.FromArgb("#f8d7da"), // Light red
                SensorStatus.DetectorDisabled => Color.FromArgb("#e9ecef"), // Light gray
                _ => Color.FromArgb("#f8f9fa") // Very light gray
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
                SensorStatus.DetectorError => "Error",
                SensorStatus.DetectorDisabled => "DISABLED",
                SensorStatus.LineOpenFault => "Line Open",
                SensorStatus.LineShortFault => "Line Short",
                _ => "Unknown"
            };
        }

        public static string GetSensorStatusIcon(SensorStatus status)
        {
            return status switch
            {
                SensorStatus.Normal => "✓",
                SensorStatus.AlarmLevel1 => "⚠",
                SensorStatus.AlarmLevel2 => "🚨",
                SensorStatus.DetectorError => "❌",
                SensorStatus.DetectorDisabled => "⏸", // Pause icon for disabled
                SensorStatus.LineOpenFault => "⚡",
                SensorStatus.LineShortFault => "⚡",
                _ => "?"
            };
        }

        public static bool IsSensorDisabled(SensorStatus status)
        {
            return status == SensorStatus.DetectorDisabled;
        }

        public static (bool HasAlarm, bool HasFault) GetCountyStatus(List<Site> sites)
        {
            bool hasAlarm = sites.Any(s => s.Status.HasAlarm);
            bool hasFault = sites.Any(s => s.Status.HasFault);
            return (hasAlarm, hasFault);
        }

        // Overloaded methods to handle both List<Sensor> and ObservableCollection<Sensor>
        public static (bool HasAlarm, bool HasFault) GetSiteStatus(List<Sensor> sensors)
        {
            // Filter out disabled sensors from status calculation
            var activeSensors = sensors.Where(s => s.CurrentValue.Status != SensorStatus.DetectorDisabled);

            bool hasAlarm = activeSensors.Any(s => s.CurrentValue.Status == SensorStatus.AlarmLevel1 ||
                                                  s.CurrentValue.Status == SensorStatus.AlarmLevel2);
            bool hasFault = activeSensors.Any(s => s.CurrentValue.Status == SensorStatus.LineOpenFault ||
                                                  s.CurrentValue.Status == SensorStatus.LineShortFault ||
                                                  s.CurrentValue.Status == SensorStatus.DetectorError);
            return (hasAlarm, hasFault);
        }

        public static (bool HasAlarm, bool HasFault) GetSiteStatus(ObservableCollection<Sensor> sensors)
        {
            // Filter out disabled sensors from status calculation
            var activeSensors = sensors.Where(s => s.CurrentValue.Status != SensorStatus.DetectorDisabled);

            bool hasAlarm = activeSensors.Any(s => s.CurrentValue.Status == SensorStatus.AlarmLevel1 ||
                                                  s.CurrentValue.Status == SensorStatus.AlarmLevel2);
            bool hasFault = activeSensors.Any(s => s.CurrentValue.Status == SensorStatus.LineOpenFault ||
                                                  s.CurrentValue.Status == SensorStatus.LineShortFault ||
                                                  s.CurrentValue.Status == SensorStatus.DetectorError);
            return (hasAlarm, hasFault);
        }

        public static (int Normal, int Alarm, int Fault, int Disabled) GetSensorCounts(ObservableCollection<Sensor> sensors)
        {
            int normal = sensors.Count(s => s.CurrentValue.Status == SensorStatus.Normal);
            int alarm = sensors.Count(s => s.CurrentValue.Status == SensorStatus.AlarmLevel1 ||
                                          s.CurrentValue.Status == SensorStatus.AlarmLevel2);
            int fault = sensors.Count(s => s.CurrentValue.Status == SensorStatus.LineOpenFault ||
                                          s.CurrentValue.Status == SensorStatus.LineShortFault ||
                                          s.CurrentValue.Status == SensorStatus.DetectorError);
            int disabled = sensors.Count(s => s.CurrentValue.Status == SensorStatus.DetectorDisabled);

            return (normal, alarm, fault, disabled);
        }
    }
}