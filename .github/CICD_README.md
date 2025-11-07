# GitHub Actions 自动构建说明

本项目使用 GitHub Actions 实现自动编译和发布。

## 工作流文件

- `.github/workflows/build-release.yml` - 主构建和发布工作流

## 触发方式

### 1. 自动触发

- **推送到 dev 分支**: 每次推送代码到 `dev` 分支时自动触发构建
- **创建版本标签**: 创建格式为 `v*.*.*` 的标签时自动触发构建和发布

### 2. 手动触发

在 GitHub 仓库页面:

1. 进入 **Actions** 标签
2. 选择 **Build and Release NeeView** 工作流
3. 点击 **Run workflow** 按钮
4. 可选配置:
   - **Version number**: 版本号 (默认: 44.2.0)
   - **Create GitHub Release**: 是否创建 Release (默认: true)

## 构建输出

工作流会生成以下构建变体:

### x64 平台
- `NeeView-x64` - 自包含版本 (包含 .NET 运行时)
- `NeeView-x64-fd` - 框架依赖版本 (需要安装 .NET 9.0)

### x86 平台
- `NeeView-x86` - 自包含版本 (包含 .NET 运行时)
- `NeeView-x86-fd` - 框架依赖版本 (需要安装 .NET 9.0)

## 版本命名规则

开发版本格式: `{VERSION_PREFIX}-dev.{TIMESTAMP}`

例如: `44.2.0-dev.20250127123045`

## 构建步骤

1. **Checkout**: 检出源代码
2. **Setup .NET**: 配置 .NET 9.0 环境
3. **Restore**: 还原 NuGet 依赖
4. **Build**: 编译主项目和 Susie 插件
5. **Optimize**: 使用 NetBeauty2 优化文件布局 (可选)
6. **Package**: 创建 ZIP 压缩包
7. **Upload**: 上传构建产物到 Artifacts
8. **Release**: 创建 GitHub Release (如果启用)

## Artifacts 保留期

构建产物在 GitHub Actions 中保留 **30 天**。

## 环境要求

- **.NET SDK**: 9.0.x
- **操作系统**: Windows Latest
- **PowerShell**: 用于构建脚本

## 本地测试

要在本地测试构建流程,可以使用以下命令:

```powershell
# 编译 x64 自包含版本
dotnet publish NeeView/NeeView.csproj `
  -c Release `
  -p:Platform=x64 `
  -p:VersionPrefix=44.2.0 `
  -p:VersionSuffix=dev.local `
  --self-contained true `
  -o Publish/NeeView-x64

# 编译 Susie 插件
dotnet publish NeeView.Susie.Server/NeeView.Susie.Server.csproj `
  -c Release `
  -p:Platform=x64 `
  -p:VersionPrefix=44.2.0 `
  -p:VersionSuffix=dev.local `
  --self-contained false `
  -o Publish/NeeView-x64/Libraries/Susie
```

## 疑难解答

### 构建失败

1. 检查 Actions 日志中的错误信息
2. 确认所有依赖项已正确配置
3. 验证 `_Version.props` 文件存在且格式正确

### NetBeauty2 优化失败

NetBeauty2 步骤设置为 `continue-on-error: true`,即使失败也不会中断构建流程。

### Release 未创建

确保满足以下条件之一:
- 手动触发工作流并勾选 "Create GitHub Release"
- 推送版本标签 (格式: `v*.*.*`)

## 更新版本号

编辑 `_Version.props` 文件:

```xml
<VersionPrefix>44.3.0</VersionPrefix>
```

或在手动触发时输入新版本号。

## 参考资料

- [GitHub Actions 文档](https://docs.github.com/en/actions)
- [.NET CLI 发布命令](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-publish)
- [NetBeauty2 工具](https://github.com/nulastudio/NetBeauty2)
