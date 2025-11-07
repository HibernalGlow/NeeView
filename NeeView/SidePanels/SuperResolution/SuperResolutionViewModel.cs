using NeeLaboratory.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using NeeLaboratory.Windows.Input;

namespace NeeView.SuperResolution
{
    /// <summary>
    /// 超分辨率视图模型
    /// </summary>
    public class SuperResolutionViewModel : BindableBase
    {
        private readonly SuperResolutionConfig _config;
        private readonly ISuperResolutionService _service;

        public SuperResolutionViewModel(SuperResolutionConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _service = SuperResolutionService.Current;

            // 初始化命令
            ProcessCurrentImageCommand = new RelayCommand(ProcessCurrentImage, CanProcessCurrentImage);
            OpenBatchProcessCommand = new RelayCommand(OpenBatchProcess);
            InitializeServiceCommand = new RelayCommand(async () => await InitializeServiceAsync(null, true));
            ScanModelsCommand = new RelayCommand(async () => await ScanModelsAsync(), () => !string.IsNullOrEmpty(_config.ModelPath));

            // 监听模型路径变化
            _config.PropertyChanged += OnConfigPropertyChanged;

            // 初始化服务
            _ = InitializeServiceAsync();
        }

        private void OnConfigPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SuperResolutionConfig.ModelPath))
            {
                // 模型路径变化时自动扫描
                _ = ScanModelsAsync();
            }
            else if (e.PropertyName == nameof(SuperResolutionConfig.IsEnabled))
            {
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        #region Properties

    /// <summary>
    /// 配置
    /// </summary>
    public SuperResolutionConfig Config => _config;

        /// <summary>
        /// 是否正在处理
        /// </summary>
        private bool _isProcessing;
        public bool IsProcessing
        {
            get => _isProcessing;
            set
            {
                if (SetProperty(ref _isProcessing, value))
                {
                    // 通知 Command 重新评估 CanExecute
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// 状态信息
        /// </summary>
        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// 处理进度 (0-100)
        /// </summary>
        private int _progress;
        public int Progress
        {
            get => _progress;
            set => SetProperty(ref _progress, value);
        }

        /// <summary>
        /// 可选的运算设备列表
        /// </summary>
        private readonly ObservableCollection<SuperResolutionDeviceInfo> _availableDevices = new ObservableCollection<SuperResolutionDeviceInfo>();
        public ObservableCollection<SuperResolutionDeviceInfo> AvailableDevices => _availableDevices;

        /// <summary>
        /// 当前选中的运算设备
        /// </summary>
        private SuperResolutionDeviceInfo? _selectedDevice;
        private bool _suppressDeviceSelectionChanges;
        public SuperResolutionDeviceInfo? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                if (SetProperty(ref _selectedDevice, value))
                {
                    if (_suppressDeviceSelectionChanges || value == null)
                    {
                        return;
                    }

                    if (_config.GpuId != value.Id || !_service.IsAvailable)
                    {
                        _config.GpuId = value.Id;
                        SuperResolutionLogger.Info($"选择 GPU 设备: {value.DisplayName} (Id={value.Id})");
                        _ = InitializeServiceAsync(value.Id, true);
                    }
                }
            }
        }

        /// <summary>
        /// 服务是否可用
        /// </summary>
        private bool _isServiceAvailable;
        public bool IsServiceAvailable
        {
            get => _isServiceAvailable;
            set
            {
                if (SetProperty(ref _isServiceAvailable, value))
                {
                    // 通知 Command 重新评估 CanExecute
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                    SuperResolutionLogger.Info($"服务可用状态已更新: {value}");
                }
            }
        }

        /// <summary>
        /// 原始图片尺寸
        /// </summary>
        private string _originalSize = "";
        public string OriginalSize
        {
            get => _originalSize;
            set => SetProperty(ref _originalSize, value);
        }

        /// <summary>
        /// 目标图片尺寸
        /// </summary>
        private string _targetSize = "";
        public string TargetSize
        {
            get => _targetSize;
            set => SetProperty(ref _targetSize, value);
        }

        /// <summary>
        /// 处理时间
        /// </summary>
        private string _processingTime = "";
        public string ProcessingTime
        {
            get => _processingTime;
            set => SetProperty(ref _processingTime, value);
        }

        /// <summary>
        /// 可用的模型列表
        /// </summary>
        private ObservableCollection<DetectedModel> _availableModels = new ObservableCollection<DetectedModel>();
        public ObservableCollection<DetectedModel> AvailableModels
        {
            get => _availableModels;
            set => SetProperty(ref _availableModels, value);
        }

        /// <summary>
        /// 选中的模型
        /// </summary>
        private DetectedModel? _selectedModel;
        public DetectedModel? SelectedModel
        {
            get => _selectedModel;
            set
            {
                if (SetProperty(ref _selectedModel, value) && value != null)
                {
                    // 当模型选中时，自动更新配置
                    UpdateConfigFromModel(value);
                }
            }
        }

        /// <summary>
        /// 模型扫描状态
        /// </summary>
        private string _modelScanStatus = "未扫描";
        public string ModelScanStatus
        {
            get => _modelScanStatus;
            set => SetProperty(ref _modelScanStatus, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// 处理当前图片命令
        /// </summary>
        public ICommand ProcessCurrentImageCommand { get; }

        /// <summary>
        /// 打开批量处理命令
        /// </summary>
        public ICommand OpenBatchProcessCommand { get; }

        /// <summary>
        /// 初始化服务命令
        /// </summary>
        public ICommand InitializeServiceCommand { get; }

        /// <summary>
        /// 扫描模型命令
        /// </summary>
        public ICommand ScanModelsCommand { get; }

        #endregion

        #region Methods

        /// <summary>
        /// 初始化服务
        /// </summary>
        private async Task InitializeServiceAsync(int? gpuOverride = null, bool force = false)
        {
            var targetGpuId = gpuOverride ?? _config.GpuId;
            _config.GpuId = targetGpuId;

            SuperResolutionLogger.Info($"开始初始化超分辨率服务... (GPU Id={targetGpuId}, Force={force})");
            StatusMessage = "Initializing super resolution service...";
            IsServiceAvailable = await _service.InitializeAsync(targetGpuId, force);

            RefreshAvailableDevices(targetGpuId);

            if (IsServiceAvailable)
            {
                StatusMessage = "Service ready";
                SuperResolutionLogger.Info("服务初始化成功");
            }
            else
            {
                var error = _service.GetLastError();
                StatusMessage = $"Service initialization failed: {error}";
                SuperResolutionLogger.Error($"服务初始化失败: {error}");
            }
        }

        /// <summary>
        /// 是否可以处理当前图片
        /// </summary>
        private bool CanProcessCurrentImage()
        {
            return _config.IsEnabled && IsServiceAvailable && !IsProcessing;
        }

        private void RefreshAvailableDevices(int targetGpuId)
        {
            var devices = _service.AvailableDevices;

            _suppressDeviceSelectionChanges = true;
            try
            {
                _availableDevices.Clear();

                if (devices != null)
                {
                    foreach (var device in devices)
                    {
                        _availableDevices.Add(device);
                    }
                }

                if (_availableDevices.Count == 0)
                {
                    SelectedDevice = null;
                    return;
                }

                var selected = _availableDevices.FirstOrDefault(device => device.Id == targetGpuId)
                               ?? _availableDevices.FirstOrDefault(device => device.Id >= 0)
                               ?? _availableDevices.FirstOrDefault();

                SelectedDevice = selected;

                if (selected != null && _config.GpuId != selected.Id)
                {
                    _config.GpuId = selected.Id;
                }
            }
            finally
            {
                _suppressDeviceSelectionChanges = false;
            }
        }

        /// <summary>
        /// 处理当前图片
        /// </summary>
        private async void ProcessCurrentImage()
        {
            SuperResolutionLogger.Info("========== ProcessCurrentImage 被调用 ==========");
            
            if (!CanProcessCurrentImage())
            {
                SuperResolutionLogger.Warning($"无法处理图片: IsServiceAvailable={IsServiceAvailable}, IsProcessing={IsProcessing}");
                return;
            }

            IsProcessing = true;
            Progress = 0;
            StatusMessage = "正在处理...";
            SuperResolutionLogger.Info("开始超分辨率处理流程");

            try
            {
                // 获取当前显示的图片
                var book = BookOperation.Current.Book;
                if (book == null || book.CurrentPage == null)
                {
                    StatusMessage = "没有打开的图片";
                    SuperResolutionLogger.Warning("尝试处理图片,但没有打开的 Book 或页面");
                    return;
                }

                var currentPage = book.CurrentPage;
                SuperResolutionLogger.Info($"开始处理当前图片: {currentPage.EntryFullName}");
                
                // 优先尝试从页面获取已解码的 BitmapSource (避免二次解码导致缩略)
                byte[]? imageData = null;
                try
                {
                    var bitmapSource = ImageDataHelper.GetCurrentBitmapSource();
                    if (bitmapSource != null)
                    {
                        SuperResolutionLogger.Info("找到已解码的 BitmapSource,优先使用它来生成无损 PNG 并传入超分服务");
                        try
                        {
                            imageData = NeeView.SuperResolution.ImageFormatConverter.ConvertBitmapSourceToPng(bitmapSource);
                            SuperResolutionLogger.Info($"从 BitmapSource 生成 PNG: {imageData.Length / 1024.0:F2} KB");
                        }
                        catch (Exception ex)
                        {
                            SuperResolutionLogger.Error($"将 BitmapSource 转为 PNG 失败: {ex.Message}", ex);
                            imageData = null; // 继续尝试后续读取路径
                        }
                    }
                }
                catch (Exception ex)
                {
                    SuperResolutionLogger.Error($"尝试使用 BitmapSource 时出错: {ex.Message}", ex);
                }

                // 如果没有可用的 BitmapSource, 从 ArchiveEntry 或文件读取原始数据
                var entry = currentPage.ArchiveEntry;
                if ((imageData == null || imageData.Length == 0) && entry != null)
                {
                    try
                    {
                        var fileProxy = await entry.GetFileProxyAsync(false, System.Threading.CancellationToken.None);
                        imageData = await System.IO.File.ReadAllBytesAsync(fileProxy.Path);

                        SuperResolutionLogger.Info($"成功读取图片数据: {imageData.Length / 1024.0:F2} KB");

                        try
                        {
                            // 记录来源与图片基本信息，便于排查 AVIF/JXL 被提前缩放或转换的问题
                            var place = entry.Archive?.GetPlace() ?? "(unknown place)";
                            var archivePath = entry.Archive?.Path ?? "(no archive path)";
                            var rootArchiveName = entry.RootArchiveName ?? "(root)";
                            var entryName = entry.EntryFullName ?? entry.EntryLastName ?? "(entry)";
                            var title = entry.EntryLastName ?? string.Empty;
                            var fileSize = entry.Length >= 0 ? entry.Length : imageData.Length;
                            var format = NeeView.SuperResolution.ImageFormatConverter.DetectFormat(imageData);

                            SuperResolutionLogger.Info($"图片来源: place={place}, archivePath={archivePath}, rootArchive={rootArchiveName}");
                            SuperResolutionLogger.Info($"图片条目: fullName={entryName}, title={title}, length={fileSize} bytes, detectedFormat={format}");
                        }
                        catch (Exception logEx)
                        {
                            SuperResolutionLogger.Error($"记录图片来源信息时出错: {logEx.Message}", logEx);
                        }
                    }
                    catch (Exception ex)
                    {
                        SuperResolutionLogger.Error($"读取图片数据失败: {ex.Message}", ex);
                        StatusMessage = $"读取图片失败: {ex.Message}";
                        return;
                    }
                }

                if (imageData == null || imageData.Length == 0)
                {
                    StatusMessage = "无法获取图片数据";
                    SuperResolutionLogger.Error("图片数据为空");
                    return;
                }

                // 调用超分服务
                Progress = 10;
                StatusMessage = "正在进行超分辨率处理...";
                
                var result = await _service.ProcessAsync(imageData, _config, System.Threading.CancellationToken.None);
                
                if (result.Success && result.OutputData != null && result.OutputData.Length > 0)
                {
                    Progress = 90;
                    StatusMessage = "保存处理结果...";
                    
                    // 保存到临时文件并显示
                    var tempPath = System.IO.Path.Combine(
                        System.IO.Path.GetTempPath(),
                        $"NeeView_SR_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                    );
                    
                    await System.IO.File.WriteAllBytesAsync(tempPath, result.OutputData);
                    SuperResolutionLogger.Info($"超分结果已保存: {tempPath}");
                    
                    // 在 NeeView 中打开结果
                    BookHub.Current.RequestLoad(this, tempPath, null, BookLoadOption.None, true);
                    
                    Progress = 100;
                    StatusMessage = $"处理完成! 耗时: {result.ProcessingTime:F2}s";
                    SuperResolutionLogger.Info($"超分处理成功完成");
                }
                else
                {
                    StatusMessage = $"处理失败: {result.ErrorMessage}";
                    SuperResolutionLogger.Error($"超分处理失败: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"错误: {ex.Message}";
                SuperResolutionLogger.Error($"处理图片时发生异常: {ex.Message}", ex);
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// 打开批量处理窗口
        /// </summary>
        private void OpenBatchProcess()
        {
            // TODO: 打开批量处理窗口
            StatusMessage = "Batch processing window (to be implemented)";
        }

        /// <summary>
        /// 扫描模型文件夹
        /// </summary>
        private async Task ScanModelsAsync()
        {
            if (string.IsNullOrEmpty(_config.ModelPath))
            {
                ModelScanStatus = "未设置模型路径";
                AvailableModels.Clear();
                return;
            }

            ModelScanStatus = "正在扫描...";
            
            await Task.Run(() =>
            {
                try
                {
                    var models = ModelScanner.ScanModelDirectory(_config.ModelPath);
                    
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        AvailableModels.Clear();
                        foreach (var model in models)
                        {
                            AvailableModels.Add(model);
                        }

                        if (models.Count > 0)
                        {
                            ModelScanStatus = $"找到 {models.Count} 个模型";
                            if (SelectedModel == null)
                            {
                                SelectedModel = models[0];
                            }
                        }
                        else
                        {
                            ModelScanStatus = "未找到可用模型";
                        }
                    });
                }
                catch (Exception ex)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        ModelScanStatus = $"扫描失败: {ex.Message}";
                        AvailableModels.Clear();
                    });
                }
            });
        }

        /// <summary>
        /// 根据选中的模型更新配置
        /// </summary>
        private void UpdateConfigFromModel(DetectedModel model)
        {
            try
            {
                SuperResolutionLogger.Info($"选中模型: {model.DisplayName}");
                
                // 根据模型类型设置算法类型
                _config.AlgorithmType = model.ModelType switch
                {
                    ModelType.Waifu2x => SuperResolutionType.Waifu2x,
                    ModelType.RealESRGAN => SuperResolutionType.RealESRGAN,
                    ModelType.RealCUGAN => SuperResolutionType.RealCUGAN,
                    _ => SuperResolutionType.Waifu2x
                };

                // 设置缩放倍数
                if (model.Scale > 0)
                {
                    _config.ScaleFactor = model.Scale;
                }

                // 设置降噪等级
                _config.NoiseLevel = model.DenoiseLevel;

                // 根据模型名称映射到 SuperResolutionModel 枚举
                _config.Model = MapDetectedModelToEnum(model);

                SuperResolutionLogger.Info($"配置已更新: Type={_config.AlgorithmType}, Scale={_config.ScaleFactor}x, Denoise={_config.NoiseLevel}, Model={_config.Model}");
            }
            catch (Exception ex)
            {
                SuperResolutionLogger.Error($"更新配置失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 将检测到的模型映射到配置枚举
        /// </summary>
        private SuperResolutionModel MapDetectedModelToEnum(DetectedModel model)
        {
            // 根据模型名称和类型映射
            var modelName = model.ModelName.ToLowerInvariant();
            
            if (model.ModelType == ModelType.Waifu2x)
            {
                if (modelName.Contains("anime"))
                {
                    if (model.Scale == 2) return SuperResolutionModel.Waifu2xAnime2x;
                    if (model.Scale == 4) return SuperResolutionModel.Waifu2xAnime4x;
                }
                else if (modelName.Contains("photo"))
                {
                    if (model.Scale == 2) return SuperResolutionModel.Waifu2xPhoto2x;
                    if (model.Scale == 4) return SuperResolutionModel.Waifu2xPhoto4x;
                }
            }
            else if (model.ModelType == ModelType.RealESRGAN)
            {
                if (modelName.Contains("anime"))
                {
                    return SuperResolutionModel.RealESRGANAnime4x;
                }
                else
                {
                    return SuperResolutionModel.RealESRGANGeneral4x;
                }
            }
            else if (model.ModelType == ModelType.RealCUGAN)
            {
                if (model.Scale == 2) return SuperResolutionModel.RealCUGANAnime2x;
                if (model.Scale == 3) return SuperResolutionModel.RealCUGANAnime3x;
                if (model.Scale == 4) return SuperResolutionModel.RealCUGANAnime4x;
            }

            // 默认返回
            return SuperResolutionModel.Waifu2xAnime2x;
        }

        #endregion
    }
}
