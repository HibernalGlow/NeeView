# 本地构建脚本 - NeeView 44.2 Dev (仅 x64 自包含版本)
# 用于测试 GitHub Actions 工作流中的构建步骤

Param(
    [string]$Version = "44.2.0",
    
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

# 配置
$ProjectFile = "NeeView\NeeView.csproj"
$SusieProject = "NeeView.Susie.Server\NeeView.Susie.Server.csproj"
$Configuration = "Release"
$Timestamp = Get-Date -Format 'yyyyMMddHHmmss'
$VersionSuffix = "dev.$Timestamp"
$FullVersion = "$Version-$VersionSuffix"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "NeeView Local Build Script (x64 Only)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Version: $FullVersion" -ForegroundColor Yellow
Write-Host "Platform: x64 (self-contained)" -ForegroundColor Yellow
Write-Host ""

function Build-NeeView {
    Write-Host "----------------------------------------" -ForegroundColor Green
    Write-Host "Building NeeView x64 (Self-Contained)" -ForegroundColor Green
    Write-Host "----------------------------------------" -ForegroundColor Green
    
    $OutputDir = "Publish\NeeView-x64"
    
    # 清理旧构建
    if ($Clean -and (Test-Path $OutputDir)) {
        Write-Host "Cleaning $OutputDir..." -ForegroundColor Yellow
        Remove-Item $OutputDir -Recurse -Force
    }
    
    # 构建主项目
    Write-Host "Building main project..." -ForegroundColor Cyan
    $params = @(
        "publish",
        $ProjectFile,
        "-c", $Configuration,
        "-p:Platform=x64",
        "-p:VersionPrefix=$Version",
        "-p:VersionSuffix=$VersionSuffix",
        "-p:PublishReadyToRun=true",
        "-p:PublishTrimmed=false",
        "-p:PublishSingleFile=false",
        "--self-contained", "true",
        "-o", $OutputDir
    )
    
    & dotnet @params
    if ($LASTEXITCODE -ne 0) {
        throw "Main project build failed with exit code $LASTEXITCODE"
    }
    
    # 构建 Susie 插件
    Write-Host "Building Susie plugin..." -ForegroundColor Cyan
    $susieParams = @(
        "publish",
        $SusieProject,
        "-c", $Configuration,
        "-p:Platform=x64",
        "-p:VersionPrefix=$Version",
        "-p:VersionSuffix=$VersionSuffix",
        "--self-contained", "false",
        "-o", "$OutputDir\Libraries\Susie"
    )
    
    & dotnet @susieParams
    if ($LASTEXITCODE -ne 0) {
        throw "Susie plugin build failed with exit code $LASTEXITCODE"
    }
    
    # 清理构建产物
    Write-Host "Cleaning artifacts..." -ForegroundColor Cyan
    Get-ChildItem -Path $OutputDir -Filter *.pdb -Recurse | Remove-Item -Force
    $settingsFile = Join-Path $OutputDir "NeeView.settings.json"
    if (Test-Path $settingsFile) {
        Remove-Item $settingsFile -Force
    }
    
    # 优化文件布局 (可选)
    if (Get-Command nbeauty2 -ErrorAction SilentlyContinue) {
        Write-Host "Optimizing with NetBeauty2..." -ForegroundColor Cyan
        try {
            & nbeauty2 --usepatch --loglevel Detail $OutputDir Libraries
            
            $bakFile = Join-Path $OutputDir "hostfxr.dll.bak"
            if (Test-Path $bakFile) {
                Remove-Item $bakFile -Force
            }
        }
        catch {
            Write-Host "NetBeauty2 optimization failed (non-critical): $_" -ForegroundColor Yellow
        }
    }
    else {
        Write-Host "NetBeauty2 not found, skipping optimization" -ForegroundColor Yellow
    }
    
    # 创建 ZIP 压缩包
    Write-Host "Creating ZIP archive..." -ForegroundColor Cyan
    $ArchiveName = "NeeView-x64-$FullVersion.zip"
    if (Test-Path $ArchiveName) {
        Remove-Item $ArchiveName -Force
    }
    
    Compress-Archive -Path "$OutputDir\*" -DestinationPath $ArchiveName -CompressionLevel Optimal
    
    $FileSize = (Get-Item $ArchiveName).Length / 1MB
    Write-Host "Archive created: $ArchiveName ($([math]::Round($FileSize, 2)) MB)" -ForegroundColor Green
    Write-Host ""
}

# 执行构建
try {
    Build-NeeView
    
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Build completed successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
}
catch {
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Build failed: $_" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    exit 1
}
