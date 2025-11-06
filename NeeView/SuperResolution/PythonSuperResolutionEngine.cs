using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Python.Runtime;

namespace NeeView.SuperResolution
{
    /// <summary>
    /// 基于 Python sr-vulkan 库的超分辨率引擎
    /// 直接调用 picacg-qt 使用的同款 Python 库实现真实超分辨率
    /// 
    /// 需要:
    /// 1. Python 3.10/3.11 已安装
    /// 2. pip install sr-vulkan sr-vulkan-model-waifu2x
    /// 3. (可选) 配置 Python 路径到 NeeView 设置
    /// 
    /// 参考: picacg-qt/src/view/tool/waifu2x_tool_view.py
    /// </summary>
    public class PythonSuperResolutionEngine : ISuperResolutionEngine, IDisposable
    {
        private bool _isInitialized;
        private bool _isModelLoaded;
        private string _lastError = "";
        private dynamic? _srModule;
        private SuperResolutionModel _loadedModel;
        private readonly object _pythonLock = new object();

        public string Name => "Python sr-vulkan";
        public string Version => "1.0.0";
        
        // 当前返回 false,提示用户需要先配置 Python
        public bool IsAvailable => CheckPythonAvailability();

        public SuperResolutionModel[] SupportedModels => new[]
        {
            SuperResolutionModel.Waifu2xAnime2x,
            SuperResolutionModel.Waifu2xAnime4x,
            SuperResolutionModel.Waifu2xPhoto2x,
            SuperResolutionModel.Waifu2xPhoto4x,
            SuperResolutionModel.RealESRGANAnime4x,
            SuperResolutionModel.RealESRGANGeneral4x,
            SuperResolutionModel.RealCUGANAnime2x,
            SuperResolutionModel.RealCUGANAnime3x,
            SuperResolutionModel.RealCUGANAnime4x,
        };

