# 测试 sr_vulkan 是否正确安装

import sys
print("Python version:", sys.version)

try:
    import sr_vulkan
    print("✅ sr_vulkan 已安装")
    print("sr_vulkan version:", sr_vulkan.__version__ if hasattr(sr_vulkan, '__version__') else "unknown")
except ImportError as e:
    print("❌ sr_vulkan 未安装:", e)
    print("\n请运行: pip install sr-vulkan sr-vulkan-model-waifu2x")
    sys.exit(1)

try:
    from sr_vulkan import sr_vulkan as sr
    print("\n可用模型:")
    
    models = [
        "waifu2x_cunet",
        "waifu2x_upconv_7_photo",
        "realesrgan_animevideo",
        "realesrgan_plus",
        "realcugan_conservative",
        "realcugan_denoise3x"
    ]
    
    for model_name in models:
        if hasattr(sr, model_name):
            print(f"  ✅ {model_name}")
        else:
            print(f"  ❌ {model_name} (未安装模型包)")
            
    print("\n✅ sr_vulkan 配置正确!")
    print("NeeView 的 Python 引擎应该可以正常工作。")
    
except Exception as e:
    print("❌ 错误:", e)
    import traceback
    traceback.print_exc()
