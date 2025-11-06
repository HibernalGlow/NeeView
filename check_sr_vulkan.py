"""
检查 sr_vulkan 和模型文件安装状态
"""
import sys
import os

# Windows CMD 编码修复
if sys.platform == 'win32':
    import io
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

print("=== Python 环境检查 ===")
print(f"Python 版本: {sys.version}")
print(f"Python 路径: {sys.executable}")
print()

print("=== 检查 sr_vulkan 安装 ===")
try:
    import sr_vulkan
    print(f"✅ sr_vulkan 已安装")
    print(f"   位置: {sr_vulkan.__file__}")
    
    # 检查版本
    try:
        from sr_vulkan import sr_vulkan as sr
        version = sr.getVersion()
        print(f"   版本: {version}")
    except Exception as e:
        print(f"   ⚠️ 无法获取版本: {e}")
    
    # 检查 GPU
    try:
        gpuInfo = sr.getGpuInfo()
        print(f"   GPU 信息: {gpuInfo}")
    except Exception as e:
        print(f"   ⚠️ 无法获取 GPU 信息: {e}")
        
except ImportError as e:
    print(f"❌ sr_vulkan 未安装: {e}")
    print()
    print("请运行: pip install sr-vulkan")
    sys.exit(1)

print()
print("=== 检查模型包安装 ===")

# 检查已安装的模型包
model_packages = [
    "sr-vulkan-model-waifu2x",
    "sr-vulkan-model-realesrgan",
    "sr-vulkan-model-realcugan"
]

for pkg in model_packages:
    try:
        __import__(pkg.replace("-", "_"))
        print(f"✅ {pkg} 已安装")
    except ImportError:
        print(f"❌ {pkg} 未安装")

print()
print("=== 检查模型文件位置 ===")

# 查找模型文件
possible_paths = [
    os.path.expanduser("~/.cache/sr-vulkan"),
    os.path.join(sys.prefix, "share", "sr-vulkan"),
    os.path.join(os.path.dirname(sr_vulkan.__file__), "models"),
]

# 检查 site-packages 中的模型
try:
    import site
    for site_dir in site.getsitepackages():
        possible_paths.append(os.path.join(site_dir, "sr_vulkan", "models"))
        possible_paths.append(os.path.join(site_dir, "sr_vulkan_model_waifu2x"))
except:
    pass

print("搜索路径:")
found_models = False
for path in possible_paths:
    if os.path.exists(path):
        print(f"\n✅ 找到: {path}")
        
        # 列出内容
        try:
            files = os.listdir(path)
            if files:
                found_models = True
                print(f"   内容: {len(files)} 个文件/文件夹")
                for f in files[:10]:  # 只显示前10个
                    full_path = os.path.join(path, f)
                    size = os.path.getsize(full_path) if os.path.isfile(full_path) else 0
                    print(f"   - {f} ({size / 1024 / 1024:.2f} MB)" if size > 0 else f"   - {f} (文件夹)")
        except Exception as e:
            print(f"   ⚠️ 无法读取目录: {e}")
    else:
        print(f"❌ 不存在: {path}")

if not found_models:
    print("\n⚠️ 未找到模型文件!")
    print("\n解决方案:")
    print("1. 安装模型包: pip install sr-vulkan-model-waifu2x")
    print("2. 手动下载模型到 ~/.cache/sr-vulkan/")

print()
print("=== 检查可用模型常量 ===")
try:
    from sr_vulkan import sr_vulkan as sr
    
    model_attrs = [attr for attr in dir(sr) if attr.startswith("MODEL_")]
    print(f"找到 {len(model_attrs)} 个模型常量:")
    
    # 按类型分组
    waifu2x_models = [m for m in model_attrs if "WAIFU2X" in m]
    realesrgan_models = [m for m in model_attrs if "REALESRGAN" in m]
    realcugan_models = [m for m in model_attrs if "REALCUGAN" in m]
    
    print(f"\nWaifu2x 模型 ({len(waifu2x_models)}):")
    for m in waifu2x_models[:5]:
        print(f"  - {m}")
    if len(waifu2x_models) > 5:
        print(f"  ... 还有 {len(waifu2x_models) - 5} 个")
    
    print(f"\nRealESRGAN 模型 ({len(realesrgan_models)}):")
    for m in realesrgan_models[:5]:
        print(f"  - {m}")
    
    print(f"\nRealCUGAN 模型 ({len(realcugan_models)}):")
    for m in realcugan_models[:5]:
        print(f"  - {m}")
        
except Exception as e:
    print(f"❌ 无法获取模型常量: {e}")

print()
print("=== 测试基本功能 ===")
try:
    from sr_vulkan import sr_vulkan as sr
    import io
    from PIL import Image
    
    # 创建测试图片
    print("创建 100x100 测试图片...")
    img = Image.new('RGB', (100, 100), color='red')
    buf = io.BytesIO()
    img.save(buf, format='PNG')
    test_data = buf.getvalue()
    
    print(f"测试图片大小: {len(test_data)} bytes")
    
    # 初始化
    print("初始化 sr_vulkan...")
    sr.init(0, 0)  # GPU 0, TTA off
    
    # 添加任务
    print("添加超分任务...")
    procId = sr.add(test_data, sr.MODEL_WAIFU2X_CUNET_UP2X, b'png', None)
    print(f"任务 ID: {procId}")
    
    # 等待结果
    print("等待处理结果...")
    import time
    max_wait = 30
    for i in range(max_wait):
        result = sr.load(procId)
        if result:
            print(f"✅ 处理成功! 耗时: {i}秒")
            print(f"输出大小: {len(result)} bytes")
            break
        time.sleep(1)
    else:
        print(f"❌ 处理超时 (等待了 {max_wait} 秒)")
        err = sr.getLastError()
        print(f"错误信息: {err}")
        
except ImportError as e:
    print(f"❌ 缺少依赖: {e}")
    print("请运行: pip install Pillow")
except Exception as e:
    print(f"❌ 测试失败: {e}")
    import traceback
    traceback.print_exc()

print()
print("=== 检查完成 ===")