        public async Task<bool> InitializeAsync(int gpuId = 0)
        {
            if (_isInitialized) return true;

            SuperResolutionLogger.Info("=== 开始初始化 Python 超分辨率引擎 ===");

            return await Task.Run(() =>
            {
                lock (_pythonLock)
                {
                    try
                    {
                        // 初始化 Python 引擎
                        if (!PythonEngine.IsInitialized)
                        {
                            SuperResolutionLogger.Info("正在查找 Python 安装...");
                            var pythonDll = FindPythonDll();
                            if (string.IsNullOrEmpty(pythonDll))
                            {
                                _lastError = "未找到 Python 安装。请安装 Python 3.10 或 3.11。";
                                SuperResolutionLogger.Error(_lastError);
                                return false;
                            }

                            SuperResolutionLogger.Info($"找到 Python DLL: {pythonDll}");
                            Python.Runtime.Runtime.PythonDLL = pythonDll;
                            
                            SuperResolutionLogger.Info("初始化 Python 引擎...");
                            PythonEngine.Initialize();
                            PythonEngine.BeginAllowThreads();
                            SuperResolutionLogger.Info("Python 引擎初始化成功");
                        }

                        using (Py.GIL())
                        {
                            // 导入 sr_vulkan 模块
                            try
                            {
                                SuperResolutionLogger.Info("正在导入 sr_vulkan 模块...");
                                _srModule = Py.Import("sr_vulkan.sr_vulkan");
                                SuperResolutionLogger.Info("sr_vulkan 模块导入成功");
                                
                                // 获取 sr_vulkan 版本信息
                                try
                                {
                                    dynamic version = _srModule.getVersion();
                                    SuperResolutionLogger.Info($"sr_vulkan 版本: {version}");
                                }
                                catch (Exception ex)
                                {
                                    SuperResolutionLogger.Warning($"无法获取 sr_vulkan 版本: {ex.Message}");
                                }

                                // 获取 GPU 信息
                                try
                                {
                                    dynamic gpuInfo = _srModule.getGpuInfo();
                                    SuperResolutionLogger.Info($"GPU 信息: {gpuInfo}");
                                }
                                catch (Exception ex)
                                {
                                    SuperResolutionLogger.Warning($"无法获取 GPU 信息: {ex.Message}");
                                }
                                
                                // 如果配置了模型路径,设置模型路径
                                var config = SuperResolutionConfig.Current;
                                string? modelPath = null;
                                
                                if (!string.IsNullOrEmpty(config.ModelPath) && Directory.Exists(config.ModelPath))
                                {
                                    modelPath = config.ModelPath;
                                    SuperResolutionLogger.Info($"使用用户指定的模型路径: {modelPath}");
                                }
                                else
                                {
                                    // 尝试使用 Python 包内的模型路径
                                    try
                                    {
                                        dynamic sys = Py.Import("sys");
                                        dynamic pathList = sys.path;
                                        
                                        // 查找 sr_vulkan_model_waifu2x 包路径
                                        foreach (dynamic path in pathList)
                                        {
                                            string pathStr = path.ToString();
                                            string modelsDir = Path.Combine(pathStr, "sr_vulkan_model_waifu2x", "models");
                                            if (Directory.Exists(modelsDir))
                                            {
                                                modelPath = modelsDir;
                                                SuperResolutionLogger.Info($"自动检测到模型路径: {modelPath}");
                                                break;
                                            }
                                        }
                                        
                                        if (string.IsNullOrEmpty(modelPath))
                                        {
                                            SuperResolutionLogger.Warning("未找到 sr_vulkan_model_waifu2x 模型路径,使用默认路径 ~/.cache/sr-vulkan/");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        SuperResolutionLogger.Warning($"检测模型路径失败: {ex.Message}");
                                    }
                                }
                                
                                // 设置模型路径
                                if (!string.IsNullOrEmpty(modelPath))
                                {
                                    try
                                    {
                                        SuperResolutionLogger.Info($"设置模型路径: {modelPath}");
                                        dynamic builtins = Py.Import("builtins");
                                        dynamic pathStr = builtins.str(modelPath);
                                        _srModule.setModelPath(pathStr);
                                        SuperResolutionLogger.Info("模型路径设置成功");
                                    }
                                    catch (Exception ex)
                                    {
                                        // setModelPath 可能不存在,忽略错误
                                        SuperResolutionLogger.Warning($"设置模型路径失败: {ex.Message}");
                                    }
                                }
                                
                                _isInitialized = true;
                                SuperResolutionLogger.Info("=== Python 超分辨率引擎初始化完成 ===");
                                return true;
                            }
                            catch (PythonException ex)
                            {
                                _lastError = $"无法导入 sr_vulkan 模块:\n{ex.Message}\n\n请运行: pip install sr-vulkan sr-vulkan-model-waifu2x";
                                SuperResolutionLogger.Error(_lastError, ex);
                                return false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _lastError = $"Python 初始化失败: {ex.Message}";
                        SuperResolutionLogger.Error(_lastError, ex);
                        return false;
                    }
                }
            });
        }

        public async Task<bool> LoadModelAsync(SuperResolutionModel model, string modelPath)
        {
            if (!_isInitialized)
            {
                _lastError = "引擎未初始化";
                return false;
            }

            return await Task.Run(() =>
            {
                try
                {
                    _loadedModel = model;
                    _isModelLoaded = true;
                    return true;
                }
                catch (Exception ex)
                {
                    _lastError = $"模型加载失败: {ex.Message}";
                    return false;
                }
            });
        }

        public async Task<byte[]> ProcessAsync(
            byte[] inputData,
            double scale,
            int denoise = -1,
            bool tta = false,
            int tileSize = 0,
            CancellationToken cancellationToken = default)
        {
            SuperResolutionLogger.Info($"=== 开始处理图片 ===");
            SuperResolutionLogger.Info($"输入大小: {inputData.Length / 1024.0:F2} KB");
            
            // 检测并转换格式
            var originalFormat = ImageFormatConverter.DetectFormat(inputData);
            SuperResolutionLogger.Info($"输入格式: {originalFormat}");
            
            byte[] processData = inputData;
            bool needsConversion = !ImageFormatConverter.IsNativelySupportedFormat(inputData);
            
            if (needsConversion)
            {
                try
                {
                    SuperResolutionLogger.Warning($"格式 {originalFormat} 需要转换为 PNG");
                    processData = ImageFormatConverter.ConvertToPng(inputData);
                    SuperResolutionLogger.Info($"格式转换完成: {inputData.Length / 1024.0:F2} KB → {processData.Length / 1024.0:F2} KB");
                }
                catch (Exception ex)
                {
                    _lastError = $"格式转换失败: {ex.Message}";
                    SuperResolutionLogger.Error(_lastError, ex);
                    return Array.Empty<byte>();
                }
            }
            
            SuperResolutionLogger.Info($"模型: {_loadedModel}, 缩放: {scale}x, 降噪: {denoise}, TTA: {tta}");

            if (!_isInitialized || _srModule == null)
            {
                _lastError = "引擎未初始化";
                SuperResolutionLogger.Error(_lastError);
                return Array.Empty<byte>();
            }

            if (!_isModelLoaded)
            {
                _lastError = "模型未加载";
                SuperResolutionLogger.Error(_lastError);
                return Array.Empty<byte>();
            }

            var startTime = System.Diagnostics.Stopwatch.StartNew();

            return await Task.Run(() =>
            {
                lock (_pythonLock)
                {
                    try
                    {
                        using (Py.GIL())
                        {
                            // 获取模型常量 (sr_vulkan 使用 MODEL_* 常量)
                            var modelName = GetPythonModelName(_loadedModel);
                            SuperResolutionLogger.Info($"使用模型常量: {modelName}");
                            
                            dynamic modelConstant = _srModule!.GetAttr(modelName);

                            // 转换字节数组为 Python bytes
                            dynamic builtins = Py.Import("builtins");
                            dynamic inputPyBytes = builtins.bytes(processData);
                            SuperResolutionLogger.Info($"已转换输入数据为 Python bytes (处理后大小: {processData.Length} bytes)");

                            // 调用 sr_vulkan.add()
                            // API: add(data:bytes, modelIndex:MODEL, backId:int, scale:float, format:str="", tileSize:int=400)
                            // backId 是任务标识符,可以随意指定
                            int taskId = System.Environment.TickCount;
                            
                            SuperResolutionLogger.Info($"调用 sr_vulkan.add() with backId={taskId}, scale={scale}...");
                            
                            int procId = (int)_srModule.add(
                                inputPyBytes,           // data
                                modelConstant,          // modelIndex
                                taskId,                 // backId (任务ID)
                                scale,                  // scale (缩放倍数)
                                new PyString("png"),    // format
                                tileSize > 0 ? tileSize : 400  // tileSize
                            );
                            
                            SuperResolutionLogger.Info($"sr_vulkan.add() 返回 procId: {procId}");

                            // 轮询等待结果 (最多等待30秒)
                            // load() 返回 Union[None, Tuple[bytes, str, int, float]] = (data, format, backId, tick)
                            SuperResolutionLogger.Info("开始轮询处理结果...");
                            dynamic? result = null;
                            byte[]? processedData = null;
                            int pollCount = 0;
                            for (int i = 0; i < 300; i++)
                            {
                                result = _srModule.load(procId);
                                pollCount++;
                                
                                // 检查是否完成 (load() 返回非 None 即完成)
                                if (result != null)
                                {
                                    SuperResolutionLogger.Info($"处理完成! 轮询次数: {pollCount}, 耗时: {pollCount * 100}ms");
                                    
                                    // result 是一个 tuple (data:bytes, format:str, backId:int, tick:float)
                                    // 提取第一个元素 data
                                    try
                                    {
                                        var resultTuple = result as PyObject;
                                        if (resultTuple != null && resultTuple.Length() >= 1)
                                        {
                                            var dataBytes = resultTuple[0];
                                            processedData = dataBytes.As<byte[]>();
                                            SuperResolutionLogger.Info($"成功提取处理结果, 大小: {processedData.Length} bytes");
                                        }
                                        else
                                        {
                                            SuperResolutionLogger.Error("load() 返回的 tuple 格式不正确");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        SuperResolutionLogger.Error($"提取处理结果失败: {ex.Message}", ex);
                                    }
                                    break;
                                }

                                if (i % 10 == 0 && i > 0)
                                {
                                    SuperResolutionLogger.DebugLog($"仍在处理中... 已轮询 {pollCount} 次 ({pollCount * 100}ms)");
                                }

                                Thread.Sleep(100);
                            }

                            if (result == null || processedData == null)
                            {
                                _lastError = "处理超时或失败";
                                SuperResolutionLogger.Error($"{_lastError} (轮询了 {pollCount} 次)");
                                
                                // 尝试获取错误信息
                                try
                                {
                                    dynamic lastError = _srModule.getLastError();
                                    SuperResolutionLogger.Error($"sr_vulkan 错误: {lastError}");
                                }
                                catch
                                {
                                    SuperResolutionLogger.Warning("无法获取 sr_vulkan 错误信息");
                                }
                                
                                return Array.Empty<byte>();
                            }

                            // 返回处理后的数据
                            startTime.Stop();
                            
                            SuperResolutionLogger.Info($"=== 处理完成 ===");
                            SuperResolutionLogger.Info($"输出大小: {processedData!.Length / 1024.0:F2} KB");
                            SuperResolutionLogger.Info($"总耗时: {startTime.ElapsedMilliseconds}ms");
                            
                            return processedData;
                        }
                    }
                    catch (PythonException ex)
                    {
                        _lastError = $"Python 处理错误:\n{ex.Message}\n\n{ex.StackTrace}";
                        SuperResolutionLogger.Error(_lastError, ex);
                        return Array.Empty<byte>();
                    }
                    catch (Exception ex)
                    {
                        _lastError = $"处理失败: {ex.Message}";
                        SuperResolutionLogger.Error(_lastError, ex);
                        return Array.Empty<byte>();
                    }
                }
            }, cancellationToken);
        }

        public async Task<byte[]> ProcessToSizeAsync(
            byte[] inputData,
            int targetWidth,
            int targetHeight,
            int denoise = -1,
            bool tta = false,
            int tileSize = 0,
            CancellationToken cancellationToken = default)
        {
            if (!_isInitialized || _srModule == null)
            {
                _lastError = "引擎未初始化";
                return Array.Empty<byte>();
            }

            if (!_isModelLoaded)
            {
                _lastError = "模型未加载";
                return Array.Empty<byte>();
            }

            return await Task.Run(() =>
            {
                lock (_pythonLock)
                {
                    try
                    {
                        using (Py.GIL())
                        {
                            // 获取 builtins 和 None
                            dynamic builtins = Py.Import("builtins");
                            dynamic pyNone = builtins.None;

                            var modelName = GetPythonModelName(_loadedModel);
                            dynamic modelClass = _srModule!.GetAttr(modelName);

                            using var pyDict = new PyDict();
                            pyDict["model"] = modelClass;
                            pyDict["width"] = new PyInt(targetWidth);
                            pyDict["high"] = new PyInt(targetHeight); // 注意: picacg-qt 用 "high" 不是 "height"

                            if (denoise >= 0)
                            {
                                pyDict["noise"] = new PyInt(denoise);
                            }

                            using var inputBytes = new PyString(Convert.ToBase64String(inputData));
                            dynamic result = CallSrVulkanProcess(inputBytes, pyDict);
                            
                            if (result == null)
                            {
                                _lastError = "处理返回空结果";
                                return Array.Empty<byte>();
                            }

                            return result.As<byte[]>();
                        }
                    }
                    catch (PythonException ex)
                    {
                        _lastError = $"Python 处理错误:\n{ex.Message}\n\n{ex.StackTrace}";
                        return Array.Empty<byte>();
                    }
                    catch (Exception ex)
                    {
                        _lastError = $"处理失败: {ex.Message}";
                        return Array.Empty<byte>();
                    }
                }
            }, cancellationToken);
        }

        public string GetLastError() => _lastError;

        public void Dispose()
        {
            lock (_pythonLock)
            {
                try
                {
                    if (_isInitialized)
                    {
                        using (Py.GIL())
                        {
                            _srModule = null;
                        }
                    }
                }
                catch { }

                _isInitialized = false;
                _isModelLoaded = false;
            }
        }

        #region 辅助方法

        /// <summary>
        /// 检查 Python 是否可用
        /// </summary>
        private bool CheckPythonAvailability()
        {
            var pythonDll = FindPythonDll();
            return !string.IsNullOrEmpty(pythonDll);
        }

        /// <summary>
        /// 查找 Python DLL
        /// </summary>
        private string FindPythonDll()
        {
            // 1. 优先使用配置的路径
            var configPath = SuperResolutionConfig.Current.PythonPath;
            if (!string.IsNullOrEmpty(configPath))
            {
                var dll = TryGetPythonDll(configPath);
                if (!string.IsNullOrEmpty(dll)) return dll;
            }

            // 2. 尝试从 PATH 环境变量查找
            var pathDirs = System.Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? Array.Empty<string>();
            foreach (var dir in pathDirs)
            {
                if (string.IsNullOrWhiteSpace(dir)) continue;
                
                var dll = TryGetPythonDll(dir);
                if (!string.IsNullOrEmpty(dll)) return dll;
            }

            // 3. 尝试常见安装位置
            var commonPaths = new[]
            {
                @"C:\Python311",
                @"C:\Python310",
                @"C:\Python39",
                Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", "Python311"),
                Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", "Python310"),
            };

            foreach (var basePath in commonPaths)
            {
                var dll = TryGetPythonDll(basePath);
                if (!string.IsNullOrEmpty(dll)) return dll;
            }

            return string.Empty;
        }

        /// <summary>
        /// 尝试从目录获取 Python DLL
        /// </summary>
        private string TryGetPythonDll(string directory)
        {
            if (!Directory.Exists(directory)) return string.Empty;

            // 查找 python3X.dll (Python 3.10, 3.11, 3.12)
            var dlls = new[] { "python311.dll", "python310.dll", "python312.dll", "python39.dll" };
            
            foreach (var dll in dlls)
            {
                var dllPath = Path.Combine(directory, dll);
                if (File.Exists(dllPath)) return dllPath;
            }

            return string.Empty;
        }

        /// <summary>
        /// 将 C# 模型枚举转换为 Python sr_vulkan 模型名
        /// 参考: sr_vulkan 库的模型命名
        /// </summary>
        private string GetPythonModelName(SuperResolutionModel model)
        {
            return model switch
            {
                // Waifu2x 系列
                SuperResolutionModel.Waifu2xAnime2x => "waifu2x_cunet",
                SuperResolutionModel.Waifu2xAnime4x => "waifu2x_cunet",
                SuperResolutionModel.Waifu2xPhoto2x => "waifu2x_upconv_7_photo",
                SuperResolutionModel.Waifu2xPhoto4x => "waifu2x_upconv_7_photo",
                
                // RealESRGAN 系列
                SuperResolutionModel.RealESRGANAnime4x => "realesrgan_animevideo",
                SuperResolutionModel.RealESRGANGeneral4x => "realesrgan_plus",
                
                // RealCUGAN 系列
                SuperResolutionModel.RealCUGANAnime2x => "realcugan_conservative",
                SuperResolutionModel.RealCUGANAnime3x => "realcugan_denoise3x",
                SuperResolutionModel.RealCUGANAnime4x => "realcugan_conservative",
                
                _ => throw new ArgumentException($"不支持的模型: {model}")
            };
        }

        /// <summary>
        /// 调用 sr_vulkan 处理函数
        /// 这个方法需要根据实际 sr_vulkan API 调整
        /// </summary>
        private dynamic CallSrVulkanProcess(PyObject inputBytes, PyDict parameters)
        {
            if (_srModule == null)
            {
                throw new InvalidOperationException("sr_vulkan 模块未初始化");
            }

            // sr_vulkan 的实际调用方式需要查看其文档
            // 这里提供两种可能的方式:

            try
            {
                // 方式1: 如果 sr_vulkan 提供了直接的 process 函数
                if (_srModule.HasAttr("process"))
                {
                    return _srModule.process(inputBytes, parameters);
                }

                // 方式2: 创建模型实例并调用
                dynamic modelClass = parameters["model"];
                dynamic modelInstance = modelClass();
                
                // 假设模型有 process 或 __call__ 方法
                if (modelInstance.HasAttr("process"))
                {
                    return modelInstance.process(inputBytes, parameters);
                }
                else
                {
                    // 直接调用模型 (如果实现了 __call__)
                    return modelInstance(inputBytes, parameters);
                }
            }
            catch (Exception ex)
            {
                _lastError = $"调用 sr_vulkan 失败: {ex.Message}\n\n" +
                            $"这可能是因为 sr_vulkan API 与预期不同。\n" +
                            $"请检查 sr_vulkan 文档或使用 Mock 引擎测试。";
                throw;
            }
        }

        #endregion
    }
}
