# 创建测试用的 AVIF 图片
# 这个脚本会使用 Python PIL 创建一个简单的测试图片

Write-Host "创建测试 AVIF 图片..." -ForegroundColor Cyan
Write-Host ""

$pythonScript = @"
from PIL import Image, ImageDraw, ImageFont
import os

# 创建一个 512x512 的测试图片
width, height = 512, 512
image = Image.new('RGB', (width, height), color='#2C3E50')

# 绘制一些图案
draw = ImageDraw.Draw(image)

# 绘制渐变背景
for i in range(height):
    r = int(44 + (200 - 44) * i / height)
    g = int(62 + (150 - 62) * i / height)
    b = int(80 + (100 - 80) * i / height)
    draw.rectangle([(0, i), (width, i+1)], fill=(r, g, b))

# 绘制网格
for i in range(0, width, 64):
    draw.line([(i, 0), (i, height)], fill='white', width=1)
for i in range(0, height, 64):
    draw.line([(0, i), (width, i)], fill='white', width=1)

# 绘制一些形状
draw.ellipse([128, 128, 384, 384], fill='#E74C3C', outline='white', width=3)
draw.rectangle([200, 200, 312, 312], fill='#3498DB', outline='white', width=3)

# 添加文本
try:
    font = ImageFont.truetype("arial.ttf", 40)
except:
    font = ImageFont.load_default()

text = "AVIF Test"
bbox = draw.textbbox((0, 0), text, font=font)
text_width = bbox[2] - bbox[0]
text_height = bbox[3] - bbox[1]
x = (width - text_width) // 2
y = height - 80

draw.text((x, y), text, fill='white', font=font)

# 保存为 AVIF
output_path = 'test.avif'
image.save(output_path, 'AVIF', quality=85)

print(f"✅ 测试图片已创建: {output_path}")
print(f"   尺寸: {width}x{height}")
print(f"   文件大小: {os.path.getsize(output_path) / 1024:.2f} KB")
"@

# 写入临时 Python 脚本
$tempScript = "create_test_avif.py"
$pythonScript | Out-File -FilePath $tempScript -Encoding UTF8

Write-Host "运行 Python 脚本..." -ForegroundColor Yellow
python $tempScript

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✅ 完成!" -ForegroundColor Green
    Write-Host ""
    Write-Host "现在可以运行测试:" -ForegroundColor Cyan
    Write-Host "  .\RunAvifSRTest.ps1 -ImagePath `"test.avif`"" -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "❌ 创建失败" -ForegroundColor Red
    Write-Host ""
    Write-Host "请确保已安装 Pillow:" -ForegroundColor Yellow
    Write-Host "  pip install Pillow pillow-avif-plugin" -ForegroundColor Cyan
}

# 清理临时文件
Remove-Item $tempScript -ErrorAction SilentlyContinue
