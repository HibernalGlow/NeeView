using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NeeView.SuperResolution;

namespace AvifSRTest
{
    /// <summary>
    /// AVIF/JXL 超分辨率独立测试程序
    /// </summary>
    class Program
    {
        [STAThread]
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            Console.WriteLine("========================================");
            Console.WriteLine("AVIF/JXL 超分辨率测试");
            Console.WriteLine("========================================");
            Console.WriteLine();

            if (args.Length == 0)
            {
                Console.WriteLine("用法: AvifSRTest.exe <图片路径>");
                Console.WriteLine("示例: AvifSRTest.exe test.avif");
                Console.WriteLine();
                Console.WriteLine("或者拖放图片文件到此程序上");
                Console.WriteLine();
                Console.Write("请输入图片路径: ");
                var input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                {
                    return;
                }
                args = new[] { input.Trim('"') };
            }

            string imagePath = args[0];
            if (!File.Exists(imagePath))
            {
                Console.WriteLine($"❌ 错误: 文件不存在 - {imagePath}");
                Console.WriteLine();
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"📁 输入文件: {imagePath}");
            Console.WriteLine();

            try
            {
                await RunTest(imagePath);
                
                Console.WriteLine();
                Console.WriteLine("✅ 测试完成!");
                Console.WriteLine();
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine($"❌ 错误: {ex.Message}");
                Console.WriteLine();
                Console.WriteLine($"详细信息: {ex}");
                Console.WriteLine();
                Console.WriteLine("按任意键退出...");
                Console.ReadKey();
            }
        }

