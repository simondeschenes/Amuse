using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Amuse.UI.Converters
{
    // Class that represents the "All" Item in the combo box
    // It has properties for the bindings that are read by the XAML
    // in order to prevent binding link errors
    public class ComboBoxAllItemConverter : IValueConverter
    {
        private class AllComboxBoxItem
        {
            public string Template
            {
                get
                {
                    return "All";
                }
            }

            public string Name
            {
                get
                {
                    return "All";
                }
            }

            public bool IsUserTemplate
            {
                get
                {
                    return false;
                }
            }
        }

        private static readonly AllComboxBoxItem _allComboxBoxItem = new AllComboxBoxItem();
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable container)
            {
                IEnumerable<object> genericContainer = container.OfType<object>();
                IEnumerable<object> emptyItem = new object[] { _allComboxBoxItem };
                return emptyItem.Concat(genericContainer);
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str && str.Equals(_allComboxBoxItem.Template))
            {
                return null;
            }
            return value;
        }
    }
}
