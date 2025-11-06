#!/usr/bin/env python
# 最小化测试 - 完全模仿 picacg-qt 的调用方式

from sr_vulkan import sr_vulkan as sr
from PIL import Image
import io
import os

print("=== 最小化 sr_vulkan 测试 ===\n")

# 1. 创建测试图片
img = Image.new('RGB', (100, 100), color='red')
buf = io.BytesIO()
img.save(buf, format='PNG')
data = buf.getvalue()
print(f"1. 测试图片: {len(data)} bytes\n")

# 2. 初始化 (完全按照 picacg-qt)
print("2. 初始化...")
stat = sr.init()
print(f"   sr.init() = {stat}")

sr.setDebug(True)
print("   已启用调试模式")

gpuInfo = sr.getGpuInfo()
cpuNum = sr.getCpuCoreNum()
print(f"   GPU信息: {gpuInfo}")
print(f"   CPU核心数: {cpuNum}")

# 3. initSet (picacg 使用 config.Encode=0, config.UseCpuNum=0)
print("\n3. 设置 GPU...")
sts = sr.initSet(0, 0)
print(f"   sr.initSet(0, 0) = {sts}")

version = sr.getVersion()
print(f"   版本: {version}")

# 4. 尝试添加任务 (使用整数 0 作为模型索引)
print("\n4. 添加任务...")
print("   参数: data={} bytes, model=0, taskId=999, scale=2, format='png'".format(len(data)))

try:
    # 完全按照 picacg-qt 的参数顺序
    procId = sr.add(data, 0, 999, 2, format='png')
    print(f"   procId = {procId}")
    
    if procId <= 0:
        err = sr.getLastError()
        print(f"   ❌ 失败: {err}")
    else:
        print(f"   ✅ 成功添加任务!")
        
        # 轮询结果
        print("\n5. 等待结果...")
        for i in range(10):
            result = sr.load(0)
            if result:
                data_out, format_out, taskId_out, tick_out = result
                print(f"   ✅ 完成! taskId={taskId_out}, tick={tick_out:.2f}s")
                print(f"   输出大小: {len(data_out)} bytes")
                break
            print(f"   等待中... ({i+1}s)")
            import time
            time.sleep(1)
        else:
            print("   超时")
            
except Exception as e:
    print(f"   异常: {e}")
    import traceback
    traceback.print_exc()
