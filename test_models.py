from PIL import Image
import io
from sr_vulkan import sr_vulkan as sr

# 生成测试图片
img = Image.new('RGB', (64, 64), (255, 0, 0))
buf = io.BytesIO()
img.save(buf, 'PNG')
data = buf.getvalue()

# 初始化
sr.setModelPath(r'D:\1Dev\Python\packages\Python311\site-packages\sr_vulkan_model_waifu2x\models')
sr.init()
sr.initSet(0, 0)

# 测试不同模型ID
print('测试不同模型ID:')
for mid in [0, 1, 18, 20, 80]:
    proc_id = sr.add(data, mid, 123+mid, 2, 'png')
    print(f'  model {mid:2d} -> procId={proc_id:3d}')
    if proc_id < 0:
        print(f'      error: {sr.getLastError()}')
