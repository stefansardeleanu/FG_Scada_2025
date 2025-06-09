using System.Globalization;
using FG_Scada_2025.Models;

namespace FG_Scada_2025.Helpers
{
    // Converter for W (Warning/Alarm Level 1) indicator
    public class BoolToAlarmColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isActive && isActive)
                return Colors.Orange;
            return Colors.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Converter for A (Alarm Level 2) indicator
    public class BoolToAlarm2ColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isActive && isActive)
                return Colors.Red;
            return Colors.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Converter for F (Fault) indicator
    public class StatusToFaultColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SensorStatus status)
            {
                return status switch
                {
                    SensorStatus.LineOpenFault => Colors.Purple,
                    SensorStatus.LineShortFault => Colors.Purple,
                    SensorStatus.DetectorError => Colors.Red,
                    SensorStatus.DetectorDisabled => Colors.Gray,
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

    // Converter for sensor type color (G, F, M, S background colors)
    public class SensorTypeToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SensorType type)
            {
                return type switch
                {
                    SensorType.GasDetector => Colors.Green,
                    SensorType.TemperatureSensor => Colors.Blue,
                    SensorType.PressureSensor => Colors.Orange,
                    SensorType.FlowSensor => Colors.Purple,
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

    // Converter for sensor type letter (G, F, M, S, etc.)
    public class SensorTypeToLetterConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SensorType type)
            {
                return type switch
                {
                    SensorType.GasDetector => "G",
                    SensorType.TemperatureSensor => "T",
                    SensorType.PressureSensor => "P",
                    SensorType.FlowSensor => "F",
                    _ => "?"
                };
            }
            return "?";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Converter for horizontal bar width
    public class SensorValueToBarWidthConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is Sensor sensor)
            {
                double maxWidth = 200; // Maximum bar width
                float percentage = (sensor.CurrentValue.ProcessValue / sensor.Config.MaxValue);
                return Math.Min(maxWidth, Math.Max(5, percentage * maxWidth));
            }
            return 5;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}