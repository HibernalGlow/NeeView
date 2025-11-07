# AVIF/JXL 超分辨率测试工具

这是一个独立的测试程序，用于验证 NeeView 的 AVIF/JXL 解码 + 超分辨率处理流程是否正常工作。

## 功能

✅ 使用 WPF BitmapDecoder 解码 AVIF/JXL/WebP 等格式  
✅ 转换为 PNG (无损)  
✅ 调用 sr_vulkan 进行超分辨率处理  
✅ 验证输出尺寸  
✅ 保存解码和超分后的结果  

## 前置要求

1. **Python 环境**
   ```powershell
   python --version  # 应该是 Python 3.11+
   ```

2. **安装 sr_vulkan**
   ```powershell
   pip install sr-vulkan sr-vulkan-model-waifu2x
   ```

3. **安装 Pillow (可选，用于创建测试图片)**
   ```powershell
   pip install Pillow pillow-avif-plugin
   ```

## 使用方法

### 方法 1: 使用现有的 AVIF/JXL 图片

```powershell
# 直接运行 (会提示输入路径)
.\RunAvifSRTest.ps1

# 或指定图片路径
.\RunAvifSRTest.ps1 -ImagePath "your_image.avif"
```

### 方法 2: 创建测试图片

```powershell
# 创建一个 512x512 的测试 AVIF 图片
.\CreateTestAvif.ps1

# 然后运行测试
.\RunAvifSRTest.ps1 -ImagePath "test.avif"
```

### 方法 3: 拖放文件

直接将 AVIF/JXL 图片拖放到 `AvifSRTest.exe` 上。

## 输出文件

测试程序会在输入图片的同一目录生成两个文件：

1. **`<原文件名>_decoded.png`** - WPF 解码后的 PNG (验证解码是否正确)
2. **`<原文件名>_SR_2x.png`** - 超分辨率处理后的结果 (2x 放大)

## 测试流程

程序会执行以下步骤：

1. 📖 读取图片文件
2. 🔍 检测图片格式 (AVIF/JXL/PNG/etc.)
3. 🖼️  使用 WPF BitmapDecoder 解码
4. 🔄 转换为 PNG 格式 (无损)
5. 💾 保存解码后的 PNG
6. ⚙️  初始化超分辨率服务
7. 🚀 执行超分处理 (2x 放大)
8. 💾 保存超分结果
9. ✔️  验证输出尺寸

## 示例输出

```
========================================
AVIF/JXL 超分辨率测试
========================================

📖 步骤 1: 读取图片文件...
   文件大小: 45.23 KB
   检测格式: AVIF

🖼️  步骤 2: 使用 WPF 解码器解码图片...
   解码成功: 512x512
   像素格式: Bgra32
   DPI: 96.0 x 96.0

🔄 步骤 3: 转换为 PNG 格式 (无损)...
   PNG 大小: 123.45 KB

   💾 解码后的 PNG 已保存: test_decoded.png

⚙️  步骤 4: 初始化超分辨率服务...
   ✅ 服务初始化成功

🚀 步骤 5: 执行超分辨率处理...
   算法: Waifu2x
   模型: Waifu2xAnime2x
   缩放: 2x
   降噪: 关闭

   ⏳ 处理中，请稍候...

📊 步骤 6: 处理结果...
   成功: ✅
   耗时: 3.45 秒
   输出大小: 456.78 KB

   💾 超分结果已保存: test_SR_2x.png

✔️  步骤 7: 验证输出尺寸...
   原始尺寸: 512x512
   输出尺寸: 1024x1024
   ✅ 尺寸验证通过! (1024x1024)

========================================
📁 生成的文件:
   1. 解码后的 PNG: test_decoded.png
   2. 超分后的结果: test_SR_2x.png
========================================

✅ 测试完成!
```

## 常见问题

### Q: 提示 "服务初始化失败"

**A:** 请确保已安装 Python 和 sr_vulkan:
```powershell
pip install sr-vulkan sr-vulkan-model-waifu2x
```

### Q: 解码失败

**A:** 确保 Windows 已安装 AVIF/JXL 编解码器，或者使用 Susie 插件。

### Q: 处理很慢

**A:** 这是正常的，超分辨率处理需要时间。可以在配置中调整 `TileSize` 参数。

### Q: 想测试不同的缩放倍数或模型

**A:** 修改 `Program.cs` 中的配置:
```csharp
var config = new SuperResolutionConfig
{
    ScaleFactor = 4.0,  // 改为 4x
    Model = SuperResolutionModel.Waifu2xAnime4x,
    // ...
};
```

## 调试

如果遇到问题，可以查看详细日志：

```
C:\Users\<用户名>\AppData\Local\NeeView\Logs\SuperResolution_*.log
```

## 手动构建

```powershell
dotnet build AvifSRTest\AvifSRTest.csproj -c Debug
```

可执行文件位置：
```
AvifSRTest\bin\Debug\net9.0-windows\AvifSRTest.exe
```

## 许可

本测试工具与 NeeView 共享相同的许可证。