        static async Task RunTest(string imagePath)
        {
            // 1. 读取图片文件
            Console.WriteLine("📖 步骤 1: 读取图片文件...");
            byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
            Console.WriteLine($"   文件大小: {imageBytes.Length / 1024.0:F2} KB");

            // 2. 检测格式
            var format = ImageFormatConverter.DetectFormat(imageBytes);
            Console.WriteLine($"   检测格式: {format}");
            Console.WriteLine();

            // 3. 使用 WPF 解码器解码
            Console.WriteLine("🖼️  步骤 2: 使用 WPF 解码器解码图片...");
            BitmapSource bitmapSource;

            using (var stream = new MemoryStream(imageBytes))
            {
                var decoder = BitmapDecoder.Create(
                    stream,
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.OnLoad
                );

                if (decoder.Frames.Count == 0)
                {
                    throw new InvalidOperationException("解码器未返回任何帧");
                }

                var frame = decoder.Frames[0];
                Console.WriteLine($"   解码成功: {frame.PixelWidth}x{frame.PixelHeight}");
                Console.WriteLine($"   像素格式: {frame.Format}");
                Console.WriteLine($"   DPI: {frame.DpiX:F1} x {frame.DpiY:F1}");

                // 冻结以便后续使用
                var bitmap = new WriteableBitmap(frame);
                bitmap.Freeze();
                bitmapSource = bitmap;
            }
            Console.WriteLine();

            // 4. 转换为 PNG (无损)
            Console.WriteLine("🔄 步骤 3: 转换为 PNG 格式 (无损)...");
            byte[] pngBytes = ImageFormatConverter.ConvertBitmapSourceToPng(bitmapSource);
            Console.WriteLine($"   PNG 大小: {pngBytes.Length / 1024.0:F2} KB");
            Console.WriteLine();

            // 5. 保存解码后的 PNG
            string decodedPngPath = Path.Combine(
                Path.GetDirectoryName(imagePath) ?? ".",
                Path.GetFileNameWithoutExtension(imagePath) + "_decoded.png"
            );
            await File.WriteAllBytesAsync(decodedPngPath, pngBytes);
            Console.WriteLine($"   💾 解码后的 PNG 已保存: {decodedPngPath}");
            Console.WriteLine();

            // 6. 初始化超分服务
            Console.WriteLine("⚙️  步骤 4: 初始化超分辨率服务...");
            var service = SuperResolutionService.Current;
            bool initialized = await service.InitializeAsync(0);

            if (!initialized)
            {
                Console.WriteLine($"   ❌ 服务初始化失败: {service.GetLastError()}");
                Console.WriteLine();
                Console.WriteLine("💡 提示: 请确保已安装 Python 和 sr_vulkan:");
                Console.WriteLine("   pip install sr-vulkan sr-vulkan-model-waifu2x");
                throw new InvalidOperationException("超分辨率服务初始化失败");
            }
            Console.WriteLine("   ✅ 服务初始化成功");
            Console.WriteLine();

            // 7. 配置超分参数
            var config = new SuperResolutionConfig
            {
                IsEnabled = true,
                AlgorithmType = SuperResolutionType.Waifu2x,
                Model = SuperResolutionModel.Waifu2xAnime2x,
                ScaleFactor = 2.0,
                NoiseLevel = -1,
                GpuId = 0,
                TileSize = 0
            };

            Console.WriteLine("🚀 步骤 5: 执行超分辨率处理...");
            Console.WriteLine($"   算法: {config.AlgorithmType}");
            Console.WriteLine($"   模型: {config.Model}");
            Console.WriteLine($"   缩放: {config.ScaleFactor}x");
            Console.WriteLine($"   降噪: {(config.NoiseLevel >= 0 ? $"等级 {config.NoiseLevel}" : "关闭")}");
            Console.WriteLine();
            Console.WriteLine("   ⏳ 处理中，请稍候...");

            // 8. 执行超分处理
            var startTime = DateTime.Now;
            var result = await service.ProcessAsync(pngBytes, config, CancellationToken.None);
            var elapsed = (DateTime.Now - startTime).TotalSeconds;

            Console.WriteLine();
            Console.WriteLine("📊 步骤 6: 处理结果...");
            Console.WriteLine($"   成功: {(result.Success ? "✅" : "❌")}");
            Console.WriteLine($"   耗时: {elapsed:F2} 秒");

            if (!result.Success)
            {
                Console.WriteLine($"   ❌ 错误: {result.ErrorMessage}");
                throw new InvalidOperationException($"超分处理失败: {result.ErrorMessage}");
            }

            if (result.OutputData == null || result.OutputData.Length == 0)
            {
                throw new InvalidOperationException("输出数据为空");
            }

            Console.WriteLine($"   输出大小: {result.OutputData.Length / 1024.0:F2} KB");
            Console.WriteLine();

            // 9. 保存超分后的结果
            string outputPath = Path.Combine(
                Path.GetDirectoryName(imagePath) ?? ".",
                Path.GetFileNameWithoutExtension(imagePath) + "_SR_2x.png"
            );
            await File.WriteAllBytesAsync(outputPath, result.OutputData);
            Console.WriteLine($"   💾 超分结果已保存: {outputPath}");
            Console.WriteLine();

            // 10. 验证输出尺寸
            Console.WriteLine("✔️  步骤 7: 验证输出尺寸...");
            using (var stream = new MemoryStream(result.OutputData))
            {
                var decoder = BitmapDecoder.Create(
                    stream,
                    BitmapCreateOptions.None,
                    BitmapCacheOption.OnLoad
                );

                if (decoder.Frames.Count > 0)
                {
                    var frame = decoder.Frames[0];
                    Console.WriteLine($"   原始尺寸: {bitmapSource.PixelWidth}x{bitmapSource.PixelHeight}");
                    Console.WriteLine($"   输出尺寸: {frame.PixelWidth}x{frame.PixelHeight}");

                    int expectedWidth = (int)(bitmapSource.PixelWidth * config.ScaleFactor);
                    int expectedHeight = (int)(bitmapSource.PixelHeight * config.ScaleFactor);

                    if (frame.PixelWidth == expectedWidth && frame.PixelHeight == expectedHeight)
                    {
                        Console.WriteLine($"   ✅ 尺寸验证通过! ({expectedWidth}x{expectedHeight})");
                    }
                    else
                    {
                        Console.WriteLine($"   ⚠️  尺寸不匹配! 期望: {expectedWidth}x{expectedHeight}");
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("========================================");
            Console.WriteLine("📁 生成的文件:");
            Console.WriteLine($"   1. 解码后的 PNG: {Path.GetFileName(decodedPngPath)}");
            Console.WriteLine($"   2. 超分后的结果: {Path.GetFileName(outputPath)}");
            Console.WriteLine("========================================");
        }
    }
}
