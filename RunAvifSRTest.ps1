# AVIF 超分辨率测试 - 构建并运行
# 用法: .\RunAvifSRTest.ps1 [-ImagePath "test.avif"]

param(
    [string]$ImagePath = ""
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "AVIF 超分辨率测试 - 构建并运行" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$projectPath = "AvifSRTest\AvifSRTest.csproj"
$outputExe = "AvifSRTest\bin\Debug\net9.0-windows\AvifSRTest.exe"

# 检查项目文件是否存在
if (-not (Test-Path $projectPath)) {
    Write-Host "错误: 项目文件不存在 - $projectPath" -ForegroundColor Red
    exit 1
}

# 构建项目
Write-Host "📦 构建测试项目..." -ForegroundColor Green
dotnet build $projectPath -c Debug

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "❌ 构建失败" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✅ 构建成功" -ForegroundColor Green
Write-Host ""

# 运行测试
if ($ImagePath) {
    if (-not (Test-Path $ImagePath)) {
        Write-Host "❌ 错误: 图片文件不存在 - $ImagePath" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "🚀 运行测试: $ImagePath" -ForegroundColor Yellow
    Write-Host ""
    
    & $outputExe $ImagePath
} else {
    Write-Host "💡 提示: 请拖放图片文件到程序上，或运行:" -ForegroundColor Yellow
    Write-Host "   .\RunAvifSRTest.ps1 -ImagePath `"your_image.avif`"" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "或直接运行程序:" -ForegroundColor Yellow
    Write-Host "   .\$outputExe" -ForegroundColor Cyan
    Write-Host ""
    
    # 启动程序
    Start-Process -FilePath $outputExe -Wait
}
