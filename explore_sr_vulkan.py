# 探索 sr_vulkan 的实际 API

from sr_vulkan import sr_vulkan as sr
import inspect

print("sr_vulkan 模块内容:")
print("=" * 60)

# 列出所有属性和方法
all_items = dir(sr)
print("\n所有项目:")
for item in all_items:
    if not item.startswith('_'):
        print(f"  - {item}")

# 查找模型类
print("\n\n模型类:")
print("=" * 60)
for item in all_items:
    if not item.startswith('_'):
        obj = getattr(sr, item)
        if inspect.isclass(obj):
            print(f"\n{item}:")
            print(f"  类型: {type(obj)}")
            # 尝试获取构造函数签名
            try:
                sig = inspect.signature(obj.__init__)
                print(f"  __init__{sig}")
            except:
                pass
            # 列出方法
            methods = [m for m in dir(obj) if not m.startswith('_') and callable(getattr(obj, m))]
            if methods:
                print(f"  方法: {', '.join(methods[:5])}")

# 查找函数
print("\n\n函数:")
print("=" * 60)
for item in all_items:
    if not item.startswith('_'):
        obj = getattr(sr, item)
        if inspect.isfunction(obj) or inspect.isbuiltin(obj):
            print(f"\n{item}:")
            try:
                sig = inspect.signature(obj)
                print(f"  签名: {sig}")
            except:
                print(f"  类型: {type(obj)}")
