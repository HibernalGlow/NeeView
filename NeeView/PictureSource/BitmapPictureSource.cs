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

            // 🔥 自动超分处理
            // 🎯 重构:使用严格的状态管理系统
            var config = SuperResolutionConfig.Current;
            
            // 🔥 调试:输出全局开关状态
            SuperResolutionLogger.Info($"[决策检查] IsEnabled={config?.IsEnabled}, AutoApplyOnView={config?.AutoApplyOnView}");
            
            if (config != null && config.IsEnabled)
            {
                long fileSize = ArchiveEntry?.Length ?? -1;
                var entryName = ArchiveEntry?.EntryName ?? "Unknown";
                var imagePath = ArchiveEntry?.SystemPath ?? "";

                if (!string.IsNullOrEmpty(imagePath))
                {
                    // 🎯 步骤 1: 查询数据库用户偏好
                    var shouldSkip = SuperResolutionStateManager.Current.ShouldSkipSuperResolution(imagePath, out var userPreference);

                    if (shouldSkip)
                    {
                        // 用户明确禁用
                        SuperResolutionLogger.Info($"[跳过超分] {entryName} (用户已禁用)");
                        return bitmapSource;
                    }
                    else if (userPreference == SuperResolutionUserPreference.Enabled)
                    {
                        // 🎯 用户明确启用 - 强制超分,忽略其他条件
                        try
                        {
                            SuperResolutionLogger.Info($"[强制超分] {entryName} (用户已启用)");
                            SuperResolutionStateManager.Current.SetActualStatus(imagePath, SuperResolutionActualStatus.Processing, "ForcedByUser");
                            
                            var srResult = await _srHelper.ProcessBitmapSourceAsync(bitmapSource, config, token);
                            if (srResult != null)
                            {
                                SuperResolutionLogger.Info($"[强制超分成功] {entryName}: {bitmapSource.PixelWidth}x{bitmapSource.PixelHeight} → {srResult.PixelWidth}x{srResult.PixelHeight}");
                                
                                // 记录到缓存和数据库
                                var cache = SuperResolutionImageCache.Current;
                                cache.Update(imagePath, item =>
                                {
                                    item.OriginalWidth = bitmapSource.PixelWidth;
                                    item.OriginalHeight = bitmapSource.PixelHeight;
                                    item.SuperResolutionWidth = srResult.PixelWidth;
                                    item.SuperResolutionHeight = srResult.PixelHeight;
                                    item.Status = SuperResolutionImageStatus.Completed;
                                });
                                
                                var record = new SuperResolutionStateRecord
                                {
                                    ImagePath = imagePath,
                                    UserPreference = SuperResolutionUserPreference.Enabled,
                                    ActualStatus = SuperResolutionActualStatus.Completed,
                                    OriginalWidth = bitmapSource.PixelWidth,
                                    OriginalHeight = bitmapSource.PixelHeight,
                                    SuperResolutionWidth = srResult.PixelWidth,
                                    SuperResolutionHeight = srResult.PixelHeight
                                };
                                SuperResolutionStateManager.Current.SaveRecord(record, "ForcedComplete");
                                
                                return srResult;
                            }
                            else
                            {
                                SuperResolutionLogger.Warning($"[强制超分失败] {entryName}, 使用原图");
                                SuperResolutionStateManager.Current.SetActualStatus(imagePath, SuperResolutionActualStatus.Failed, "ForcedFailed");
                            }
                        }
                        catch (Exception ex)
                        {
                            SuperResolutionLogger.Error($"[强制超分异常] {entryName}: {ex.Message}", ex);
                            SuperResolutionStateManager.Current.SetActualStatus(imagePath, SuperResolutionActualStatus.Failed, "ForcedException");
                        }
                    }
                    else if (userPreference == SuperResolutionUserPreference.Auto && config.AutoApplyOnView)
                    {
                        // 🎯 自动模式 - 检查条件
                        if (_srHelper.ShouldProcess(bitmapSource, config, fileSize))
                        {
                            try
                            {
                                SuperResolutionLogger.Info($"[自动超分] {entryName} ({fileSize / 1024.0:F2} KB, {bitmapSource.PixelWidth}x{bitmapSource.PixelHeight})");
                                SuperResolutionStateManager.Current.SetActualStatus(imagePath, SuperResolutionActualStatus.Processing, "AutoByCondition");
                                
                                var srResult = await _srHelper.ProcessBitmapSourceAsync(bitmapSource, config, token);
                                if (srResult != null)
                                {
                                    SuperResolutionLogger.Info($"[自动超分成功] {entryName}: {bitmapSource.PixelWidth}x{bitmapSource.PixelHeight} → {srResult.PixelWidth}x{srResult.PixelHeight}");
                                    
                                    // 记录到缓存和数据库
                                    var cache = SuperResolutionImageCache.Current;
                                    cache.Update(imagePath, item =>
                                    {
                                        item.OriginalWidth = bitmapSource.PixelWidth;
                                        item.OriginalHeight = bitmapSource.PixelHeight;
                                        item.SuperResolutionWidth = srResult.PixelWidth;
                                        item.SuperResolutionHeight = srResult.PixelHeight;
                                        item.Status = SuperResolutionImageStatus.Completed;
                                    });
                                    
                                    var record = new SuperResolutionStateRecord
                                    {
                                        ImagePath = imagePath,
                                        UserPreference = SuperResolutionUserPreference.Auto,
                                        ActualStatus = SuperResolutionActualStatus.Completed,
                                        OriginalWidth = bitmapSource.PixelWidth,
                                        OriginalHeight = bitmapSource.PixelHeight,
                                        SuperResolutionWidth = srResult.PixelWidth,
                                        SuperResolutionHeight = srResult.PixelHeight
                                    };
                                    SuperResolutionStateManager.Current.SaveRecord(record, "AutoComplete");
                                    
                                    return srResult;
                                }
                                else
                                {
                                    SuperResolutionLogger.Warning($"[自动超分失败] {entryName}, 使用原图");
                                    SuperResolutionStateManager.Current.SetActualStatus(imagePath, SuperResolutionActualStatus.Failed, "AutoFailed");
                                }
                            }
                            catch (Exception ex)
                            {
                                SuperResolutionLogger.Error($"[自动超分异常] {entryName}: {ex.Message}", ex);
                                SuperResolutionStateManager.Current.SetActualStatus(imagePath, SuperResolutionActualStatus.Failed, "AutoException");
                            }
                        }
                        else
                        {
                            SuperResolutionLogger.Info($"[跳过超分] {entryName} (不符合自动条件)");
                        }
                    }
                    else
                    {
                        SuperResolutionLogger.Info($"[跳过超分] {entryName} (Auto模式但AutoApplyOnView未启用)");
                    }
                }
            }
            else
            {
                // 🔥 全局开关关闭或配置为空
                if (config == null)
                {
                    SuperResolutionLogger.Info($"[跳过超分] Config is null");
                }
                else
                {
                    SuperResolutionLogger.Info($"[跳过超分] 全局开关已关闭 (IsEnabled={config.IsEnabled})");
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
