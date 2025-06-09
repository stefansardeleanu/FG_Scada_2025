using System.Globalization;
using FG_Scada_2025.Models;

namespace FG_Scada_2025.Helpers
{
    public class AlarmPriorityToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is AlarmPriority priority)
            {
                return priority switch
                {
                    AlarmPriority.Critical => Colors.Red,
                    AlarmPriority.High => Colors.Orange,
                    AlarmPriority.Medium => Colors.Yellow,
                    AlarmPriority.Low => Colors.LightGray,
                    _ => Colors.Gray
                };
            }
            return Colors.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AlarmTypeToTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is AlarmType type)
            {
                return type switch
                {
                    AlarmType.AlarmLevel1 => "Level 1",
                    AlarmType.AlarmLevel2 => "Level 2",
                    AlarmType.LineOpenFault => "Line Open",
                    AlarmType.LineShortFault => "Line Short",
                    AlarmType.DetectorError => "Error",
                    AlarmType.DetectorDisabled => "Disabled",
                    AlarmType.CommunicationLoss => "Comm Loss",
                    _ => "Unknown"
                };
            }
            return "Unknown";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToActiveTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? "ACTIVE" : "CLEARED";
            }
            return "UNKNOWN";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToActiveColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? Colors.Red : Colors.Green;
            }
            return Colors.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AlarmDurationConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Alarm alarm)
            {
                if (alarm.IsActive)
                {
                    var duration = DateTime.Now - alarm.StartTime;
                    return FormatDuration(duration);
                }
                else if (alarm.EndTime.HasValue)
                {
                    var duration = alarm.EndTime.Value - alarm.StartTime;
                    return FormatDuration(duration);
                }
            }
            return "--";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalDays >= 1)
                return $"{(int)duration.TotalDays}d {duration.Hours}h";
            else if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours}h {duration.Minutes}m";
            else
                return $"{duration.Minutes}m";
        }
    }

    public class AlarmTypeToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is AlarmType type)
            {
                return type switch
                {
                    AlarmType.AlarmLevel1 => Colors.Orange,
                    AlarmType.AlarmLevel2 => Colors.Red,
                    AlarmType.LineOpenFault => Colors.Purple,
                    AlarmType.LineShortFault => Colors.Purple,
                    AlarmType.DetectorError => Colors.DarkRed,
                    AlarmType.DetectorDisabled => Colors.Gray,
                    AlarmType.CommunicationLoss => Colors.Blue,
                    _ => Colors.LightGray
                };
            }
            return Colors.LightGray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}