using System.Globalization;
using FG_Scada_2025.Models;

namespace FG_Scada_2025.Helpers
{
    public class SiteStatusToColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is SiteStatus status)
            {
                return StatusHelper.GetStatusColor(status);
            }
            return Colors.Gray;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}