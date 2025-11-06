using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit;
using NeeView.SuperResolution;

namespace NeeView.UnitTest.SuperResolution;

/// <summary>
/// 图像格式转换器测试
/// </summary>
public class ImageFormatConverterTests
{
    [Fact]
    public void ConvertBitmapSourceToPng_WithValidBitmap_ShouldProducePngData()
    {
        // Arrange
        var bitmap = CreateTestBitmap(100, 100);
        
        // Act
        var pngData = ImageFormatConverter.ConvertBitmapSourceToPng(bitmap);
        
        // Assert
        Assert.NotNull(pngData);
        Assert.NotEmpty(pngData);
        
        // 验证是 PNG 格式(PNG 文件头: 89 50 4E 47)
        Assert.Equal(0x89, pngData[0]);
        Assert.Equal(0x50, pngData[1]);
        Assert.Equal(0x4E, pngData[2]);
        Assert.Equal(0x47, pngData[3]);
    }

    [Fact]
    public void ConvertBitmapSourceToPng_WithDifferentPixelFormats_ShouldWork()
    {
        // Arrange
        var formats = new[]
        {
            PixelFormats.Bgr24,
            PixelFormats.Bgr32,
            PixelFormats.Bgra32,
            PixelFormats.Pbgra32
        };
        
        foreach (var format in formats)
        {
            var bitmap = CreateTestBitmap(50, 50, format);
            
            // Act
            var pngData = ImageFormatConverter.ConvertBitmapSourceToPng(bitmap);
            
            // Assert
            Assert.NotNull(pngData);
            Assert.NotEmpty(pngData);
            
            SuperResolutionLogger.Info($"格式 {format} 转换成功,大小: {pngData.Length} 字节");
        }
    }

    [Fact]
    public void ConvertBitmapSourceToPng_WithLargeBitmap_ShouldNotThrow()
    {
        // Arrange
        var bitmap = CreateTestBitmap(1024, 1024);
        
        // Act
        var pngData = ImageFormatConverter.ConvertBitmapSourceToPng(bitmap);
        
        // Assert
        Assert.NotNull(pngData);
        Assert.NotEmpty(pngData);
        Assert.True(pngData.Length > 1024, "1024x1024 PNG 应该大于 1KB");
    }

    [Fact]
    public void ConvertBitmapSourceToPng_OutputSize_ShouldBeReasonable()
    {
        // Arrange
        var bitmap = CreateTestBitmap(800, 600);
        
        // Act
        var pngData = ImageFormatConverter.ConvertBitmapSourceToPng(bitmap);
        
        // Assert
        // PNG 压缩后的大小应该小于原始像素数据
        // 800x600x4 = 1,920,000 字节 (RGBA)
        var maxExpectedSize = 800 * 600 * 4;
        Assert.True(pngData.Length < maxExpectedSize, 
            $"PNG 大小 ({pngData.Length}) 应该小于原始数据 ({maxExpectedSize})");
        
        SuperResolutionLogger.Info($"800x600 转换为 PNG: {pngData.Length:N0} 字节");
    }

    [Fact]
    public void ConvertBitmapSourceToPng_RoundTrip_ShouldPreserveSize()
    {
        // Arrange
        var originalWidth = 400;
        var originalHeight = 300;
        var bitmap = CreateTestBitmap(originalWidth, originalHeight);
        
        // Act
        var pngData = ImageFormatConverter.ConvertBitmapSourceToPng(bitmap);
        
        // 重新加载 PNG
        using var ms = new MemoryStream(pngData);
        var decoder = new PngBitmapDecoder(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        var frame = decoder.Frames[0];
        
        // Assert
        Assert.Equal(originalWidth, frame.PixelWidth);
        Assert.Equal(originalHeight, frame.PixelHeight);
    }

    // 辅助方法
    private BitmapSource CreateTestBitmap(int width, int height, PixelFormat? format = null)
    {
        var pixelFormat = format ?? PixelFormats.Bgra32;
        var stride = width * ((pixelFormat.BitsPerPixel + 7) / 8);
        var pixels = new byte[height * stride];
        
        // 填充测试图案(渐变色)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var offset = y * stride + x * 4;
                if (offset + 3 < pixels.Length)
                {
                    pixels[offset] = (byte)(x * 255 / width);     // B
                    pixels[offset + 1] = (byte)(y * 255 / height); // G
                    pixels[offset + 2] = 128;                      // R
                    pixels[offset + 3] = 255;                      // A
                }
            }
        }
        
        return BitmapSource.Create(width, height, 96, 96, pixelFormat, null, pixels, stride);
    }
}
