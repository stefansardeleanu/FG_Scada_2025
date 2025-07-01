using System.Globalization;
using FG_Scada_2025.Models;
using Microsoft.Maui.Controls;

namespace FG_Scada_2025.Helpers
{
    public class SensorValueToPercentageConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Sensor sensor)
            {
                // Enhanced null safety
                if (sensor.Config == null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Sensor {sensor.Tag}: Config is NULL, returning 0");
                    return 0;
                }

                if (sensor.CurrentValue == null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Sensor {sensor.Tag}: CurrentValue is NULL, returning 0");
                    return 0;
                }

                // Get the current process value
                float processValue = sensor.CurrentValue.ProcessValue;
                float minValue = sensor.Config.MinValue;
                float maxValue = sensor.Config.MaxValue;

                // FIXED CALCULATION: Handle the range properly
                float range = maxValue - minValue;

                if (range <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Sensor {sensor.Tag}: Invalid range (Min={minValue}, Max={maxValue}), returning 0");
                    return 0;
                }

                // Calculate percentage based on the range
                float percentage = ((processValue - minValue) / range) * 100f;

                // Ensure percentage is within bounds (0-100)
                percentage = Math.Min(100f, Math.Max(0f, percentage));

                // Round to integer
                int intPercentage = (int)Math.Round(percentage);

                System.Diagnostics.Debug.WriteLine($"📊 Sensor {sensor.Tag}: PV={processValue}, Range=[{minValue}-{maxValue}], Percentage={percentage:F1}% → {intPercentage}%");

                return intPercentage;
            }

            System.Diagnostics.Debug.WriteLine($"❌ SensorValueToPercentageConverter: value is not Sensor, returning 0");
            return 0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SensorValueToGridLengthConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Sensor sensor)
            {
                // Enhanced null safety
                if (sensor.Config == null || sensor.CurrentValue == null)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Sensor {sensor?.Tag ?? "Unknown"}: Null config or value, returning 0*");
                    return new GridLength(0, GridUnitType.Star);
                }

                // Get the current process value
                float processValue = sensor.CurrentValue.ProcessValue;
                float minValue = sensor.Config.MinValue;
                float maxValue = sensor.Config.MaxValue;

                // Calculate range
                float range = maxValue - minValue;

                if (range <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Sensor {sensor.Tag}: Invalid range, returning 0*");
                    return new GridLength(0, GridUnitType.Star);
                }

                // Calculate percentage based on the range
                float percentage = ((processValue - minValue) / range) * 100f;

                // Ensure percentage is within bounds (0-100)
                percentage = Math.Min(100f, Math.Max(0f, percentage));

                // Convert to integer, minimum 1 to avoid 0*
                int intPercentage = Math.Max(1, (int)Math.Round(percentage));

                System.Diagnostics.Debug.WriteLine($"🔧 Sensor {sensor.Tag}: PV={processValue}, GridLength={intPercentage}*");

                return new GridLength(intPercentage, GridUnitType.Star);
            }

            System.Diagnostics.Debug.WriteLine($"❌ SensorValueToGridLengthConverter: value is not Sensor, returning 1*");
            return new GridLength(1, GridUnitType.Star);
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
            if (value is Sensor sensor && sensor.Config != null && sensor.CurrentValue != null)
            {
                double maxHeight = 150;

                float range = sensor.Config.MaxValue - sensor.Config.MinValue;
                if (range <= 0) return 5;

                float percentage = ((sensor.CurrentValue.ProcessValue - sensor.Config.MinValue) / range);
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
            if (value is Sensor sensor && sensor.CurrentValue != null && sensor.Alarms != null)
            {
                var color = Colors.Gray;
                float processValue = sensor.CurrentValue.ProcessValue;

                if (processValue >= sensor.Alarms.AlarmLevel2)
                    color = Colors.Red;
                else if (processValue >= sensor.Alarms.AlarmLevel1)
                    color = Colors.Orange;
                else
                    color = Colors.Green;

                return color;
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