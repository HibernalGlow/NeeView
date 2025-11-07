# 本地构建脚本 - NeeView 44.2 Dev
# 用于测试 GitHub Actions 工作流中的构建步骤

Param(
    [ValidateSet("x64", "x86", "All")]
    [string]$Platform = "x64",
    
    [switch]$FrameworkDependent,
    
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
Write-Host "NeeView Local Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Version: $FullVersion" -ForegroundColor Yellow
Write-Host "Platform: $Platform" -ForegroundColor Yellow
Write-Host "Framework Dependent: $FrameworkDependent" -ForegroundColor Yellow
Write-Host ""

function Build-NeeView {
    param(
        [string]$Arch,
        [bool]$IsFD
    )
    
    $SelfContained = if ($IsFD) { "false" } else { "true" }
    $OutputSuffix = if ($IsFD) { "-fd" } else { "" }
    $OutputDir = "Publish\NeeView-$Arch$OutputSuffix"
    
    Write-Host "----------------------------------------" -ForegroundColor Green
    Write-Host "Building NeeView-$Arch$OutputSuffix" -ForegroundColor Green
    Write-Host "----------------------------------------" -ForegroundColor Green
    
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
        "-p:Platform=$Arch",
        "-p:VersionPrefix=$Version",
        "-p:VersionSuffix=$VersionSuffix",
        "-p:PublishReadyToRun=true",
        "-p:PublishTrimmed=false",
        "-p:PublishSingleFile=false",
        "--self-contained", $SelfContained,
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
        "-p:Platform=$Arch",
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
    $ArchiveName = "NeeView-$Arch$OutputSuffix-$FullVersion.zip"
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
    if ($Platform -eq "All") {
        Build-NeeView -Arch "x64" -IsFD $false
        Build-NeeView -Arch "x64" -IsFD $true
        Build-NeeView -Arch "x86" -IsFD $false
        Build-NeeView -Arch "x86" -IsFD $true
    }
    else {
        Build-NeeView -Arch $Platform -IsFD $FrameworkDependent
    }
    
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
