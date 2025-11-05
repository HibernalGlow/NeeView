# NeeView 超分辨率功能 - 快速验证测试

## 🎯 立即可以测试的内容

### 准备工作
```powershell
# 1. 进入项目目录
cd d:\1VSCODE\Projects\ImageAll\NeeWaifu\NeeView

# 2. 编译项目
dotnet build

# 如果编译失败，检查错误信息并修复
```

### 测试清单

#### ✅ 第一阶段：编译测试 (5分钟)
- [ ] 项目可以成功编译
- [ ] 没有编译错误
- [ ] 可能有一些警告（可忽略）

**预期结果**:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

#### ✅ 第二阶段：UI测试 (10分钟)

**步骤**:
1. 启动 NeeView.exe
2. 打开一张测试图片
3. 按快捷键 `S, R` 或通过菜单打开超分辨率面板

**检查项**:
- [ ] 面板可以打开和关闭
- [ ] 所有控件正常显示
- [ ] 算法下拉框有三个选项 (Waifu2x, RealESRGAN, RealCUGAN)
- [ ] 模型下拉框根据算法变化
- [ ] 缩放模式可以切换 (By Factor / Target Size)
- [ ] 所有滑块可以拖动
- [ ] 设置会被保存和恢复

**预期界面**:
```
┌─ Super Resolution ─────────────────┐
│ Algorithm:  [Waifu2x           ▼] │
│ Model:      [CUNet             ▼] │
│                                    │
│ ○ Scale by Factor  ○ Target Size   │
│ Scale Factor: [2x] (1-4)           │
│                                    │
│ [ Advanced Options ]               │
│ ├─ Use TTA: ☐                      │
│ ├─ Denoise Level: 1 (0-3)          │
│ ├─ GPU ID: 0                       │
│ └─ Tile Size: 512                  │
│                                    │
│ Status: Ready                      │
│ Progress: [          ] 0%          │
│                                    │
│ [Process Current Image]            │
│ [Batch Processing...]              │
└────────────────────────────────────┘
```

#### ✅ 第三阶段：命令测试 (10分钟)

**测试 "Process Current Image" 命令**:

1. 打开一张图片
2. 确保超分辨率面板显示 "Status: Ready"
3. 点击 "Process Current Image" 按钮

**预期行为**:
```
操作                          消息提示
────────────────────────────────────────────
点击按钮前                    Status: Ready
↓
点击按钮                      Processing test.jpg (800x600) with super resolution...
↓
处理中 (1-2秒)                [进度条动画]
↓
处理完成                      Super resolution completed: test.jpg scaled by 2x
↓
自动打开新标签页              显示 test_SR_20250105_143022.jpg
```

