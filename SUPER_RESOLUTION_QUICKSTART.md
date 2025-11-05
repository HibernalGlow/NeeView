# NeeView 超分辨率功能 - 快速开始

## 🚀 5分钟快速集成

### 第1步：添加配置引用 (30秒)

在 `NeeView/Config/Config.cs` 中添加：

```csharp
// 在文件顶部添加using
using NeeView.SuperResolution;

// 在Config类中添加属性
[DataMember]
public SuperResolutionConfig SuperResolution { get; set; } = new SuperResolutionConfig();
```

### 第2步：注册面板 (1分钟)

**文件1：** `NeeView/SidePanels/SidePanelFactory.cs`

```csharp
public static IPanel? CreatePanel(string typeCode)
{
    return typeCode switch
    {
        // ... 现有代码 ...
        nameof(SuperResolutionPanel) => new SuperResolutionPanel(Config.Current.SuperResolution),
        _ => null,
    };
}
```

**文件2：** `NeeView/SidePanels/SidePanelFrame.cs`

```csharp
// 添加属性
public bool IsSuperResolutionPanelVisible
{
    get => IsVisiblePanel(nameof(SuperResolutionPanel));
    set => SetVisiblePanel(nameof(SuperResolutionPanel), value);
}

// 添加方法
public bool ToggleSuperResolutionPanel(bool byMenu)
{
    return ToggleVisiblePanel(nameof(SuperResolutionPanel), byMenu);
}
```

### 第3步：注册命令 (1分钟)

在你的命令注册代码中添加：

```csharp
// 注册切换面板命令
CommandTable.Add(new ToggleSuperResolutionPanelCommand());

// 注册处理图片命令
CommandTable.Add(new ProcessCurrentImageWithSuperResolutionCommand());

// 注册批量处理命令
CommandTable.Add(new OpenBatchSuperResolutionCommand());
```

### 第4步：添加菜单项 (1分钟)

在主菜单XAML中添加：

```xml
<MenuItem Header="查看(_V)">
    <!-- 现有菜单项 -->
    <Separator/>
    <MenuItem Header="超分辨率面板" 
              Command="{Binding ToggleSuperResolutionPanelCommand}"/>
</MenuItem>

<MenuItem Header="工具(_T)">
    <MenuItem Header="超分辨率">
        <MenuItem Header="处理当前图片" 
                  Command="{Binding ProcessCurrentImageCommand}"/>
        <MenuItem Header="批量处理..." 
                  Command="{Binding OpenBatchProcessCommand}"/>
    </MenuItem>
</MenuItem>
```

### 第5步：编译测试 (30秒)

```bash
# 编译项目
dotnet build

# 或在Visual Studio中按F5
```

## ✅ 验证集成

运行NeeView后，应该能看到：

1. ✅ 菜单中出现"超分辨率面板"选项
2. ✅ 点击后右侧显示超分辨率面板
3. ✅ 面板中显示"Service initialization failed"（正常，因为还未集成算法库）

## ⚠️ 下一步：集成算法库

**重要**：当前只是UI框架，要让功能真正工作，需要：

### 方案A：使用ncnn-vulkan (推荐)

```csharp
// 在SuperResolutionService.cs的ProcessAsync方法中：

public async Task<SuperResolutionResult> ProcessAsync(...)
{
    // 1. 安装ncnn-vulkan C#绑定
    // 2. 加载模型
    var ncnn = new NcnnNet();
    ncnn.LoadModel(modelPath);
    
    // 3. 处理图片
    var outputImage = ncnn.Process(inputData, scale);
    
    // 4. 返回结果
    return new SuperResolutionResult
    {
        Success = true,
        OutputData = outputImage,
        ProcessingTime = stopwatch.Elapsed.TotalSeconds
    };
}
```

### 方案B：使用ONNX Runtime

```bash
# 安装NuGet包
Install-Package Microsoft.ML.OnnxRuntime.Gpu
Install-Package SixLabors.ImageSharp
```

