# NeeView 超分辨率功能集成指南

## 概述

本文档说明如何将超分辨率（Super Resolution）功能集成到NeeView图片浏览器中。该功能参考了picacg-qt的超分实现，提供了类似的功能和用户体验。

## 功能特性

- ✅ 多种超分算法支持（Waifu2x, RealESRGAN, Real-CUGAN）
- ✅ 多种模型选择（动漫风格、照片风格、不同倍数）
- ✅ 灵活的缩放设置（按倍数或指定尺寸）
- ✅ 高级选项（TTA模式、降噪等级、GPU选择）
- ✅ 批量处理功能
- ✅ 处理进度显示
- ✅ 结果缓存支持

## 已创建的文件

### 1. 核心类型定义
- `NeeView/SuperResolution/SuperResolutionType.cs` - 定义算法类型、模型类型、状态等枚举

### 2. 配置管理
- `NeeView/SuperResolution/SuperResolutionConfig.cs` - 超分辨率配置类

### 3. 服务接口和实现
- `NeeView/SuperResolution/ISuperResolutionService.cs` - 服务接口定义
- `NeeView/SuperResolution/SuperResolutionService.cs` - 服务实现（需要集成实际的超分库）

### 4. UI组件
- `NeeView/SidePanels/SuperResolution/SuperResolutionPanel.cs` - 面板定义
- `NeeView/SidePanels/SuperResolution/SuperResolutionView.xaml` - 视图UI
- `NeeView/SidePanels/SuperResolution/SuperResolutionView.xaml.cs` - 视图代码
- `NeeView/SidePanels/SuperResolution/SuperResolutionViewModel.cs` - 视图模型

### 5. 批量处理
- `NeeView/SuperResolution/BatchProcessViewModel.cs` - 批量处理视图模型

## 集成步骤

### 第1步：添加配置到Config类

在 `NeeView/Config/Config.cs` 中添加：

```csharp
using NeeView.SuperResolution;

public class Config : BindableBase
{
    // ... 现有代码 ...
    
    [DataMember]
    public SuperResolutionConfig SuperResolution { get; set; } = new SuperResolutionConfig();
}
```

### 第2步：注册面板

在 `NeeView/SidePanels/SidePanelFactory.cs` 中添加：

```csharp
public static IPanel? CreatePanel(string typeCode)
{
    return typeCode switch
    {
        // ... 现有面板 ...
        nameof(SuperResolutionPanel) => new SuperResolutionPanel(Config.Current.SuperResolution),
        _ => null,
    };
}
```

在 `NeeView/SidePanels/SidePanelFrame.cs` 中添加相关属性和方法：

```csharp
// 添加属性
public bool IsSuperResolutionPanelVisible
{
    get { return IsVisiblePanel(nameof(SuperResolutionPanel)); }
    set { SetVisiblePanel(nameof(SuperResolutionPanel), value); }
}

// 添加切换方法
public bool ToggleSuperResolutionPanel(bool byMenu)
{
    return ToggleVisiblePanel(nameof(SuperResolutionPanel), byMenu);
}
```

### 第3步：集成超分辨率库

`SuperResolutionService.cs` 中的 `ProcessAsync` 方法需要集成实际的超分库。推荐的库：

#### 选项1：ncnn-vulkan (推荐)
- 跨平台
- GPU加速
- 支持多种模型

```bash
# 需要添加NuGet包或DLL引用
# 参考: https://github.com/Tencent/ncnn
```

#### 选项2：onnxruntime
- 支持ONNX模型
- CPU/GPU加速

```bash
Install-Package Microsoft.ML.OnnxRuntime.Gpu
```

#### 集成示例代码：

```csharp
public async Task<SuperResolutionResult> ProcessAsync(
    byte[] inputData,
    SuperResolutionConfig config,
    CancellationToken cancellationToken = default)
{
    // 1. 加载图片
    using var image = Image.Load<Rgba32>(inputData);
    
    // 2. 根据配置选择模型
    var modelPath = GetModelPath(config.Model);
    
    // 3. 调用超分算法
    // TODO: 实际的ncnn或onnx处理代码
    
    // 4. 保存结果
    using var outputStream = new MemoryStream();
    await image.SaveAsync(outputStream, new PngEncoder(), cancellationToken);
    
    return new SuperResolutionResult
    {
        Success = true,
        OutputData = outputStream.ToArray(),
        ProcessingTime = stopwatch.Elapsed.TotalSeconds
    };
}
```

### 第4步：添加命令

创建 `NeeView/Command/SuperResolutionCommands.cs`：

