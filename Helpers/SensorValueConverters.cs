using System.Globalization;
using FG_Scada_2025.Models;

namespace FG_Scada_2025.Helpers
{
    public class SensorValueToPercentageConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Sensor sensor)
            {
                float percentage = (sensor.CurrentValue.ProcessValue / sensor.Config.MaxValue) * 100;
                return Math.Min(100, Math.Max(0, percentage));
            }
            return 0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SensorValueToBarHeightConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Sensor sensor)
            {
                double maxHeight = 150; // Maximum bar height
                float percentage = (sensor.CurrentValue.ProcessValue / sensor.Config.MaxValue);
                return Math.Min(maxHeight, Math.Max(5, percentage * maxHeight));
            }
            return 5;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SensorValueToBarColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Sensor sensor)
            {
                if (sensor.CurrentValue.ProcessValue >= sensor.Alarms.AlarmLevel2)
                    return Colors.Red;
                else if (sensor.CurrentValue.ProcessValue >= sensor.Alarms.AlarmLevel1)
                    return Colors.Orange;
                else
                    return Colors.Green;
            }
            return Colors.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SensorStatusToBackgroundConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SensorStatus status)
            {
                return status switch
                {
                    SensorStatus.Normal => Colors.LightGreen,
                    SensorStatus.AlarmLevel1 => Colors.Orange,
                    SensorStatus.AlarmLevel2 => Colors.Red,
                    SensorStatus.LineOpenFault => Colors.Purple,
                    SensorStatus.LineShortFault => Colors.Purple,
                    SensorStatus.DetectorError => Colors.DarkRed,
                    SensorStatus.DetectorDisabled => Colors.Gray,
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