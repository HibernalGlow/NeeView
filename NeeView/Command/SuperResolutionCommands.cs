using NeeView.Properties;
using System;
using System.Globalization;

namespace NeeView.SuperResolution
{
    /// <summary>
    /// 处理当前图片命令
    /// </summary>
    public class ProcessCurrentImageWithSuperResolutionCommand : CommandElement
    {
        public ProcessCurrentImageWithSuperResolutionCommand()
        {
            this.Group = TextResources.GetString("CommandGroup.Effect");
            this.IsShowMessage = true;
        }

        public override bool CanExecute(object? sender, CommandContext e)
        {
            // 检查是否有当前图片和服务是否可用
            return ImageDataHelper.GetCurrentImageInfo() != null && 
                   SuperResolutionService.Current.IsAvailable;
        }

        public override async void Execute(object? sender, CommandContext e)
        {
            try
            {
                // 获取当前图片信息
                var imageInfo = ImageDataHelper.GetCurrentImageInfo();
                if (imageInfo == null)
                {
                    InfoMessage.Current.SetMessage(InfoMessageType.Notify, 
                        "No image to process");
                    return;
                }

                var (fileName, width, height) = imageInfo.Value;

                // 显示处理中提示
                InfoMessage.Current.SetMessage(InfoMessageType.Notify, 
                    $"Processing {fileName} ({width}x{height}) with super resolution...");

                // 获取图片数据
                var imageData = await ImageDataHelper.GetCurrentImageDataAsync();
                if (imageData == null || imageData.Length == 0)
                {
                    InfoMessage.Current.SetMessage(InfoMessageType.Notify, 
                        "Failed to get image data");
                    return;
                }

                // 处理图片
                var config = Config.Current.SuperResolution;
                var result = await SuperResolutionService.Current.ProcessAsync(
                    imageData, 
                    config);

                if (result.Success && result.OutputData != null)
                {
                    // 显示处理后的图片
                    bool shown = await ImageDataHelper.ShowProcessedImageAsync(result.OutputData, fileName);
                    
                    if (shown)
                    {
                        var message = config.ScaleMode == ScaleMode.ScaleFactor
                            ? $"Super resolution completed: {fileName} scaled by {config.ScaleFactor}x"
                            : $"Super resolution completed: {fileName} resized to {config.TargetWidth}x{config.TargetHeight}";
                        
                        InfoMessage.Current.SetMessage(InfoMessageType.Notify, message);
                    }
                    else
                    {
                        InfoMessage.Current.SetMessage(InfoMessageType.Notify, 
                            "Processing completed but failed to display result");
                    }
                }
                else
                {
                    InfoMessage.Current.SetMessage(InfoMessageType.Notify, 
                        $"Processing failed: {result.ErrorMessage ?? "Unknown error"}");
                }
            }
            catch (Exception ex)
            {
                InfoMessage.Current.SetMessage(InfoMessageType.Notify, 
                    $"Error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 打开批量处理窗口命令
    /// </summary>
    public class OpenBatchSuperResolutionCommand : CommandElement
    {
        public OpenBatchSuperResolutionCommand()
        {
            this.Group = TextResources.GetString("CommandGroup.Effect");
            this.IsShowMessage = false;
        }

        public override bool CanExecute(object? sender, CommandContext e)
        {
            return SuperResolutionService.Current.IsAvailable;
        }

        public override void Execute(object? sender, CommandContext e)
        {
            // TODO: 创建并显示批量处理窗口
            // var window = new BatchProcessWindow(Config.Current.SuperResolution);
            // window.Show();
            
            InfoMessage.Current.SetMessage(InfoMessageType.Notify, 
                "Batch processing window (to be implemented)");
        }
    }
}
