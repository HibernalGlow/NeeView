#!/usr/bin/env python
# 测试 sr_vulkan API 调用流程

from sr_vulkan import sr_vulkan as sr
import io
import os
from PIL import Image
import time

print("=== 测试 sr_vulkan API ===\n")

# 创建测试图片
print("1. 创建 100x100 测试图片...")
img = Image.new('RGB', (100, 100), color='red')
buf = io.BytesIO()
img.save(buf, format='PNG')
data = buf.getvalue()
print(f"   图片大小: {len(data)} bytes\n")

# 初始化
print("2. 初始化 sr_vulkan...")

# 自动检测模型路径
import sr_vulkan_model_waifu2x
model_path = os.path.join(os.path.dirname(sr_vulkan_model_waifu2x.__file__), "models")
print(f"   自动检测模型路径: {model_path}")
print(f"   路径存在: {os.path.exists(model_path)}")

# 先设置模型路径(在 init 之前!)
sr.setModelPath(model_path)
print("   已设置模型路径")

# 然后初始化 (不带参数!)
print("   调用 sr.init()...")
result = sr.init()
print(f"   init() 返回: {result}")

# 调用 initSet (关键!)
print("   调用 sr.initSet(0, 0)...")
result = sr.initSet(0, 0)
print(f"   initSet() 返回: {result}\n")

# 添加任务
print("3. 添加超分任务...")
print(f"   使用模型索引: 0 (MODEL_WAIFU2X_CUNET_UP1X_DENOISE0X)")
print(f"   任务ID: 12345")
print(f"   缩放: 2x")

try:
    procId = sr.add(data, 0, 12345, 2, format='png')
    print(f"   返回 procId: {procId}\n")
    
    if procId <= 0:
        print(f"   ❌ procId <= 0, 任务添加失败!")
        err = sr.getLastError()
        print(f"   错误信息: {err}")
        exit(1)
except Exception as e:
    print(f"   ❌ add() 失败: {e}")
    import traceback
    traceback.print_exc()
    exit(1)

# 轮询结果
print("4. 轮询处理结果...")
result = None
for i in range(60):  # 最多等待60秒
    result = sr.load(0)
    if result:
        print(f"   ✅ 在第 {i+1} 次轮询时获取到结果\n")
        break
    if i % 5 == 0 and i > 0:
        print(f"   仍在处理... (已等待 {i} 秒)")
    time.sleep(1)

if not result:
    print("   ❌ 处理超时!")
    err = sr.getLastError()
    print(f"   错误信息: {err}")
    exit(1)

# 解析结果
print("5. 解析结果...")
print(f"   结果类型: {type(result)}")
print(f"   结果长度: {len(result)}")

try:
    data_out, format_out, taskId_out, tick_out = result
    print(f"   taskId: {taskId_out}")
    print(f"   format: {format_out}")
    print(f"   tick: {tick_out:.2f}s")
    print(f"   输出大小: {len(data_out) if data_out else 0} bytes")
    
    if data_out and len(data_out) > 0:
        print("\n✅ 测试成功!")
        # 验证输出图片
        img_out = Image.open(io.BytesIO(data_out))
        print(f"   输出尺寸: {img_out.width}x{img_out.height}")
    else:
        print("\n❌ 输出数据为空!")
except Exception as e:
    print(f"\n❌ 解析失败: {e}")
    import traceback
    traceback.print_exc()
