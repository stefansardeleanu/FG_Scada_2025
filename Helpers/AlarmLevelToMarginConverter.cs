using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FG_Scada_2025.Helpers
{
    public class AlarmLevelToMarginConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is float alarmLevel)
            {
                // Convert alarm level to margin for positioning alarm line
                // Assuming max value is 100 and bar height is 150
                return (alarmLevel / 100.0) * 150;
            }
            return 0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
