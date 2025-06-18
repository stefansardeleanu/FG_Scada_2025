using System.Globalization;

namespace FG_Scada_2025.Helpers
{
    public class BoolToRealTimeColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isEnabled)
            {
                return isEnabled ? "#27ae60" : "#e74c3c"; // Green when ON, Red when OFF
            }
            return "#e74c3c"; // Default to red (OFF)
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}