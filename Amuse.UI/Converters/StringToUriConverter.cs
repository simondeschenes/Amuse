using System;
using System.Globalization;
using System.Windows.Data;

namespace Amuse.UI.Converters
{
    [ValueConversion(typeof(string), typeof(Uri))]
    public class StringToUriConverter : IValueConverter
    {
        private readonly Uri nullUri = new Uri("about:blank");

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string stringToConvert = (string)value;
            if (stringToConvert != null)
            {
                return new Uri(stringToConvert);
            }
            return nullUri;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Uri uriToConvertBack = (Uri)value;
            if (uriToConvertBack != null && !uriToConvertBack.Equals(nullUri))
            {
                return uriToConvertBack.OriginalString;
            }

            return null;
        }
    }
}
