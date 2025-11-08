using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NeeView
{
    /// <summary>
    /// FolderItem 是否为视频文件的转换器
    /// </summary>
    public class FolderItemIsMediaConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not FileFolderItem fileFolderItem)
                return Visibility.Collapsed;

            // 判断是否是视频文件
            var page = fileFolderItem.GetPage();
            if (page?.ArchiveEntry?.IsMedia() == true)
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
}
