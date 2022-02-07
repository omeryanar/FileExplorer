using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace FileExplorer.Core
{
    public class StringFormatConverter : MarkupExtension, IValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string format = Properties.Resources.ResourceManager.GetString(parameter.ToString(), Properties.Resources.Culture);

            if (String.IsNullOrEmpty(format))
                return value;
            else
                return String.Format(format, value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class InvalidFileNameConverter : MarkupExtension, IValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString().ContainsInvalidFileNameCharacters();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class IntToFileSizeConverter : MarkupExtension, IValueConverter
    {
        private const string ByteFormat = "{0:0.0} Bytes";
        private const string KiloByteFormat = "{0:0.0} KB";
        private const string MegaByteFormat = "{0:0.0} MB";
        private const string GigaByteFormat = "{0:0.0} GB";

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            decimal size = System.Convert.ToDecimal(value);

            if (size == 0)
                return String.Empty;

            if (size < 1024)
                return String.Format(ByteFormat, size);

            if (size < 1048576)
                return String.Format(KiloByteFormat, size / 1024);

            if (size < 1073741824)
                return String.Format(MegaByteFormat, size / 1048576);

            return String.Format(GigaByteFormat, size / 1073741824);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class RightPaneVisibilityConverter : MarkupExtension, IMultiValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3 || values[0] == null)
                return Visibility.Collapsed;

            try
            {
                if (System.Convert.ToBoolean(values[1]) || System.Convert.ToBoolean(values[2]))
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
            catch (Exception)
            {
                return Visibility.Collapsed;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class FileModelVisibilityConverter : MarkupExtension, IMultiValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 4)
                return false;

            bool isHidden = System.Convert.ToBoolean(values[0]);
            bool isSystem = System.Convert.ToBoolean(values[1]);
            bool showHidden = System.Convert.ToBoolean(values[2]);
            bool showSystem = System.Convert.ToBoolean(values[3]);

            if (!isHidden && !isSystem)
                return true;
            if (showHidden && showSystem)
                return true;
            if (isHidden == showHidden && isSystem == showSystem)
                return true;

            return false;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class StringToEnumerableObjectConverter : MarkupExtension, IValueConverter
    {
        public string Separator { get; set; } = "|";

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString().Split(Separator);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is IEnumerable<object> list ? String.Join(Separator, list) : null;
        }
    }
}