```csharp
using Microsoft.ML.OnnxRuntime;
using SixLabors.ImageSharp;

public async Task<SuperResolutionResult> ProcessAsync(...)
{
    // 1. 加载ONNX模型
    var session = new InferenceSession(modelPath);
    
    // 2. 预处理图片
    var tensor = PreprocessImage(inputData);
    
    // 3. 推理
    var inputs = new List<NamedOnnxValue> { 
        NamedOnnxValue.CreateFromTensor("input", tensor) 
    };
    var outputs = session.Run(inputs);
    
    // 4. 后处理
    var outputImage = PostprocessImage(outputs.First().AsEnumerable<float>());
    
    return new SuperResolutionResult { ... };
}
```

## 📦 需要的模型文件

下载并放置到 `%AppData%/NeeView/Models/SuperResolution/`:

```
Models/
└── SuperResolution/
    ├── waifu2x-anime-2x.bin
    ├── waifu2x-anime-2x.param
    ├── waifu2x-anime-4x.bin
    ├── waifu2x-anime-4x.param
    ├── realesrgan-x4plus-anime.bin
    └── realesrgan-x4plus-anime.param
```

**下载地址**：
- Waifu2x: https://github.com/nihui/waifu2x-ncnn-vulkan/releases
- RealESRGAN: https://github.com/xinntao/Real-ESRGAN-ncnn-vulkan/releases

## 🔍 调试技巧

### 检查服务初始化

```csharp
// 在App.xaml.cs的OnStartup中添加：
await SuperResolutionService.Current.InitializeAsync();
if (SuperResolutionService.Current.IsAvailable)
{
    Debug.WriteLine("Super Resolution Service Ready!");
}
else
{
    Debug.WriteLine($"Error: {SuperResolutionService.Current.GetLastError()}");
}
```

### 测试简单处理

```csharp
// 在任何地方测试：
var testData = File.ReadAllBytes("test.png");
var config = new SuperResolutionConfig { ScaleFactor = 2.0 };
var result = await SuperResolutionService.Current.ProcessAsync(testData, config);
if (result.Success)
{
    File.WriteAllBytes("output.png", result.OutputData);
}
```

## 📊 性能调优

```csharp
// 在配置中调整：
config.GpuId = 0;              // 使用第一个GPU
config.TileSize = 200;         // 显存不足时减小
config.MaxConcurrentProcessing = 2;  // 并发任务数
config.UseTTA = false;         // 提高速度（降低质量）
```

## 🐛 常见问题

### 问题1：面板不显示
**解决**：检查是否正确注册到SidePanelFactory

### 问题2：命令找不到
**解决**：确保在CommandTable中注册了命令

### 问题3：Service初始化失败
**解决**：
1. 检查模型文件是否存在
2. 检查GPU驱动是否安装
3. 查看GetLastError()获取详细错误

### 问题4：处理很慢
**解决**：
1. 使用GPU（设置GpuId = 0）
2. 减小TileSize
3. 关闭TTA模式

## 📚 完整文档

- **详细集成指南**：`SUPER_RESOLUTION_INTEGRATION_GUIDE.md`
- **功能总结**：`SUPER_RESOLUTION_SUMMARY.md`
- **参考实现**：`ref/picacg-qt/src/view/tool/waifu2x_tool_view.py`

## 🎯 检查清单

集成完成后，确认以下项目：

- [ ] Config中添加了SuperResolution属性
- [ ] SidePanelFactory中注册了SuperResolutionPanel
- [ ] SidePanelFrame中添加了访问方法
- [ ] 命令已注册到CommandTable
- [ ] 菜单中添加了对应的MenuItem
- [ ] 项目可以成功编译
- [ ] 运行后面板可以显示
- [ ] 集成了实际的超分算法库（重要！）
- [ ] 下载了模型文件
- [ ] 测试了单图处理
- [ ] 测试了批量处理

## 💡 提示

- 先让界面运行起来，然后再集成算法库
- 从简单的测试开始，逐步完善功能
- 参考picacg-qt的实现了解算法调用方式
- 使用异步方式避免UI卡顿

## 🆘 需要帮助？

如果遇到问题：

1. 检查编译错误
2. 查看输出窗口的调试信息
3. 参考已创建文件中的注释
4. 查看完整集成指南

---

**预计总时间**：5分钟界面集成 + 2-4小时算法库集成
