from PIL import Image
import io
from sr_vulkan import sr_vulkan as sr

print('测试所有模型类型:')
print('Waifu2x模型:', sr.MODEL_WAIFU2X_ANIME_UP2X)
print('RealESRGAN模型:', sr.MODEL_REALESRGAN_X4PLUS_UP4X)
print('RealCUGAN模型:', sr.MODEL_REALCUGAN_SE_UP2X)
print()

# 初始化
sr.init()
sr.initSet(0, 0)

# 生成测试图
img = Image.new('RGB', (64, 64), (255, 0, 0))
buf = io.BytesIO()
img.save(buf, 'PNG')
data = buf.getvalue()

# 测试各模型
models = [
    (18, 'Waifu2x', 2),
    (80, 'RealESRGAN', 4),
    (0, 'RealCUGAN', 2)
]

for mid, name, scale in models:
    proc_id = sr.add(data, mid, 10000+mid, scale=scale)
    status = 'SUCCESS' if proc_id >= 0 else f'FAILED({sr.getLastError()})'
    print(f'{name:12s} (ID={mid:2d}, scale={scale}x): procId={proc_id:3d} [{status}]')
