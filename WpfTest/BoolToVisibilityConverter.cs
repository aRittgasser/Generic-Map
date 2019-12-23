using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace WpfTest
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        private bool _inverted;

        public bool Inverted
        {
            get => _inverted;
            set => _inverted = value;
        }
        private object VisibilityToBool(object value)

        {

            if (!(value is Visibility))
                return DependencyProperty.UnsetValue;

            return ((Visibility)value == Visibility.Visible) ^ Inverted;
        }



        private object BoolToVisibility(object value)

        {
            if (!(value is bool))
                return DependencyProperty.UnsetValue;


            return (bool)value ^ Inverted
                ? Visibility.Visible
                : Visibility.Collapsed;

        }


        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return BoolToVisibility(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return VisibilityToBool(value);
        }
    }
}
