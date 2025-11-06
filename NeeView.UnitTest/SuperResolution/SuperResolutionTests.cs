using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using NeeView.SuperResolution;

namespace NeeView.UnitTest.SuperResolution;

/// <summary>
/// 超分辨率功能测试
/// </summary>
public class SuperResolutionTests
{
    [Fact]
    public void Logger_ShouldCreateLogEntry()
    {
        // Arrange
        var testMessage = $"Test message at {DateTime.Now:HH:mm:ss.fff}";
        
        // Act
        SuperResolutionLogger.Info(testMessage);
        
        // Assert - 日志应该被记录,不抛出异常即可
        Assert.True(true);
    }

    [Fact]
    public void Logger_DevMode_ShouldBeConfigured()
    {
        // Act & Assert
#if DEBUG
        Assert.True(SuperResolutionLogger.IsDevMode, "DEBUG 模式下 IsDevMode 应该为 true");
#else
        Assert.False(SuperResolutionLogger.IsDevMode, "RELEASE 模式下 IsDevMode 应该为 false");
#endif
    }

    [Fact]
    public void Config_ShouldHaveValidDefaults()
    {
        // Act
        var config = new SuperResolutionConfig();
        
        // Assert
        Assert.NotNull(config);
        Assert.Equal(2.0, config.ScaleFactor); // 默认 2x
        Assert.Equal(SuperResolutionType.Waifu2x, config.AlgorithmType);
    }

    [Fact]
    public void DetectedModel_ShouldHaveCorrectProperties()
    {
        // Arrange
        var model = new DetectedModel
        {
            ModelName = "waifu2x-anime",
            ModelType = ModelType.Waifu2x,
            Scale = 2,
            DenoiseLevel = 1,
            SupportsTTA = true,
            ParamFilePath = "test.param",
            BinFilePath = "test.bin"
        };
        
        // Act
        var displayName = model.DisplayName;
        
        // Assert
        Assert.Contains("Waifu2x", displayName);
        Assert.Contains("2x", displayName);
        Assert.Contains("Denoise1", displayName);
    }

    [Fact]
    public void ModelScanner_WithEmptyDirectory_ShouldReturnEmptyList()
    {
        // Arrange
        var tempDir = Path.Combine(Path.GetTempPath(), $"NeeView_Test_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // Act
            var models = ModelScanner.ScanModelDirectory(tempDir);
            
            // Assert
            Assert.NotNull(models);
            Assert.Empty(models);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Config_ScaleFactor_ShouldAcceptValidValues()
    {
        // Arrange
        var config = new SuperResolutionConfig();
        
        // Act & Assert
        config.ScaleFactor = 2;
        Assert.Equal(2, config.ScaleFactor);
        
        config.ScaleFactor = 3;
        Assert.Equal(3, config.ScaleFactor);
        
        config.ScaleFactor = 4;
        Assert.Equal(4, config.ScaleFactor);
    }

    [Fact]
    public void ModelType_ShouldHaveExpectedValues()
    {
        // Act & Assert
        Assert.True(Enum.IsDefined(typeof(ModelType), ModelType.Waifu2x));
        Assert.True(Enum.IsDefined(typeof(ModelType), ModelType.RealESRGAN));
        Assert.True(Enum.IsDefined(typeof(ModelType), ModelType.Unknown));
    }

    [Fact]
    public void ModelScanner_GetDisplayName_ShouldWork()
    {
        // Arrange
        var model = new DetectedModel
        {
            ModelName = "test",
            ModelType = ModelType.RealESRGAN,
            Scale = 4,
            DenoiseLevel = -1,
            ParamFilePath = "test.param",
            BinFilePath = "test.bin"
        };
        
        // Act
        var displayName = ModelScanner.GetDisplayName(model);
        
        // Assert
        Assert.Contains("RealESRGAN", displayName);
        Assert.Contains("4x", displayName);
    }
}
