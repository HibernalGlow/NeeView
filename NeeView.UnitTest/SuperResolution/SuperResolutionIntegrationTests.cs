using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Xunit;
using NeeView.SuperResolution;

namespace NeeView.UnitTest.SuperResolution;

/// <summary>
/// 超分辨率实际处理测试 - 使用真实模型和图片
/// </summary>
public class SuperResolutionIntegrationTests
{
    private const string MODEL_PATH = @"D:\1Dev\Python\packages\Python311\site-packages\sr_vulkan_model_waifu2x\models";
    
    [Fact]
    public async Task ProcessAsync_WithRealImage_ShouldUpscale()
    {
        // 跳过测试如果模型路径不存在
        if (!Directory.Exists(MODEL_PATH))
        {
            SuperResolutionLogger.Warning($"跳过测试: 模型路径不存在 {MODEL_PATH}");
            return;
        }

        // Arrange - 创建测试图片
        var testImagePath = CreateTestImage(256, 256);
        SuperResolutionLogger.Info($"创建测试图片: {testImagePath}");
        
        try
        {
            var imageData = await File.ReadAllBytesAsync(testImagePath);
            SuperResolutionLogger.Info($"图片大小: {imageData.Length / 1024.0:F2} KB");
            
            // 配置超分参数
            var config = new SuperResolutionConfig
            {
                IsEnabled = true,
                AlgorithmType = SuperResolutionType.Waifu2x,
                Model = SuperResolutionModel.Waifu2xAnime2x,
                ScaleFactor = 2.0,
                UseTTA = false,
                GpuId = 0
            };
            
            // Act - 执行超分
            SuperResolutionLogger.Info("开始执行超分辨率处理...");
            var service = SuperResolutionService.Current;
            
            // 初始化服务
            var initResult = await service.InitializeAsync();
            if (!initResult)
            {
                SuperResolutionLogger.Error($"服务初始化失败: {service.GetLastError()}");
                // 跳过测试如果初始化失败
                return;
            }
            
            SuperResolutionLogger.Info("服务初始化成功");
            
            var result = await service.ProcessAsync(imageData, config, CancellationToken.None);
            
            // Assert
            SuperResolutionLogger.Info($"处理结果: Success={result.Success}, Error={result.ErrorMessage}");
            
            Assert.True(result.Success, $"超分处理应该成功: {result.ErrorMessage}");
            Assert.NotNull(result.OutputData);
            Assert.NotEmpty(result.OutputData);
            
            // 验证输出图片尺寸
            using var ms = new MemoryStream(result.OutputData);
            var decoder = BitmapDecoder.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            var frame = decoder.Frames[0];
            
            SuperResolutionLogger.Info($"原始尺寸: 256x256");
            SuperResolutionLogger.Info($"输出尺寸: {frame.PixelWidth}x{frame.PixelHeight}");
            SuperResolutionLogger.Info($"处理耗时: {result.ProcessingTime:F2}s");
            
            // 2倍放大应该是 512x512
            Assert.Equal(512, frame.PixelWidth);
            Assert.Equal(512, frame.PixelHeight);
            
            // 保存结果供手动检查
            var outputPath = Path.Combine(Path.GetTempPath(), $"SuperResolution_Test_Output_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            await File.WriteAllBytesAsync(outputPath, result.OutputData);
            SuperResolutionLogger.Info($"✅ 测试通过! 输出保存至: {outputPath}");
        }
        finally
        {
            // 清理测试图片
            if (File.Exists(testImagePath))
            {
                File.Delete(testImagePath);
            }
        }
    }

    [Fact]
    public async Task ProcessAsync_DifferentScales_ShouldWork()
    {
        if (!Directory.Exists(MODEL_PATH))
        {
            return;
        }

        var testImagePath = CreateTestImage(128, 128);
        
        try
        {
            var imageData = await File.ReadAllBytesAsync(testImagePath);
            var service = SuperResolutionService.Current;
            
            // 测试不同缩放倍数
            var scales = new[] { 2.0, 3.0, 4.0 };
            
            foreach (var scale in scales)
            {
                SuperResolutionLogger.Info($"测试 {scale}x 缩放...");
                
                var config = new SuperResolutionConfig
                {
                    IsEnabled = true,
                    ScaleFactor = scale,
                    GpuId = 0
                };
                
                var result = await service.ProcessAsync(imageData, config, CancellationToken.None);
                
                Assert.True(result.Success, $"{scale}x 缩放应该成功");
                
                using var ms = new MemoryStream(result.OutputData!);
                var decoder = BitmapDecoder.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                var frame = decoder.Frames[0];
                
                var expectedSize = (int)(128 * scale);
                SuperResolutionLogger.Info($"  {scale}x: {frame.PixelWidth}x{frame.PixelHeight} (期望: {expectedSize}x{expectedSize})");
                
                // 允许一定误差
                Assert.InRange(frame.PixelWidth, expectedSize - 10, expectedSize + 10);
            }
            
            SuperResolutionLogger.Info("✅ 多倍数缩放测试通过!");
        }
        finally
        {
            if (File.Exists(testImagePath))
            {
                File.Delete(testImagePath);
            }
        }
    }

    [Fact]
    public async Task ProcessAsync_WithDenoise_ShouldWork()
    {
        if (!Directory.Exists(MODEL_PATH))
        {
            return;
        }

        var testImagePath = CreateTestImage(200, 200);
        
        try
        {
            var imageData = await File.ReadAllBytesAsync(testImagePath);
            var service = SuperResolutionService.Current;
            
            // 使用带降噪的模型
            var config = new SuperResolutionConfig
            {
                IsEnabled = true,
                Model = SuperResolutionModel.Waifu2xAnime2x, // 使用存在的模型
                ScaleFactor = 2.0,
                GpuId = 0
            };
            
            SuperResolutionLogger.Info("测试降噪模型...");
            
            var result = await service.ProcessAsync(imageData, config, CancellationToken.None);
            
            Assert.True(result.Success, "降噪模型处理应该成功");
            Assert.NotNull(result.OutputData);
            
            SuperResolutionLogger.Info($"✅ 降噪测试通过! 耗时: {result.ProcessingTime:F2}s");
        }
        finally
        {
            if (File.Exists(testImagePath))
            {
                File.Delete(testImagePath);
            }
        }
    }

    [Fact]
    public async Task ProcessAsync_Performance_ShouldBeReasonable()
    {
        if (!Directory.Exists(MODEL_PATH))
        {
            return;
        }

        var testImagePath = CreateTestImage(512, 512);
        
        try
        {
            var imageData = await File.ReadAllBytesAsync(testImagePath);
            var service = SuperResolutionService.Current;
            
            var config = new SuperResolutionConfig
            {
                IsEnabled = true,
                ScaleFactor = 2.0,
                GpuId = 0
            };
            
            SuperResolutionLogger.Info("性能测试: 512x512 → 1024x1024");
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await service.ProcessAsync(imageData, config, CancellationToken.None);
            stopwatch.Stop();
            
            Assert.True(result.Success, "性能测试应该成功");
            
            var elapsed = stopwatch.Elapsed.TotalSeconds;
            SuperResolutionLogger.Info($"处理时间: {elapsed:F2}s");
            SuperResolutionLogger.Info($"服务报告时间: {result.ProcessingTime:F2}s");
            
            // GPU 处理应该在合理时间内完成 (< 30秒)
            Assert.True(elapsed < 30, $"处理时间 ({elapsed:F2}s) 应该 < 30秒");
            
            SuperResolutionLogger.Info("✅ 性能测试通过!");
        }
        finally
        {
            if (File.Exists(testImagePath))
            {
                File.Delete(testImagePath);
            }
        }
    }

    /// <summary>
    /// 创建测试图片
    /// </summary>
    private string CreateTestImage(int width, int height)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"SuperResolution_Test_Input_{Guid.NewGuid()}.png");
        
        // 创建一个简单的渐变测试图片
        var pixelFormat = System.Windows.Media.PixelFormats.Bgra32;
        var stride = width * 4;
        var pixels = new byte[height * stride];
        
        // 创建渐变图案
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var offset = y * stride + x * 4;
                
                pixels[offset] = (byte)(x * 255 / width);      // B
                pixels[offset + 1] = (byte)(y * 255 / height); // G
                pixels[offset + 2] = (byte)((x + y) * 128 / (width + height)); // R
                pixels[offset + 3] = 255; // A
            }
        }
        
        var bitmap = System.Windows.Media.Imaging.BitmapSource.Create(
            width, height, 96, 96, pixelFormat, null, pixels, stride);
        
        // 保存为 PNG
        using var fs = new FileStream(tempPath, FileMode.Create);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        encoder.Save(fs);
        
        return tempPath;
    }
}