**检查项**:
- [ ] 点击按钮后显示处理中消息
- [ ] 有进度反馈（模拟延迟1-2秒）
- [ ] 处理完成后显示成功消息
- [ ] 新标签页自动打开
- [ ] 新文件名包含 "_SR_" 和时间戳
- [ ] 临时文件位于 `%TEMP%\NeeView_SuperResolution\`

**测试不同设置**:
- [ ] 修改缩放倍数 (2x, 3x, 4x)
- [ ] 切换到 "Target Size" 模式
- [ ] 启用 TTA 选项
- [ ] 调整去噪级别

#### ✅ 第四阶段：错误处理测试 (5分钟)

**测试场景**:

1. **无图片时**:
   - 关闭所有图片
   - 点击 "Process Current Image"
   - 预期: "No image to process"

2. **不支持的文件类型**:
   - 打开一个文本文件或视频文件（如果NeeView支持）
   - 点击处理
   - 预期: 相应的错误消息

3. **快速连续点击**:
   - 快速点击处理按钮多次
   - 预期: 任务队列正常工作，不崩溃

#### ✅ 第五阶段：配置持久化测试 (5分钟)

**步骤**:
1. 修改一些设置:
   - 算法改为 RealESRGAN
   - 模型改为 X4Plus_Anime
   - 缩放倍数改为 3
   - 启用 TTA
   - 去噪级别改为 2
2. 关闭 NeeView
3. 重新启动 NeeView
4. 打开超分辨率面板

**检查项**:
- [ ] 所有设置都保持不变
- [ ] 算法仍然是 RealESRGAN
- [ ] 模型仍然是 X4Plus_Anime
- [ ] 缩放倍数是 3
- [ ] TTA 仍然启用
- [ ] 去噪级别是 2

## 🐛 常见问题排查

### 问题1: 编译失败

**症状**: `error CS0246: The type or namespace name 'XXX' could not be found`

**解决**:
```csharp
// 检查 using 语句是否正确
using NeeView.SuperResolution;
using NeeView.PageFrames;
using System.Windows.Media.Imaging;
```

### 问题2: 面板不显示

**检查**:
1. `SidePanelFactory.cs` 是否注册了 `SuperResolutionPanel`
2. `Config.cs` 是否添加了 `SuperResolution` 属性
3. 快捷键 `S, R` 是否冲突

**调试**:
```csharp
// 在 ToggleVisibleSuperResolutionCommand.Execute 中添加断点
System.Diagnostics.Debug.WriteLine("Toggling super resolution panel");
```

### 问题3: 点击处理按钮无反应

**检查**:
1. `CommandTable.cs` 是否注册了 `ProcessCurrentImageWithSuperResolutionCommand`
2. `CanExecute` 是否返回 true
3. 是否有图片打开

**调试**:
```csharp
// 在 ProcessCurrentImageWithSuperResolutionCommand.Execute 开头添加
System.Diagnostics.Debug.WriteLine("Processing started");
System.Diagnostics.Debug.WriteLine($"Has image: {ImageDataHelper.GetCurrentImageInfo() != null}");
System.Diagnostics.Debug.WriteLine($"Service available: {SuperResolutionService.Current.IsAvailable}");
```

### 问题4: 处理后没有打开新图片

**检查**:
1. 临时目录是否可写: `%TEMP%\NeeView_SuperResolution\`
2. 文件是否成功创建
3. `BookHub.Current.RequestLoad` 是否正常工作

**调试**:
```csharp
// 在 ImageDataHelper.ShowProcessedImageAsync 中添加
var tempFilePath = "...";
System.Diagnostics.Debug.WriteLine($"Temp file: {tempFilePath}");
System.Diagnostics.Debug.WriteLine($"File exists: {File.Exists(tempFilePath)}");
```

### 问题5: 内存或性能问题

**可能原因**:
- 大图片占用内存过多
- 模拟延迟导致积累任务

**解决**:
```csharp
// 在 SuperResolutionService 中检查任务队列
System.Diagnostics.Debug.WriteLine($"Queue length: {_taskQueue.Count}");
System.Diagnostics.Debug.WriteLine($"Active tasks: {_activeTasks}");
```

## 📊 测试结果记录表

| 测试项 | 预期结果 | 实际结果 | 状态 | 备注 |
|--------|----------|----------|------|------|
| 编译成功 | 无错误 | | ⬜ | |
| 面板显示 | UI正常 | | ⬜ | |
| 快捷键 S,R | 面板切换 | | ⬜ | |
| 算法切换 | 模型联动 | | ⬜ | |
| 缩放模式切换 | UI更新 | | ⬜ | |
| 处理按钮 | 显示消息 | | ⬜ | |
| 打开结果 | 新标签页 | | ⬜ | |
| 错误处理 | 提示正确 | | ⬜ | |
| 配置保存 | 重启后保持 | | ⬜ | |
| 性能 | 无卡顿 | | ⬜ | |

**状态说明**:
- ✅ 通过
- ❌ 失败
- ⚠️ 部分通过
- ⬜ 未测试

## 🎯 验收标准

### 最低标准 (必须全部满足)
- [x] ✅ 代码可以编译
- [ ] ✅ 应用可以启动
- [ ] ✅ 面板可以打开
- [ ] ✅ 控件可以交互
- [ ] ✅ 处理按钮有反应
- [ ] ✅ 有消息反馈

### 基本标准 (应该满足)
- [ ] ✅ 处理完成后打开新图片
- [ ] ✅ 临时文件正确创建
- [ ] ✅ 设置可以保存
- [ ] ✅ 错误处理正常
- [ ] ✅ 快捷键工作
- [ ] ✅ UI响应流畅

### 完整标准 (期望满足)
- [ ] ✅ 所有算法可选
- [ ] ✅ 所有模型可选
- [ ] ✅ 两种缩放模式都工作
- [ ] ✅ 高级选项生效
- [ ] ✅ 批量处理可用 (UI存在)
- [ ] ✅ 帮助文档完整

## 🚀 测试通过后

### 下一步: 集成真实算法

**准备工作**:
1. 决定使用 ONNX Runtime 或 ncnn
2. 下载模型文件
3. 测试小图片处理
4. 优化性能

**参考**:
- `OnnxSuperResolutionEngine.template.cs` - ONNX 实现模板
- `IMAGE_DATA_INTEGRATION_GUIDE.md` - 详细集成指南
- `INTEGRATION_COMPLETE_REPORT.md` - 完整报告

### 验证真实算法

**测试图片**:
- 小图: 256x256 (快速测试)
- 中图: 512x512 (正常测试)
- 大图: 1920x1080 (性能测试)

**验证项**:
- [ ] 图片确实被放大
- [ ] 细节增强明显
- [ ] 没有明显伪影
- [ ] 处理时间合理
- [ ] GPU 加速生效 (如果使用)

## 📝 测试日志模板

```
测试日期: 2025-01-05
测试人员: [Your Name]
NeeView 版本: [Version]
超分辨率版本: 1.0.0 (Mock Engine)

=== 编译测试 ===
[ ] 编译成功
[ ] 警告数量: 0
[ ] 错误数量: 0

=== UI测试 ===
[ ] 面板显示正常
[ ] 所有控件可用
[ ] 快捷键工作: S,R

=== 功能测试 ===
测试图片: test.jpg (800x600)
[ ] 处理启动: 成功
[ ] 处理时间: 1.2秒
[ ] 结果显示: 成功
[ ] 临时文件: test_SR_20250105_143022.jpg

=== 错误测试 ===
[ ] 无图片错误: 正确提示
[ ] 连续点击: 无崩溃

=== 配置测试 ===
[ ] 设置保存: 成功
[ ] 设置恢复: 成功

=== 问题记录 ===
1. [如有问题，记录在此]
2. 

=== 总结 ===
测试通过: [ ] 是 [ ] 否
主要问题: 
建议:
```

---

**创建时间**: 2025-01-05  
**文档版本**: 1.0  
**适用版本**: NeeView 超分辨率 v1.0 (Mock Engine)
