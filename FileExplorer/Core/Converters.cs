using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using DevExpress.Data;
using DevExpress.Xpf.Grid;

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

    public class IntToFileSizeConverter : MarkupExtension, IValueConverter
    {
        private const string ByteFormat = "{0:N0} Bytes";
        private const string KiloByteFormat = "{0:N2} KB";
        private const string MegaByteFormat = "{0:N2} MB";
        private const string GigaByteFormat = "{0:N2} GB";
        private const string TeraByteFormat = "{0:N2} TB";

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            decimal size = System.Convert.ToDecimal(value);

            if (size == 0)
                return " ";

            if (size < 1024)
                return String.Format(ByteFormat, size);

            if (size < 1048576)
                return String.Format(KiloByteFormat, size / 1024);

            if (size < 1073741824)
                return String.Format(MegaByteFormat, size / 1048576);

            if (size < 1099511627776)
                return String.Format(GigaByteFormat, size / 1073741824);

            return String.Format(TeraByteFormat, size / 1099511627776);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class IntToDoubleConverter : MarkupExtension, IValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToInt32(value);
        }
    }

    public class MultiBooleanToVisibilityConverter : MarkupExtension, IMultiValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 1)
                return Visibility.Collapsed;

            if (values.OfType<bool>().Any(x => x))
                return Visibility.Visible;
            else 
                return Visibility.Collapsed;
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
            return value != null ? new List<object>(value.ToString().Split(Separator)) : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is IEnumerable<object> list ? String.Join(Separator, list) : null;
        }
    }

    public class ObjectToVisibilityConverter : MarkupExtension, IValueConverter
    {
        public bool Inverse { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Inverse ? Visibility.Visible : Visibility.Collapsed;
            else
                return Inverse ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class EnumToBooleanConverter : MarkupExtension, IValueConverter
    {
        public Enum TrueValue { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Enum enumValue && Enum.Equals(enumValue, TrueValue))
                return true;

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class NumericToBooleanConverter : MarkupExtension, IValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            decimal decimalValue = System.Convert.ToDecimal(value);
            if (decimalValue != 0)
                return true;

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue)
                return 1;

            return 0;
        }
    }

    public class RowHandleToRowNumberConverter : MarkupExtension, IValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int rowHandle = System.Convert.ToInt32(value);

            return rowHandle < 0 ? String.Empty : rowHandle + 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class StringToIncrementalSearchModeConverter : MarkupExtension, IValueConverter
    {
        public bool Inverse { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue && !String.IsNullOrEmpty(stringValue))
                return IncrementalSearchMode.Disabled;

            return IncrementalSearchMode.Enabled;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class NonNullValueConverter : MarkupExtension, IValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            lastValue = value;

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? lastValue : value;
        }

        private object lastValue;
    }

    public class ColumnTypeToUnboundTypeConverter : MarkupExtension, IValueConverter
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is UnboundColumnType unboundType)
            {
                switch (unboundType)
                {
                    case UnboundColumnType.Object:
                        return ColumnType.General;
                    case UnboundColumnType.String:
                        return ColumnType.Text; ;
                    case UnboundColumnType.Integer:
                        return ColumnType.Integer; ;
                    case UnboundColumnType.Decimal:
                        return ColumnType.Decimal; ;
                    case UnboundColumnType.DateTime:
                        return ColumnType.DateTime; ;
                    case UnboundColumnType.Boolean:
                        return ColumnType.Boolean; ;
                    default:
                        return ColumnType.General;
                }
            }

            return ColumnType.General;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ColumnType columnType)
            {
                switch (columnType)
                {
                    case ColumnType.General:
                        return UnboundColumnType.Object;
                    case ColumnType.Text:
                        return UnboundColumnType.String; ;
                    case ColumnType.Integer:
                        return UnboundColumnType.Integer; ;
                    case ColumnType.Decimal:
                        return UnboundColumnType.Decimal; ;
                    case ColumnType.DateTime:
                        return UnboundColumnType.DateTime; ;
                    case ColumnType.Boolean:
                        return UnboundColumnType.Boolean; ;
                    default:
                        return UnboundColumnType.Object;
                }
            }

            return UnboundColumnType.Object;            
        }
    }
}
