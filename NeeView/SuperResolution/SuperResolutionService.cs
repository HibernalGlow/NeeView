using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NeeView.SuperResolution
{
    /// <summary>
    /// 超分辨率服务实现
    /// </summary>
    public class SuperResolutionService : ISuperResolutionService, IDisposable
    {
        private static readonly Lazy<SuperResolutionService> _instance = new(() => new SuperResolutionService());
        public static SuperResolutionService Current => _instance.Value;

        private readonly ConcurrentDictionary<int, SuperResolutionTask> _tasks = new();
        private readonly SemaphoreSlim _processSemaphore;
        private int _nextTaskId = 1;
        private bool _isInitialized;
        private string _lastError = "";
        private bool _disposed;
        private ISuperResolutionEngine? _engine;

        private SuperResolutionService()
        {
            _processSemaphore = new SemaphoreSlim(2, 2); // 默认最多2个并发任务
        }

        /// <summary>
        /// 是否可用
        /// </summary>
        public bool IsAvailable => _isInitialized;

        /// <summary>
        /// 初始化服务
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized) return true;

            try
            {
                // 创建引擎实例
                _engine = SuperResolutionEngineFactory.GetDefaultEngine();
                
                // 初始化引擎
                _isInitialized = await _engine.InitializeAsync();

                if (!_isInitialized)
                {
                    _lastError = _engine.GetLastError();
                }

                return _isInitialized;
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// 获取最后的错误信息
        /// </summary>
        public string GetLastError() => _lastError;

        /// <summary>
        /// 处理图片数据
        /// </summary>
        public async Task<SuperResolutionResult> ProcessAsync(
            byte[] inputData,
            SuperResolutionConfig config,
            CancellationToken cancellationToken = default)
        {
            if (!_isInitialized || _engine == null)
            {
                return new SuperResolutionResult
                {
                    Success = false,
                    ErrorMessage = "Service not initialized"
                };
            }

            var stopwatch = Stopwatch.StartNew();
            var result = new SuperResolutionResult();

            try
            {
                await _processSemaphore.WaitAsync(cancellationToken);

                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // 加载模型 (如果需要)
                    var modelLoadResult = await _engine.LoadModelAsync(config.Model, "");
                    if (!modelLoadResult)
                    {
                        return new SuperResolutionResult
                        {
                            Success = false,
                            ErrorMessage = $"模型加载失败: {_engine.GetLastError()}"
                        };
                    }

                    // 使用引擎处理图片
                    byte[] outputData;
                    
                    if (config.ScaleMode == ScaleMode.ScaleFactor)
                    {
                        outputData = await _engine.ProcessAsync(
                            inputData,
                            config.ScaleFactor,
                            config.NoiseLevel,
                            config.UseTTA,
                            config.TileSize,
                            cancellationToken);
                    }
                    else
                    {
                        outputData = await _engine.ProcessToSizeAsync(
                            inputData,
                            config.TargetWidth,
                            config.TargetHeight,
                            config.NoiseLevel,
                            config.UseTTA,
                            config.TileSize,
                            cancellationToken);
                    }

                    result.OutputData = outputData;
                    result.Success = outputData != null && outputData.Length > 0;
                    if (!result.Success)
                    {
                        result.ErrorMessage = _engine.GetLastError();
                    }
                }
                finally
                {
                    _processSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                result.Success = false;
                result.ErrorMessage = "Operation cancelled";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _lastError = ex.Message;
            }
            finally
            {
                stopwatch.Stop();
                result.ProcessingTime = stopwatch.Elapsed.TotalSeconds;
            }

            return result;
        }

        /// <summary>
        /// 处理图片文件
        /// </summary>
        public async Task<SuperResolutionResult> ProcessFileAsync(
            string inputPath,
            string outputPath,
            SuperResolutionConfig config,
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(inputPath))
            {
                return new SuperResolutionResult
                {
                    Success = false,
                    ErrorMessage = $"Input file not found: {inputPath}"
                };
            }

            try
            {
                var inputData = await File.ReadAllBytesAsync(inputPath, cancellationToken);
                var result = await ProcessAsync(inputData, config, cancellationToken);

                if (result.Success && result.OutputData != null)
                {
                    var outputDir = Path.GetDirectoryName(outputPath);
                    if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }

                    await File.WriteAllBytesAsync(outputPath, result.OutputData, cancellationToken);
                    result.OutputPath = outputPath;
                }

                return result;
            }
            catch (Exception ex)
            {
                return new SuperResolutionResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// 添加任务到队列
        /// </summary>
        public int AddTask(SuperResolutionTask task)
        {
            task.TaskId = Interlocked.Increment(ref _nextTaskId);
            task.Status = SuperResolutionStatus.Pending;
            _tasks[task.TaskId] = task;

            // 在后台处理任务
            _ = Task.Run(async () => await ProcessTaskAsync(task));

            return task.TaskId;
        }

        /// <summary>
        /// 处理任务
        /// </summary>
        private async Task ProcessTaskAsync(SuperResolutionTask task)
        {
            try
            {
                task.Status = SuperResolutionStatus.Processing;
                task.CancellationTokenSource ??= new CancellationTokenSource();

                SuperResolutionResult result;

                if (task.InputData != null)
                {
                    result = await ProcessAsync(task.InputData, task.Config, task.CancellationTokenSource.Token);
                }
                else if (!string.IsNullOrEmpty(task.InputPath))
                {
                    result = await ProcessFileAsync(task.InputPath, task.OutputPath, task.Config, task.CancellationTokenSource.Token);
                }
                else
                {
                    throw new InvalidOperationException("No input data or path specified");
                }

                if (result.Success)
                {
                    task.Status = SuperResolutionStatus.Completed;
                    task.ProcessingTime = result.ProcessingTime;
                    task.Callback?.Invoke(result.OutputData ?? Array.Empty<byte>(), SuperResolutionStatus.Completed, result.ProcessingTime);
                }
                else
                {
                    task.Status = SuperResolutionStatus.Failed;
                    task.ErrorMessage = result.ErrorMessage;
                    task.Callback?.Invoke(Array.Empty<byte>(), SuperResolutionStatus.Failed, result.ProcessingTime);
                }
            }
            catch (OperationCanceledException)
            {
                task.Status = SuperResolutionStatus.Cancelled;
                task.Callback?.Invoke(Array.Empty<byte>(), SuperResolutionStatus.Cancelled, 0);
            }
            catch (Exception ex)
            {
                task.Status = SuperResolutionStatus.Failed;
                task.ErrorMessage = ex.Message;
                task.Callback?.Invoke(Array.Empty<byte>(), SuperResolutionStatus.Failed, 0);
            }
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        public void CancelTask(int taskId)
        {
            if (_tasks.TryGetValue(taskId, out var task))
            {
                task.CancellationTokenSource?.Cancel();
                task.Status = SuperResolutionStatus.Cancelled;
            }
        }

        /// <summary>
        /// 取消所有任务
        /// </summary>
        public void CancelAllTasks()
        {
            foreach (var task in _tasks.Values)
            {
                task.CancellationTokenSource?.Cancel();
                task.Status = SuperResolutionStatus.Cancelled;
            }
        }

        /// <summary>
        /// 获取任务
        /// </summary>
        public SuperResolutionTask? GetTask(int taskId)
        {
            return _tasks.TryGetValue(taskId, out var task) ? task : null;
        }

        /// <summary>
        /// 清理已完成的任务
        /// </summary>
        public void CleanupCompletedTasks()
        {
            var completedTasks = _tasks.Where(kvp =>
                kvp.Value.Status == SuperResolutionStatus.Completed ||
                kvp.Value.Status == SuperResolutionStatus.Failed ||
                kvp.Value.Status == SuperResolutionStatus.Cancelled)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var taskId in completedTasks)
            {
                _tasks.TryRemove(taskId, out _);
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            CancelAllTasks();
            _engine?.Dispose();
            _processSemaphore?.Dispose();
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
