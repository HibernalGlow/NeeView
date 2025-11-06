using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NeeView.SuperResolution
{
    /// <summary>
    /// 超分处理助手 - 处理 BitmapSource 的超分流程
    /// 优点:直接操作已解码的像素数据,避免 AVIF/JXL 二次解码损失
    /// </summary>
    public class SuperResolutionHelper
    {
        private readonly ISuperResolutionService _service;

        public SuperResolutionHelper()
        {
            _service = SuperResolutionService.Current;
        }

        /// <summary>
        /// 对 BitmapSource 进行超分处理 (推荐用于 AVIF/JXL 等已解码格式)
        /// </summary>
        /// <param name="source">已解码的 BitmapSource (来自 Susie/WPF 解码器)</param>
        /// <param name="config">超分配置</param>
        /// <param name="token">取消令牌</param>
        /// <returns>超分后的 BitmapSource</returns>
        public async Task<BitmapSource?> ProcessBitmapSourceAsync(
            BitmapSource source,
            SuperResolutionConfig config,
            CancellationToken token = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            SuperResolutionLogger.Info($"[BitmapSource 超分] 输入: {source.PixelWidth}x{source.PixelHeight}, {source.Format}");

            try
            {
                // 1. 将 BitmapSource 转换为 PNG 字节数组 (零质量损失)
                var inputPngBytes = ImageFormatConverter.ConvertBitmapSourceToPng(source);
                SuperResolutionLogger.Info($"BitmapSource 已转换为 PNG: {inputPngBytes.Length / 1024.0:F2} KB");

                // 2. 调用超分服务
                var result = await _service.ProcessAsync(inputPngBytes, config, token);

                if (!result.Success || result.OutputData == null || result.OutputData.Length == 0)
                {
                    SuperResolutionLogger.Error($"超分失败: {result.ErrorMessage}");
                    return null;
                }

                // 3. 将超分结果转换回 BitmapSource
                var outputSource = CreateBitmapSourceFromBytes(result.OutputData);
                SuperResolutionLogger.Info($"[BitmapSource 超分完成] 输出: {outputSource.PixelWidth}x{outputSource.PixelHeight}");

                return outputSource;
            }
            catch (Exception ex)
            {
                SuperResolutionLogger.Error($"BitmapSource 超分异常: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// 从字节数组创建 BitmapSource
        /// </summary>
        private BitmapSource CreateBitmapSourceFromBytes(byte[] imageData)
        {
            using (var stream = new System.IO.MemoryStream(imageData))
            {
                var decoder = BitmapDecoder.Create(
                    stream,
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.OnLoad
                );

                var frame = decoder.Frames[0];
                
                // 冻结以提高性能和跨线程使用
                var bitmap = new WriteableBitmap(frame);
                bitmap.Freeze();

                return bitmap;
            }
        }

        /// <summary>
        /// 检查是否应该对此图片进行超分
        /// </summary>
        public bool ShouldProcess(BitmapSource source, SuperResolutionConfig config)
        {
            if (source == null || !config.IsEnabled)
                return false;

            // 检查尺寸限制
            var maxDimension = Math.Max(source.PixelWidth, source.PixelHeight);
            if (config.AutoApplyOnView && maxDimension > config.AutoApplyMaxSize)
            {
                SuperResolutionLogger.Info($"图片尺寸 {maxDimension}px 超过自动超分限制 {config.AutoApplyMaxSize}px,跳过");
                return false;
            }

            return true;
        }
    }
}
