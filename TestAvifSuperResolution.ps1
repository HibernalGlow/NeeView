# AVIF 超分辨率测试脚本
# 用法: .\TestAvifSuperResolution.ps1 -ImagePath ".\test.avif"

param(
    [Parameter(Mandatory=$true)]
    [string]$ImagePath
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "AVIF 超分辨率测试" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 检查文件是否存在
if (-not (Test-Path $ImagePath)) {
    Write-Host "错误: 文件不存在 - $ImagePath" -ForegroundColor Red
    exit 1
}

$ImagePath = Resolve-Path $ImagePath
Write-Host "输入文件: $ImagePath" -ForegroundColor Green

# 检查 NeeView 是否已构建
$exePath = "d:\1VSCODE\Projects\ImageAll\NeeWaifu\NeeView\NeeView\bin\Debug\net9.0-windows\NeeView.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "错误: NeeView.exe 未找到，请先构建项目" -ForegroundColor Red
    Write-Host "运行: dotnet build" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "提示: 请按以下步骤操作:" -ForegroundColor Yellow
Write-Host "1. NeeView 将自动启动并打开图片" -ForegroundColor Yellow
Write-Host "2. 打开侧边栏 (菜单 -> 视图 -> 超分辨率)" -ForegroundColor Yellow
Write-Host "3. 设置模型路径并扫描模型" -ForegroundColor Yellow
Write-Host "4. 选择一个模型" -ForegroundColor Yellow
Write-Host "5. 点击 '处理当前图片'" -ForegroundColor Yellow
Write-Host "6. 查看日志输出" -ForegroundColor Yellow
Write-Host ""

# 启动 NeeView 并打开图片
Write-Host "启动 NeeView..." -ForegroundColor Green
Start-Process -FilePath $exePath -ArgumentList "`"$ImagePath`""

Write-Host ""
Write-Host "日志位置: C:\Users\$env:USERNAME\AppData\Local\NeeView\Logs\" -ForegroundColor Cyan
Write-Host ""
Write-Host "按 Ctrl+C 查看最新日志..." -ForegroundColor Yellow
Start-Sleep -Seconds 2

# 等待并监控日志
$logPath = "C:\Users\$env:USERNAME\AppData\Local\NeeView\Logs"
$today = Get-Date -Format "yyyyMMdd"
$srLogPattern = "SuperResolution*$today*.log"

Write-Host "监控日志文件..." -ForegroundColor Cyan
for ($i = 0; $i -lt 30; $i++) {
    $logFiles = Get-ChildItem -Path $logPath -Filter $srLogPattern -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending
    if ($logFiles) {
        $latestLog = $logFiles[0].FullName
        Write-Host "`n找到日志: $latestLog" -ForegroundColor Green
        Write-Host "最新日志内容 (最后 20 行):" -ForegroundColor Cyan
        Write-Host "----------------------------------------" -ForegroundColor Gray
        Get-Content $latestLog -Tail 20 -ErrorAction SilentlyContinue
        Write-Host "----------------------------------------" -ForegroundColor Gray
        break
    }
    Start-Sleep -Seconds 1
}

Write-Host "`n测试脚本完成。请在 NeeView 中执行超分操作。" -ForegroundColor Green
