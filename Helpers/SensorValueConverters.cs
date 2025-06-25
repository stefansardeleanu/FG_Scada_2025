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
                // Ensure we don't divide by zero
                if (sensor.Config.MaxValue <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"📊 Sensor {sensor.Tag}: MaxValue is {sensor.Config.MaxValue}, returning 0%");
                    return 0;
                }

                // Calculate percentage (0-100)
                float percentage = (sensor.CurrentValue.ProcessValue / sensor.Config.MaxValue) * 100;

                // Ensure percentage is within bounds (0-100)
                percentage = Math.Min(100, Math.Max(0, percentage));

                System.Diagnostics.Debug.WriteLine($"📊 Sensor {sensor.Tag}: Value={sensor.CurrentValue.ProcessValue}, MaxValue={sensor.Config.MaxValue}, Percentage={percentage:F1}%");

                return percentage;
            }

            System.Diagnostics.Debug.WriteLine($"📊 SensorValueToPercentageConverter: value is not Sensor, returning 0");
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
            System.Diagnostics.Debug.WriteLine($"🎨 SensorValueToBarColorConverter called with value: {value?.GetType().Name}");

            if (value is Sensor sensor)
            {
                var color = Colors.Gray; // default

                if (sensor.CurrentValue.ProcessValue >= sensor.Alarms.AlarmLevel2)
                    color = Colors.Red;
                else if (sensor.CurrentValue.ProcessValue >= sensor.Alarms.AlarmLevel1)
                    color = Colors.Orange;
                else
                    color = Colors.Green;

                System.Diagnostics.Debug.WriteLine($"🎨 Sensor {sensor.Tag}: Value={sensor.CurrentValue.ProcessValue}, Alarm1={sensor.Alarms.AlarmLevel1}, Alarm2={sensor.Alarms.AlarmLevel2}, Color={color}");
                return color;
            }

            System.Diagnostics.Debug.WriteLine($"🎨 SensorValueToBarColorConverter returning Gray (value not Sensor)");
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