import sr_vulkan
import time

print('=== sr-vulkan API 测试 ===')
print()

# 创建实例
sr = sr_vulkan.Sr()
print('Sr 对象方法:')
for m in [x for x in dir(sr) if not x.startswith('_')]:
    print(f'  - {m}')
print()

# 设置路径
sr.setPath(r'D:\1Dev\Python\packages\Python311\site-packages\sr_vulkan_model_waifu2x\models')
print('✓ 路径设置成功')

# 初始化
result = sr.init()
print(f'sr.init() = {result}')

# 设置GPU
result = sr.initSet(0, 0)
print(f'sr.initSet(0, 0) = {result}')

# 版本
print(f'版本: {sr.getVersion()}')

# GPU信息  
print(f'GPU: {sr.getGpuInfo()}')

print()
print('=== 测试完成 ===')
