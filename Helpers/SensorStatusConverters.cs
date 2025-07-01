using System.Globalization;
using FG_Scada_2025.Models;
using FG_Scada_2025.Helpers;

namespace FG_Scada_2025.Helpers
{
    // Enhanced sensor status converters for disabled indication
    public class SensorStatusToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SensorStatus status)
            {
                return StatusHelper.GetSensorStatusColor(status);
            }
            return Colors.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SensorStatusToBackgroundColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SensorStatus status)
            {
                return StatusHelper.GetSensorBackgroundColor(status);
            }
            return Colors.White;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SensorStatusToTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SensorStatus status)
            {
                return StatusHelper.GetSensorStatusText(status);
            }
            return "Unknown";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SensorStatusToIconConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SensorStatus status)
            {
                return StatusHelper.GetSensorStatusIcon(status);
            }
            return "?";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SensorStatusToOpacityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SensorStatus status)
            {
                return status == SensorStatus.DetectorDisabled ? 0.6 : 1.0;
            }
            return 1.0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SensorStatusToDisabledConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SensorStatus status)
            {
                return status == SensorStatus.DetectorDisabled;
            }
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}