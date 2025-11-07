#!/usr/bin/env python# 测试 sr_vulkan 是否正确安装

# -*- coding: utf-8 -*-

"""import sys

测试 sr-vulkan 2.0.1 的正确使用方法print("Python version:", sys.version)

"""

import sr_vulkantry:

import time    import sr_vulkan

from pathlib import Path    print("✅ sr_vulkan 已安装")

    print("sr_vulkan version:", sr_vulkan.__version__ if hasattr(sr_vulkan, '__version__') else "unknown")

print("=== sr-vulkan 测试脚本 ===\n")except ImportError as e:

    print("❌ sr_vulkan 未安装:", e)

# 1. 创建 Sr 实例    print("\n请运行: pip install sr-vulkan sr-vulkan-model-waifu2x")

print("1. 创建 Sr 实例...")    sys.exit(1)

sr = sr_vulkan.Sr()

print(f"   sr 对象: {sr}")try:

print(f"   sr 类型: {type(sr)}")    from sr_vulkan import sr_vulkan as sr

    print("\n可用模型:")

# 2. 列出所有可用方法    

print("\n2. Sr 对象的所有方法:")    models = [

methods = [x for x in dir(sr) if not x.startswith('_')]        "waifu2x_cunet",

for method in methods:        "waifu2x_upconv_7_photo",

    print(f"   - {method}")        "realesrgan_animevideo",

        "realesrgan_plus",

# 3. 设置模型路径        "realcugan_conservative",

print("\n3. 设置模型路径...")        "realcugan_denoise3x"

model_path = r"D:\1Dev\Python\packages\Python311\site-packages\sr_vulkan_model_waifu2x\models"    ]

try:    

    sr.setPath(model_path)    for model_name in models:

    print(f"   ✓ 模型路径设置成功: {model_path}")        if hasattr(sr, model_name):

except Exception as e:            print(f"  ✅ {model_name}")

    print(f"   ✗ 设置路径失败: {e}")        else:

            print(f"  ❌ {model_name} (未安装模型包)")

# 4. 初始化            

print("\n4. 调用 sr.init()...")    print("\n✅ sr_vulkan 配置正确!")

try:    print("NeeView 的 Python 引擎应该可以正常工作。")

    result = sr.init()    

    print(f"   init() 返回: {result} (type: {type(result)})")except Exception as e:

except Exception as e:    print("❌ 错误:", e)

    print(f"   ✗ init() 失败: {e}")    import traceback

    traceback.print_exc()

# 5. 获取版本
print("\n5. 获取版本信息...")
try:
    version = sr.getVersion()
    print(f"   版本: {version}")
except Exception as e:
    print(f"   ✗ 获取版本失败: {e}")

# 6. 获取 GPU 信息
print("\n6. 获取 GPU 信息...")
try:
    gpu_info = sr.getGpuInfo()
    print(f"   GPU: {gpu_info}")
except Exception as e:
    print(f"   ✗ 获取 GPU 失败: {e}")

# 7. 设置 GPU
print("\n7. 调用 sr.initSet(0, 0)...")
try:
    result = sr.initSet(0, 0)  # GPU 0, 自动线程
    print(f"   initSet() 返回: {result} (type: {type(result)})")
except Exception as e:
    print(f"   ✗ initSet() 失败: {e}")

# 8. 列出所有模型常量
print("\n8. 列出所有 MODEL_ 常量:")
model_constants = [x for x in dir(sr_vulkan) if x.startswith('MODEL_')]
for i, const in enumerate(model_constants[:10], 1):  # 只显示前10个
    try:
        value = getattr(sr_vulkan, const)
        print(f"   {i:2d}. {const:40s} = {value}")
    except:
        print(f"   {i:2d}. {const:40s} = ???")
if len(model_constants) > 10:
    print(f"   ... 还有 {len(model_constants) - 10} 个常量")

# 9. 测试一个简单的超分
print("\n9. 测试图片超分...")
test_image = r"D:\1VSCODE\Projects\ImageAll\NeeWaifu\NeeView\test_input.png"

if Path(test_image).exists():
    print(f"   读取测试图片: {test_image}")
    with open(test_image, 'rb') as f:
        image_data = f.read()
    print(f"   图片大小: {len(image_data)} bytes")
    
    # 尝试添加任务
    print(f"\n   尝试使用 MODEL_WAIFU2X_ANIME_UP2X (假设ID=18)...")
    try:
        # 获取模型 ID
        model_id = sr_vulkan.MODEL_WAIFU2X_ANIME_UP2X
        print(f"   模型 ID: {model_id}")
        
        task_id = 12345
        scale = 2
        
        print(f"   调用 sr.add(data, model={model_id}, taskId={task_id}, scale={scale}, format='png')...")
        proc_id = sr.add(image_data, model_id, task_id, scale, format='png')
        print(f"   ✓ sr.add() 返回 procId: {proc_id}")
        
        if proc_id < 0:
            print(f"   ✗ procId < 0, 表示错误!")
            try:
                error = sr.getLastError()
                print(f"   错误信息: {error}")
            except:
                print(f"   无法获取错误信息")
        else:
            print(f"   ✓ 任务添加成功,开始轮询...")
            
            # 轮询结果
            for i in range(100):
                result = sr.load(0)
                if result is not None:
                    data, fmt, ret_task_id, tick = result
                    print(f"   ✓ 处理完成!")
                    print(f"      返回 taskId: {ret_task_id}")
                    print(f"      耗时: {tick:.2f}s")
                    print(f"      输出大小: {len(data)} bytes")
                    
                    # 保存结果
                    output_path = r"D:\1VSCODE\Projects\ImageAll\NeeWaifu\NeeView\test_output.png"
                    with open(output_path, 'wb') as f:
                        f.write(data)
                    print(f"      保存到: {output_path}")
                    break
                
                if i % 10 == 0:
                    print(f"   轮询中... ({i}/100)")
                time.sleep(0.1)
            else:
                print(f"   ✗ 超时,未获取到结果")
                
    except Exception as e:
        print(f"   ✗ 超分失败: {e}")
        import traceback
        traceback.print_exc()
else:
    print(f"   ✗ 测试图片不存在: {test_image}")
    print(f"   提示: 请手动放一张 test_input.png 到脚本目录")

print("\n=== 测试完成 ===")