```csharp
public class ToggleSuperResolutionPanelCommand : CommandElement
{
    public ToggleSuperResolutionPanelCommand()
    {
        // 配置命令
    }

    public override void Execute(object? parameter, CommandContext context)
    {
        SidePanelFrame.Current.ToggleSuperResolutionPanel(true);
    }
}

public class ProcessCurrentImageCommand : CommandElement
{
    public override async void Execute(object? parameter, CommandContext context)
    {
        // 获取当前图片并处理
        var currentImage = BookHub.Current.GetCurrentImageData();
        if (currentImage != null)
        {
            await SuperResolutionService.Current.ProcessAsync(
                currentImage, 
                Config.Current.SuperResolution);
        }
    }
}
```

### 第5步：添加菜单项

在主菜单配置中添加：

```xml
<MenuItem Header="View">
    <!-- ... 现有菜单项 ... -->
    <MenuItem Header="Super Resolution Panel" 
              Command="{Binding ToggleSuperResolutionPanelCommand}"/>
</MenuItem>

<MenuItem Header="Tools">
    <MenuItem Header="Super Resolution">
        <MenuItem Header="Process Current Image" 
                  Command="{Binding ProcessCurrentImageCommand}"/>
        <MenuItem Header="Batch Process..." 
                  Command="{Binding OpenBatchProcessCommand}"/>
    </MenuItem>
</MenuItem>
```

### 第6步：添加图标资源

在资源字典中添加超分辨率的图标：

```xml
<ResourceDictionary>
    <!-- AI/超分图标 -->
    <DrawingImage x:Key="pic_ai_24px">
        <!-- SVG路径数据 -->
    </DrawingImage>
</ResourceDictionary>
```

## 使用方法

### 单图处理

1. 打开NeeView并浏览图片
2. 从菜单选择 `View > Super Resolution Panel` 或使用快捷键
3. 在右侧面板中配置超分设置：
   - 选择算法和模型
   - 设置缩放倍数或目标尺寸
   - 调整高级选项（TTA、降噪等）
4. 点击 "Process Current Image" 按钮
5. 等待处理完成，查看结果

### 批量处理

1. 点击 "Batch Process..." 按钮
2. 添加文件或文件夹
3. 选择输出文件夹（可选）
4. 配置处理参数
5. 点击 "Start" 开始批量处理
6. 查看处理进度和结果

## 性能优化建议

1. **GPU加速**：确保正确配置GPU ID，使用GPU可大幅提升速度
2. **Tile大小**：显存不足时减小Tile Size，避免OOM错误
3. **并发数量**：根据硬件配置调整MaxConcurrentProcessing
4. **缓存**：启用缓存避免重复处理
5. **TTA模式**：仅在需要最高质量时使用，会显著增加处理时间

## 依赖库

需要添加以下依赖（选择其一）：

### 方案1：ncnn-vulkan
```xml
<!-- 需要手动引用native库 -->
<ItemGroup>
    <None Include="runtimes\**\*">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
</ItemGroup>
```

### 方案2：ONNX Runtime
```xml
<ItemGroup>
    <PackageReference Include="Microsoft.ML.OnnxRuntime.Gpu" Version="1.16.0" />
</ItemGroup>
```

## 模型文件

需要下载对应的模型文件：

- Waifu2x模型：https://github.com/nagadomi/waifu2x
- RealESRGAN模型：https://github.com/xinntao/Real-ESRGAN
- Real-CUGAN模型：https://github.com/bilibili/ailab

将模型文件放置在：`[AppData]/NeeView/Models/SuperResolution/`

## 已知限制

1. **实际超分算法未集成**：当前`SuperResolutionService`只是框架，需要集成实际的算法库
2. **模型文件管理**：需要实现模型下载和管理功能
3. **进度回调**：某些算法可能不支持实时进度报告
4. **内存管理**：处理大图时需要注意内存使用

## 后续工作

- [ ] 集成实际的超分算法库（ncnn或onnxruntime）
- [ ] 实现模型文件下载和管理
- [ ] 添加预览对比功能
- [ ] 优化大图处理的内存使用
- [ ] 添加更多预设配置
- [ ] 实现自动应用于浏览的图片
- [ ] 添加处理历史记录

## 参考

- picacg-qt超分实现：`ref/picacg-qt/src/view/tool/waifu2x_tool_view.py`
- NeeView ImageEffect系统：`NeeView/NeeView/Effects/ImageEffect.cs`

## 技术支持

如有问题，请参考：
- [ncnn文档](https://github.com/Tencent/ncnn/wiki)
- [ONNX Runtime文档](https://onnxruntime.ai/docs/)
- [Waifu2x项目](https://github.com/nagadomi/waifu2x)
