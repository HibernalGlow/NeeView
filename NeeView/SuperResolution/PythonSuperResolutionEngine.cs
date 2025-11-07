using System;
using System.Collections.Generic;
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
        private readonly List<SuperResolutionDeviceInfo> _devices = new();
        private readonly List<SuperResolutionModelInfo> _availableModels = new();

        public string Name => "Python sr-vulkan";
        public string Version => "1.0.0";
        
        // 当前返回 false,提示用户需要先配置 Python
        public bool IsAvailable => CheckPythonAvailability();

        // 🔥 移除硬编码的模型列表,改为动态获取
        public SuperResolutionModel[] SupportedModels => new SuperResolutionModel[0];
        
        /// <summary>
        /// 动态获取的可用模型列表 (模型名, 模型ID)
        /// </summary>
        public IReadOnlyList<SuperResolutionModelInfo> AvailableModels => _availableModels;

        public IReadOnlyList<SuperResolutionDeviceInfo> AvailableDevices => _devices;

        private void ResetDeviceList()
        {
            _devices.Clear();
            _devices.Add(new SuperResolutionDeviceInfo(-1, "CPU (sr_vulkan)", "回退模式"));
        }

        private void EnsureDefaultGpuEntry()
        {
            if (!_devices.Any(device => device.Id >= 0))
            {
                _devices.Add(new SuperResolutionDeviceInfo(0, "默认 GPU", "sr_vulkan"));
            }
        }

        private void UpdateDeviceList(dynamic gpuInfoObject)
        {
            try
            {
                var infoText = gpuInfoObject?.ToString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(infoText))
                {
                    EnsureDefaultGpuEntry();
                    return;
                }

                var lines = infoText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var hasGpuEntry = false;

                foreach (var rawLine in lines)
                {
                    var line = rawLine.Trim();
                    if (!line.StartsWith("[") || !line.Contains("]"))
                    {
                        continue;
                    }

                    var endBracket = line.IndexOf(']');
                    if (endBracket <= 1)
                    {
                        continue;
                    }

                    var inside = line.Substring(1, endBracket - 1).Trim();
                    var spaceIndex = inside.IndexOf(' ');
                    if (spaceIndex <= 0)
                    {
                        continue;
                    }

                    var idPart = inside.Substring(0, spaceIndex).Trim();
                    if (!int.TryParse(idPart, out int gpuId))
                    {
                        continue;
                    }

                    var name = inside.Substring(spaceIndex + 1).Trim();
                    if (string.IsNullOrEmpty(name))
                    {
                        name = $"GPU {gpuId}";
                    }

                    var description = line.Substring(endBracket + 1).Trim();

                    _devices.Add(new SuperResolutionDeviceInfo(gpuId, name, description));
                    hasGpuEntry = true;
                }

                if (!hasGpuEntry)
                {
                    EnsureDefaultGpuEntry();
                }
            }
            catch (Exception ex)
            {
                SuperResolutionLogger.Warning($"解析 GPU 信息失败: {ex.Message}");
                EnsureDefaultGpuEntry();
            }
        }

        /// <summary>
        /// 动态读取sr-vulkan模块的所有MODEL_*常量
        /// </summary>
        private void DiscoverAvailableModels()
        {
            if (_srModule == null)
            {
                SuperResolutionLogger.Warning("sr_vulkan 模块未初始化,无法读取模型列表");
                return;
            }

            _availableModels.Clear();

            try
            {
                // 获取模块的所有属性名
                dynamic dirResult = _srModule.InvokeMethod("__dir__");
                var attributes = new List<string>();
                
                foreach (PyObject attr in dirResult)
                {
                    var attrName = attr.ToString();
                    if (attrName != null && attrName.StartsWith("MODEL_", StringComparison.Ordinal))
                    {
                        attributes.Add(attrName);
                    }
                }

                SuperResolutionLogger.Info($"找到 {attributes.Count} 个模型常量");

                // 读取每个模型的ID值
                foreach (var modelName in attributes.OrderBy(x => x))
                {
                    try
                    {
                        dynamic modelIdObj = _srModule.GetAttr(modelName);
                        int modelId = (int)modelIdObj;
                        
                        var modelInfo = new SuperResolutionModelInfo(modelName, modelId);
                        _availableModels.Add(modelInfo);
                        
                        SuperResolutionLogger.DebugLog($"  {modelName} = {modelId} (scale={modelInfo.Scale}x, denoise={modelInfo.DenoiseLevel})");
                    }
                    catch (Exception ex)
                    {
                        SuperResolutionLogger.Warning($"读取模型 {modelName} 失败: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                SuperResolutionLogger.Error($"读取模型列表失败: {ex.Message}", ex);
            }
        }

        public async Task<bool> InitializeAsync(int gpuId = 0)
        {
            if (_isInitialized) return true;

            SuperResolutionLogger.Info("=== 开始初始化 Python 超分辨率引擎 ===");

            return await Task.Run(() =>
            {
                lock (_pythonLock)
                {
                    ResetDeviceList();

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
                                
                                // ===== 关键发现 =====
                                // sr-vulkan 会自动从已安装的 Python 包中查找模型文件,无需手动设置路径!
                                // 只要通过 pip 安装了 sr_vulkan_model_waifu2x, sr_vulkan_model_realesrgan 等包,
                                // sr-vulkan 会自动在 site-packages 中找到它们的 models 子目录。
                                // ⚠️ 调用 setModelPath() 反而会导致 sr.add() 返回 -20 (invalid model index)!
                                //
                                // 经过测试验证:
                                // ✅ 不调用 setModelPath: 所有模型正常 (procId > 0)
                                // ❌ 调用 setModelPath: 所有模型失败 (procId = -20)
                                //
                                // 所以这里删除了所有 setModelPath() 调用,让 sr-vulkan 自动处理。
                                
                                // ===== 步骤 1: 基础初始化 =====
                                SuperResolutionLogger.Info("调用 sr.init()...");
                                int initResult = (int)_srModule.init();
                                SuperResolutionLogger.Info($"sr.init() 返回: {initResult}");
                                
                                if (initResult < 0)
                                {
                                    SuperResolutionLogger.Warning($"GPU 初始化返回负值 (可能使用 CPU 模式): {initResult}");
                                }
                                
                                // 启用调试模式
                                try
                                {
                                    _srModule.setDebug(true);
                                    SuperResolutionLogger.Info("已启用 sr_vulkan 调试模式");
                                }
                                catch
                                {
                                    SuperResolutionLogger.Warning("setDebug 方法不可用");
                                }
                                
                                // ===== 步骤 2: 设置 GPU 和线程数 (关键!会加载模型) =====
                                // 参考 picacg-qt: sr.initSet(config.Encode, config.UseCpuNum)
                                SuperResolutionLogger.Info($"调用 sr.initSet({gpuId}, 0)...");
                                int initSetResult = (int)_srModule.initSet(gpuId, 0);  // 0 = 自动线程数
                                SuperResolutionLogger.Info($"sr.initSet() 返回: {initSetResult}");
                                
                                if (initSetResult < 0)
                                {
                                    _lastError = $"sr.initSet 失败,返回码: {initSetResult}";
                                    SuperResolutionLogger.Error(_lastError);
                                    return false;
                                }
                                
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
                                    UpdateDeviceList(gpuInfo);
                                }
                                catch (Exception ex)
                                {
                                    SuperResolutionLogger.Warning($"无法获取 GPU 信息: {ex.Message}");
                                    EnsureDefaultGpuEntry();
                                }
                                
                                // 🚀 动态读取所有可用模型
                                try
                                {
                                    SuperResolutionLogger.Info("正在读取可用模型列表...");
                                    DiscoverAvailableModels();
                                    SuperResolutionLogger.Info($"发现 {_availableModels.Count} 个可用模型");
                                }
                                catch (Exception ex)
                                {
                                    SuperResolutionLogger.Warning($"读取模型列表失败: {ex.Message}");
                                }
                                
                                _isInitialized = true;
                                if (_devices.Count > 0)
                                {
                                    var summary = string.Join(", ", _devices.Select(device => device.DisplayName));
                                    SuperResolutionLogger.Info($"可用设备: {summary}");
                                }
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

            // sr-vulkan 2.0.1 在 initSet() 时已加载所有模型到 GPU
            // 这里只需要记录当前使用的模型即可
            return await Task.Run(() =>
            {
                try
                {
                    _loadedModel = model;
                    _isModelLoaded = true;
                    SuperResolutionLogger.Info($"切换模型: {model}");
                    return true;
                }
                catch (Exception ex)
                {
                    _lastError = $"模型切换失败: {ex.Message}";
                    SuperResolutionLogger.Error(_lastError, ex);
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
            
            SuperResolutionLogger.Info($"模型: {_loadedModel}, 降噪: {denoise}, TTA: {tta}");
            
            // 🔥 智能缩放倍数选择:
            // 1. 如果配置了CustomScaleFactor(>0),优先使用
            // 2. 否则从模型名称自动提取 (如 UP2X→2, UP4X→4)
            int actualScale;
            var config = SuperResolutionConfig.Current;
            if (config != null && config.CustomScaleFactor > 0)
            {
                actualScale = config.CustomScaleFactor;
                SuperResolutionLogger.Info($"使用自定义缩放倍数: {actualScale}x");
            }
            else
            {
                actualScale = GetScaleFromModel(_loadedModel);
                SuperResolutionLogger.Info($"使用模型默认缩放倍数: {actualScale}x (从 {_loadedModel} 提取)");
            }

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
                            // 获取模型常量 (sr_vulkan 使用整数 ID, 例如 MODEL_WAIFU2X_ANIME_UP2X = 18)
                            var modelName = GetPythonModelName(_loadedModel);
                            SuperResolutionLogger.Info($"使用模型常量: {modelName}");
                            
                            dynamic modelConstant = _srModule!.GetAttr(modelName);
                            int modelId = (int)modelConstant;
                            SuperResolutionLogger.Info($"模型 ID: {modelId}");

                            // 转换字节数组为 Python bytes
                            dynamic builtins = Py.Import("builtins");
                            dynamic inputPyBytes = builtins.bytes(processData);
                            SuperResolutionLogger.Info($"已转换输入数据为 Python bytes (处理后大小: {processData.Length} bytes)");

                            // 调用 sr_vulkan.add()
                            // 参考 picacg-qt: sr.add(data, model_id, task_id, scale, format=mat, tileSize=tileSize)
                            // taskId 用于匹配返回结果
                            int taskId = System.Environment.TickCount & 0x7FFFFFFF;  // 确保正数
                            
                            SuperResolutionLogger.Info($"调用 sr_vulkan.add() with taskId={taskId}, modelId={modelId}, scale={actualScale}...");
                            
                            // 根据 picacg-qt 的调用方式:
                            // if scale <= 0:
                            //     sr.add(data, model, taskId, width, height, format=mat, tileSize=tileSize)
                            // else:
                            //     sr.add(data, model, taskId, scale, format=mat, tileSize=tileSize)
                            
                            int procId;
                            if (actualScale > 0)
                            {
                                // 使用 scale 模式
                                // 参考 picacg-qt: sr.add(data, model, taskId, scale, format=mat, tileSize=tileSize)
                                if (tileSize > 0)
                                {
                                    procId = (int)_srModule.add(
                                        inputPyBytes,           // data
                                        new PyInt(modelId),     // model
                                        new PyInt(taskId),      // taskId (backId)
                                        new PyInt(actualScale), // scale (使用从模型提取的倍数)
                                        format: new PyString("png"),
                                        tileSize: new PyInt(tileSize)
                                    );
                                }
                                else
                                {
                                    // 不指定 tileSize,使用默认值
                                    procId = (int)_srModule.add(
                                        inputPyBytes,           // data
                                        new PyInt(modelId),     // model
                                        new PyInt(taskId),      // taskId (backId)
                                        new PyInt(actualScale), // scale (使用从模型提取的倍数)
                                        format: new PyString("png")
                                    );
                                }
                            }
                            else
                            {
                                // 使用固定尺寸模式 (暂不支持)
                                throw new NotSupportedException("固定尺寸模式暂不支持");
                            }
                            
                            SuperResolutionLogger.Info($"sr_vulkan.add() 返回 procId: {procId}");

                            // 🔥 检查 procId 是否为错误码 (负数表示错误)
                            if (procId < 0)
                            {
                                _lastError = $"sr_vulkan.add() 失败,错误码: {procId}";
                                SuperResolutionLogger.Error(_lastError);
                                
                                // 尝试获取详细错误信息
                                try
                                {
                                    dynamic lastError = _srModule.getLastError();
                                    var errorMsg = lastError?.ToString();
                                    if (!string.IsNullOrEmpty(errorMsg))
                                    {
                                        _lastError += $"\n详细错误: {errorMsg}";
                                        SuperResolutionLogger.Error($"sr_vulkan 错误详情: {errorMsg}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    SuperResolutionLogger.Warning($"无法获取 sr_vulkan 错误信息: {ex.Message}");
                                }
                                
                                return Array.Empty<byte>();
                            }

                            // 轮询等待结果 (最多等待30秒)
                            // load(0) 返回 (data:bytes, format:str, taskId:int, tick:float) 或 None
                            SuperResolutionLogger.Info("开始轮询处理结果...");
                            dynamic? result = null;
                            byte[]? processedData = null;
                            int pollCount = 0;
                            for (int i = 0; i < 300; i++)
                            {
                                // 🔥 检查取消令牌
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    SuperResolutionLogger.Warning($"处理被取消 (轮询了 {pollCount} 次)");
                                    cancellationToken.ThrowIfCancellationRequested();
                                }
                                
                                result = _srModule.load(0);  // 参数 0 表示获取任何完成的任务
                                pollCount++;
                                
                                // 检查是否完成
                                if (result != null)
                                {
                                    // result 是元组: (data, format, returnedTaskId, tick)
                                    try
                                    {
                                        var resultTuple = result as PyObject;
                                        if (resultTuple != null && resultTuple.Length() >= 4)
                                        {
                                            var dataBytes = resultTuple[0];
                                            var formatStr = resultTuple[1];
                                            int returnedTaskId = resultTuple[2].As<int>();
                                            float tickFloat = resultTuple[3].As<float>();
                                            
                                            SuperResolutionLogger.Info($"获取到完成任务: taskId={returnedTaskId}, tick={tickFloat:F2}s");
                                            
                                            // 验证 taskId 匹配
                                            if (returnedTaskId == taskId)
                                            {
                                                processedData = dataBytes.As<byte[]>();
                                                SuperResolutionLogger.Info($"处理完成! 轮询次数: {pollCount}, 输出大小: {processedData.Length} bytes");
                                                break;
                                            }
                                            else
                                            {
                                                SuperResolutionLogger.Warning($"taskId 不匹配 (期望:{taskId}, 实际:{returnedTaskId}), 继续等待...");
                                            }
                                        }
                                        else
                                        {
                                            SuperResolutionLogger.Error($"load() 返回的 tuple 格式不正确 (长度:{resultTuple?.Length() ?? 0})");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        SuperResolutionLogger.Error($"提取处理结果失败: {ex.Message}", ex);
                                    }
                                }

                                if (i % 10 == 0 && i > 0)
                                {
                                    SuperResolutionLogger.DebugLog($"仍在处理中... 已轮询 {pollCount} 次");
                                }

                                // 动态调整轮询间隔: 前5次50ms快速检查,之后100ms
                                int sleepTime = pollCount < 5 ? 50 : 100;
                                Thread.Sleep(sleepTime);
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
        /// 从模型枚举提取固定的缩放倍数
        /// </summary>
        private int GetScaleFromModel(SuperResolutionModel model)
        {
            return model switch
            {
                // Waifu2x 2x系列
                SuperResolutionModel.Waifu2xAnime2x => 2,
                SuperResolutionModel.Waifu2xAnime2xDenoise0 => 2,
                SuperResolutionModel.Waifu2xAnime2xDenoise1 => 2,
                SuperResolutionModel.Waifu2xAnime2xDenoise2 => 2,
                SuperResolutionModel.Waifu2xAnime2xDenoise3 => 2,
                SuperResolutionModel.Waifu2xPhoto2x => 2,
                SuperResolutionModel.Waifu2xPhoto2xDenoise0 => 2,
                SuperResolutionModel.Waifu2xPhoto2xDenoise1 => 2,
                SuperResolutionModel.Waifu2xPhoto2xDenoise2 => 2,
                SuperResolutionModel.Waifu2xPhoto2xDenoise3 => 2,
                
                // Waifu2x 4x系列 (使用2x模型scale=4)
                SuperResolutionModel.Waifu2xAnime4x => 4,
                SuperResolutionModel.Waifu2xPhoto4x => 4,
                
                // RealESRGAN 系列 (固定4x)
                SuperResolutionModel.RealESRGANAnime4x => 4,
                SuperResolutionModel.RealESRGANGeneral4x => 4,
                
                // RealCUGAN 系列
                SuperResolutionModel.RealCUGANAnime2x => 2,
                SuperResolutionModel.RealCUGANAnime3x => 3,
                SuperResolutionModel.RealCUGANAnime4x => 4,
                
                _ => 2 // 默认2x
            };
        }

        /// <summary>
        /// 将 C# 模型枚举转换为 Python sr_vulkan 模型名
        /// 参考: sr_vulkan 库的模型命名
        /// </summary>
        private string GetPythonModelName(SuperResolutionModel model)
        {
            return model switch
            {
                // Waifu2x 动漫系列 - 2x
                SuperResolutionModel.Waifu2xAnime2x => "MODEL_WAIFU2X_ANIME_UP2X",
                SuperResolutionModel.Waifu2xAnime2xDenoise0 => "MODEL_WAIFU2X_ANIME_UP2X_DENOISE0X",
                SuperResolutionModel.Waifu2xAnime2xDenoise1 => "MODEL_WAIFU2X_ANIME_UP2X_DENOISE1X",
                SuperResolutionModel.Waifu2xAnime2xDenoise2 => "MODEL_WAIFU2X_ANIME_UP2X_DENOISE2X",
                SuperResolutionModel.Waifu2xAnime2xDenoise3 => "MODEL_WAIFU2X_ANIME_UP2X_DENOISE3X",
                
                // Waifu2x 动漫 4x (使用2x模型+scale参数)
                SuperResolutionModel.Waifu2xAnime4x => "MODEL_WAIFU2X_ANIME_UP2X",
                
                // Waifu2x 照片系列 - 2x
                SuperResolutionModel.Waifu2xPhoto2x => "MODEL_WAIFU2X_PHOTO_UP2X",
                SuperResolutionModel.Waifu2xPhoto2xDenoise0 => "MODEL_WAIFU2X_PHOTO_UP2X_DENOISE0X",
                SuperResolutionModel.Waifu2xPhoto2xDenoise1 => "MODEL_WAIFU2X_PHOTO_UP2X_DENOISE1X",
                SuperResolutionModel.Waifu2xPhoto2xDenoise2 => "MODEL_WAIFU2X_PHOTO_UP2X_DENOISE2X",
                SuperResolutionModel.Waifu2xPhoto2xDenoise3 => "MODEL_WAIFU2X_PHOTO_UP2X_DENOISE3X",
                
                // Waifu2x 照片 4x (使用2x模型+scale参数)
                SuperResolutionModel.Waifu2xPhoto4x => "MODEL_WAIFU2X_PHOTO_UP2X",
                
                // RealESRGAN 系列
                SuperResolutionModel.RealESRGANAnime4x => "MODEL_REALESRGAN_X4PLUSANIME_UP4X",
                SuperResolutionModel.RealESRGANGeneral4x => "MODEL_REALESRGAN_X4PLUS_UP4X",
                
                // RealCUGAN 系列
                SuperResolutionModel.RealCUGANAnime2x => "MODEL_REALCUGAN_SE_UP2X_CONSERVATIVE",
                SuperResolutionModel.RealCUGANAnime3x => "MODEL_REALCUGAN_SE_UP3X_DENOISE3X",
                SuperResolutionModel.RealCUGANAnime4x => "MODEL_REALCUGAN_SE_UP4X_CONSERVATIVE",
                
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
