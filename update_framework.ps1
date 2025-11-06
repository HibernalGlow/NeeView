# 批量修改目标框架的PowerShell脚本
Get-ChildItem -Path . -Recurse -Filter "*.csproj" | ForEach-Object {
    $content = Get-Content -Path $_.FullName -Raw
    if ($content -match 'net9\.0-windows') {
        $content = $content -replace 'net9\.0-windows', 'net8.0-windows'
        Set-Content -Path $_.FullName -Value $content -NoNewline
        Write-Host "已修改: $($_.FullName)"
    }
}