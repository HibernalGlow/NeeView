using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NeeView
{
    /// <summary>
    /// FolderItem统计信息可见性转换器
    /// </summary>
    [ValueConversion(typeof(int), typeof(Visibility))]
    public class FolderItemStatsToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count && count > 0)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// FolderItem多个统计信息可见性转换器 (任意一个>0则显示)
    /// </summary>
    public class FolderItemAnyStatsToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (var value in values)
            {
                if (value is int count && count > 0)
                {
                    return Visibility.Visible;
                }
            }
            return Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
