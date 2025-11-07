from sr_vulkan import sr_vulkan as sr
import time

print('=== sr-vulkan 完整测试 ===')
print()

# 1. 列出所有函数
funcs = [x for x in dir(sr) if not x.startswith('_') and not x.startswith('MODEL')]
print('可用函数:')
for f in funcs:
    print(f'  - {f}')
print()

# 2. 查看模型常量
models = [x for x in dir(sr) if x.startswith('MODEL_WAIFU2X_ANIME')]
print('Waifu2x Anime 模型:')
for m in models:
    val = getattr(sr, m)
    print(f'  {m} = {val}')
print()

# 3. 初始化流程
print('--- 初始化流程 ---')
sr.setPath(r'D:\1Dev\Python\packages\Python311\site-packages\sr_vulkan_model_waifu2x\models')
print('✓ setPath')

result = sr.init()
print(f'sr.init() = {result}')

result = sr.initSet(0, 0)
print(f'sr.initSet(0, 0) = {result}')

print(f'版本: {sr.getVersion()}')
print(f'GPU: {sr.getGpuInfo()}')
print()

print('=== 初始化成功 ===')
