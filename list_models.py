from sr_vulkan import sr_vulkan as sr

models = [(x, getattr(sr, x)) for x in dir(sr) if 'MODEL_WAIFU2X' in x]
for name, val in sorted(models, key=lambda x: x[1]):
    print(f'{val:3d} : {name}')
