# 🚀 NeeView 自动构建快速指南

## 📦 已创建的文件

```
.github/
├── workflows/
│   └── build-release.yml      # GitHub Actions 主工作流
└── CICD_README.md             # 详细文档

_Version.props                  # 版本配置文件
Build-Local.ps1                 # 本地构建测试脚本
```

## ⚡ 快速开始

### 方式 1: 推送到 dev 分支 (自动触发)

```bash
git add .
git commit -m "feat: 添加超分辨率预加载和自定义缩放功能"
git push origin dev
```

推送后自动触发构建,但**不会创建 Release**。

### 方式 2: 手动触发 (推荐)

1. 进入 GitHub 仓库
2. 点击 **Actions** 标签
3. 选择 **Build and Release NeeView**
4. 点击 **Run workflow**
5. 配置参数:
   - Version: `44.2.0`
   - Create Release: ✅ (勾选)
6. 点击绿色 **Run workflow** 按钮

✅ **会自动创建 GitHub Release!**

### 方式 3: 创建版本标签

```bash
git tag v44.2.0
git push origin v44.2.0
```

自动触发构建并创建 Release。

## 📋 构建产物

每次构建生成 **4 个 ZIP 包**:

| 文件名 | 说明 |
|--------|------|
| `NeeView-x64-{version}.zip` | x64 自包含版本 (推荐) |
| `NeeView-x64-fd-{version}.zip` | x64 框架依赖版本 |
| `NeeView-x86-{version}.zip` | x86 自包含版本 |
| `NeeView-x86-fd-{version}.zip` | x86 框架依赖版本 |

版本号格式: `44.2.0-dev.20250127123045`

## 🧪 本地测试

### 构建 x64 版本

```powershell
.\Build-Local.ps1 -Platform x64
```

### 构建框架依赖版本

```powershell
.\Build-Local.ps1 -Platform x64 -FrameworkDependent
```

### 构建所有变体

```powershell
.\Build-Local.ps1 -Platform All
```

### 清理并重新构建

```powershell
.\Build-Local.ps1 -Platform x64 -Clean
```

## 📊 查看构建状态

### GitHub Actions 界面

1. 进入仓库 **Actions** 标签
2. 查看工作流运行列表
3. 点击具体运行查看详细日志

### 徽章 (可选添加到 README)

```markdown
![Build Status](https://github.com/YOUR_USERNAME/NeeView/actions/workflows/build-release.yml/badge.svg?branch=dev)
```

## 🔧 常见问题

### Q: 构建失败怎么办?

**A:** 点击失败的工作流 → 查看红色 ❌ 的步骤 → 展开日志查看错误信息。

### Q: Release 创建失败?

**A:** 确保:
- 手动触发时勾选了 "Create GitHub Release"
- 或者推送的是版本标签 (v开头)
- 检查 GITHUB_TOKEN 权限

### Q: 如何修改版本号?

**A:** 编辑 `_Version.props`:

```xml
<VersionPrefix>44.3.0</VersionPrefix>
```

或手动触发时输入新版本号。

### Q: NetBeauty2 优化失败?

**A:** 这是非关键步骤,设置了 `continue-on-error: true`,即使失败也不影响构建。

## 📝 工作流特性

- ✅ **多平台**: x64 / x86
- ✅ **多变体**: 自包含 / 框架依赖
- ✅ **自动版本**: 时间戳后缀
- ✅ **自动清理**: 移除 PDB 和设置文件
- ✅ **ZIP 打包**: 自动压缩
- ✅ **Artifacts 上传**: 保留 30 天
- ✅ **自动 Release**: 带 Changelog
- ✅ **NetBeauty2**: 优化文件布局 (可选)

## 🎯 下一步

1. **提交代码**到 dev 分支
2. **手动触发**工作流 (勾选 Create Release)
3. **等待构建**完成 (~10-15 分钟)
4. **下载 Release** 中的 ZIP 包
5. **测试功能**: 超分辨率、预加载、自定义倍数

## 📚 详细文档

查看 `.github/CICD_README.md` 获取完整说明。

---

**🎉 现在可以推送代码或手动触发工作流了!**
