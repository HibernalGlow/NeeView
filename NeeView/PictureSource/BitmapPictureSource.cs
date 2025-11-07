using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NeeView.SuperResolution;

namespace NeeView
{
    /// <summary>
    /// Picture 用のリソース
    /// </summary>
    public class BitmapPictureSource : IPictureSource<IStreamSource>
    {
        private static readonly BitmapFactory _bitmapFactory = new();
        private static readonly SuperResolutionHelper _srHelper = new();


        public BitmapPictureSource(ArchiveEntry archiveEntry, PictureInfo? pictureInfo)
        {
            ArchiveEntry = archiveEntry;
            PictureInfo = pictureInfo;
        }


        public ArchiveEntry ArchiveEntry { get; } 

        public PictureInfo? PictureInfo { get; }


        /// <summary>
        /// ImageSource作成
        /// </summary>
        /// <remarks>
        /// 元データは _pageContent からではなく引数で渡す
        /// </remarks>
        /// <param name="streamSource">元データ</param>
        /// <param name="size">作成する画像サイズ。Size.Emptyの場合は元データサイズで作成</param>
        /// <param name="setting">生成オプション</param>
        /// <param name="token">キャンセルトークン</param>
        /// <returns>ImageSource</returns>
        public async ValueTask<ImageSource> CreateImageSourceAsync(IStreamSource streamSource, Size size, BitmapCreateSetting setting, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            //Debug.WriteLine($"{ArchiveEntry}, {size:f0}", "CreateImageSource()");

            using var stream = await streamSource.OpenStreamAsync(token);

            if (setting.IsKeepAspectRatio && !size.IsEmpty)
            {
                size = new Size(size.Width, 0);
            }

            var bitmapSource = _bitmapFactory.CreateBitmapSource(stream, PictureInfo?.BitmapInfo, size, setting, token);

            // 色情報とBPP設定。
            PictureInfo?.SetPixelInfo(bitmapSource);

            // 🔥 自动超分处理 - 像 AVIF/JXL 一样直接传递像素数据
            var config = SuperResolutionConfig.Current;
            if (config != null && config.IsEnabled && config.AutoApplyOnView)
            {
                // 检查是否应该处理
                if (_srHelper.ShouldProcess(bitmapSource, config))
                {
                    try
                    {
                        SuperResolutionLogger.Info($"[自动超分] 压缩包图片: {ArchiveEntry.EntryName}");
                        var srResult = await _srHelper.ProcessBitmapSourceAsync(bitmapSource, config, token);
                        if (srResult != null)
                        {
                            SuperResolutionLogger.Info($"[自动超分成功] {ArchiveEntry.EntryName}: {bitmapSource.PixelWidth}x{bitmapSource.PixelHeight} -> {srResult.PixelWidth}x{srResult.PixelHeight}");
                            return srResult;
                        }
                        else
                        {
                            SuperResolutionLogger.Warning($"[自动超分失败] {ArchiveEntry.EntryName}, 使用原图");
                        }
                    }
                    catch (Exception ex)
                    {
                        SuperResolutionLogger.Error($"[自动超分异常] {ArchiveEntry.EntryName}: {ex.Message}", ex);
                    }
                }
            }

            return bitmapSource;
        }

        public async ValueTask<byte[]> CreateImageAsync(IStreamSource streamSource, Size size, BitmapCreateSetting setting, BitmapImageFormat format, int quality, CancellationToken token)
        {
            using var stream = await streamSource.OpenStreamAsync(token);
            return CreateImage(stream, size, setting, format, quality, token);
        }

        public byte[] CreateImage(Stream stream, Size size, BitmapCreateSetting setting, BitmapImageFormat format, int quality, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            using var outStream = new MemoryStream();
            _bitmapFactory.CreateImage(stream, PictureInfo?.BitmapInfo, outStream, size, format, quality, setting, token);
            return outStream.ToArray();
        }

        public async ValueTask<byte[]> CreateThumbnailAsync(IStreamSource streamSource, ThumbnailProfile profile, CancellationToken token)
        {
            using var stream = await streamSource.OpenStreamAsync(token);
            return CreateThumbnail(stream, profile, token);
        }

        public byte[] CreateThumbnail(Stream stream, ThumbnailProfile profile, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            Size size;
            BitmapInfo? bitmapInfo;
            if (PictureInfo != null)
            {
                size = PictureInfo.Size;
                bitmapInfo = PictureInfo.BitmapInfo;
            }
            else
            {
                bitmapInfo = BitmapInfo.Create(stream);
                size = bitmapInfo.IsTranspose ? bitmapInfo.GetPixelSize().Transpose() : bitmapInfo.GetPixelSize();
            }

            size = ThumbnailProfile.GetThumbnailSize(size);
            var setting = profile.CreateBitmapCreateSetting(bitmapInfo?.Metadata?.IsOrientationEnabled == true);
            stream.Seek(0, SeekOrigin.Begin);
            return CreateImage(stream, size, setting, Config.Current.Thumbnail.Format, Config.Current.Thumbnail.Quality, token);
        }


        public Size FixedSize(Size size)
        {
            Debug.Assert(PictureInfo != null);

            var maxWidth = Math.Max(PictureInfo.Size.Width, Config.Current.Performance.MaximumSize.Width);
            var maxHeight = Math.Max(PictureInfo.Size.Height, Config.Current.Performance.MaximumSize.Height);
            var maxSize = new Size(maxWidth, maxHeight);
            return size.Limit(maxSize);
        }
    }
}
