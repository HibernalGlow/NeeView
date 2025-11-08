using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace NeeView
{
    public class FileInfoOverlayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not FileInformationSource source)
                return "";

            var page = source.Page;
            var pictureInfo = source.PictureInfo;

            // 构建信息文本
            var fileName = Path.GetFileName(page?.EntryName ?? "");
            var fileSize = page?.ArchiveEntry?.Length ?? 0;
            var sizeText = fileSize > 0 ? FormatFileSize(fileSize) : "";
            
            var resolution = "";
            if (pictureInfo != null && pictureInfo.OriginalSize.Width > 0)
            {
                resolution = $"{pictureInfo.OriginalSize.Width}x{pictureInfo.OriginalSize.Height}";
            }

            // 组合信息
            var parts = new System.Collections.Generic.List<string>();
            if (!string.IsNullOrEmpty(fileName))
                parts.Add(fileName);
            if (!string.IsNullOrEmpty(sizeText))
                parts.Add(sizeText);
            if (!string.IsNullOrEmpty(resolution))
                parts.Add(resolution);

            return string.Join(" | ", parts);
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
