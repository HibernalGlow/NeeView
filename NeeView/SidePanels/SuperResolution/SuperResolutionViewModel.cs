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
        
        // 🎯 当前图片强制禁用超分的标记 (用于"取消超分"场景)
        private static string? _currentImageNoSuperResolution = null;

        // 🎯 防止递归更新的标志
        private bool _isUpdatingFromDatabase = false;

        public SuperResolutionViewModel(SuperResolutionConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _service = SuperResolutionService.Current;

            // 🎯 确保 Current 指向传入的 config 实例
            SuperResolutionConfig.Current = _config;

            // 🎯 初始化状态数据库
            InitializeStateDatabase();

            // 初始化命令
            ProcessCurrentImageCommand = new RelayCommand(ProcessCurrentImage, CanProcessCurrentImage);
            OpenBatchProcessCommand = new RelayCommand(OpenBatchProcess);
            InitializeServiceCommand = new RelayCommand(async () => await InitializeServiceAsync(null, true));
            ScanModelsCommand = new RelayCommand(async () => await ScanModelsAsync(), () => !string.IsNullOrEmpty(_config.ModelPath));

            // 监听模型路径变化
            _config.PropertyChanged += OnConfigPropertyChanged;

            // 监听页面变化
            BookOperation.Current.BookChanged += OnBookChanged;

            // 初始化服务
            _ = InitializeServiceAsync();
        }

        /// <summary>
        /// 初始化状态数据库
        /// </summary>
        private void InitializeStateDatabase()
        {
            try
            {
                var dbPath = System.IO.Path.Combine(
                    Environment.LocalApplicationDataPath, 
                    "SuperResolutionState.db");
                SuperResolutionStateManager.Current.Initialize(dbPath);
                SuperResolutionLogger.Info($"[StateDB] 数据库已初始化: {dbPath}");
            }
            catch (Exception ex)
            {
                SuperResolutionLogger.Error($"[StateDB] 初始化失败: {ex.Message}", ex);
            }
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
                
                // 🎯 全局开关关闭时,自动禁用当前图片超分
                if (!_config.IsEnabled && EnableCurrentImageSuperResolution)
                {
                    EnableCurrentImageSuperResolution = false;
                    ToastService.Current.Show(new Toast("🔄 已自动切换到原图", "全局超分已禁用", ToastIcon.Information));
                }
            }
        }

        /// <summary>
        /// 处理页面变化事件
        /// </summary>
        private void OnBookChanged(object? sender, BookChangedEventArgs e)
        {
            UpdateCurrentImageInfo();
        }

        /// <summary>
        /// 更新当前图片信息
        /// 🎯 重构后:只更新显示信息,不修改用户偏好(EnableCurrentImageSuperResolution)
        /// </summary>
        private void UpdateCurrentImageInfo()
        {
            try
            {
                var book = BookOperation.Current.Book;
                if (book == null || book.CurrentPage == null)
                {
                    CurrentImageFullPath = "";
                    CurrentImageResolution = "";
                    CurrentImageStatus = SuperResolutionImageStatus.None;
                    SuperResolutionResolution = "";
                    SuperResolutionImagePath = "";
                    
                    // 🎯 用户偏好独立管理,从数据库读取
                    _isUpdatingFromDatabase = true;
                    try
                    {
                        var userPref = SuperResolutionStateManager.Current.GetUserPreference("");
                        EnableCurrentImageSuperResolution = (userPref == SuperResolutionUserPreference.Enabled);
                    }
                    finally
                    {
                        _isUpdatingFromDatabase = false;
                    }
                    return;
                }

                var page = book.CurrentPage;
                var entry = page.ArchiveEntry;
                if (entry != null)
                {
                    CurrentImageFullPath = entry.SystemPath;
                    
                    // 更新分辨率信息
                    if (page.Size.Width > 0 && page.Size.Height > 0)
                    {
                        CurrentImageResolution = $"{(int)page.Size.Width}x{(int)page.Size.Height}";
                    }
                    else
                    {
                        CurrentImageResolution = "";
                    }
                    
                    // 🎯 检查缓存状态并获取真实超分信息
                    var cache = SuperResolutionImageCache.Current;
                    var cacheItem = cache.Get(CurrentImageFullPath);
                    
                    if (cacheItem != null)
                    {
                        CurrentImageStatus = cacheItem.Status;
                        
                        // 🔥 获取真实超分分辨率和路径
                        if (cacheItem.Status == SuperResolutionImageStatus.Completed)
                        {
                            if (cacheItem.SuperResolutionWidth > 0 && cacheItem.SuperResolutionHeight > 0)
                            {
                                SuperResolutionResolution = $"{cacheItem.SuperResolutionWidth}x{cacheItem.SuperResolutionHeight}";
                            }
                            else
                            {
                                SuperResolutionResolution = "未知";
                            }
                            
                            // 超分图片路径(如果有保存)
                            SuperResolutionImagePath = cacheItem.SuperResolutionData != null 
                                ? $"内存缓存 ({cacheItem.SuperResolutionData.Length / 1024.0:F1} KB)" 
                                : "";
                            
                            SuperResolutionLogger.Info($"[缓存信息] {CurrentImageFullPath}: 原图{cacheItem.OriginalWidth}x{cacheItem.OriginalHeight} → 超分{cacheItem.SuperResolutionWidth}x{cacheItem.SuperResolutionHeight}");
                        }
                        else
                        {
                            SuperResolutionResolution = "";
                            SuperResolutionImagePath = "";
                        }
                    }
                    else
                    {
                        CurrentImageStatus = SuperResolutionImageStatus.None;
                        SuperResolutionResolution = "";
                        SuperResolutionImagePath = "";
                    }

                    // 🎯 从数据库读取用户偏好,更新UI状态
                    _isUpdatingFromDatabase = true;
                    try
                    {
                        var userPreference = SuperResolutionStateManager.Current.GetUserPreference(CurrentImageFullPath);
                        switch (userPreference)
                        {
                            case SuperResolutionUserPreference.Enabled:
                                EnableCurrentImageSuperResolution = true;
                                break;
                            case SuperResolutionUserPreference.Disabled:
                                EnableCurrentImageSuperResolution = false;
                                break;
                            case SuperResolutionUserPreference.Auto:
                                // Auto 模式:勾选框显示为未勾选,表示"跟随自动条件"
                                // 如果已有缓存且全局开关+自动超分都启用,则勾选
                                if (cacheItem != null && cacheItem.Status == SuperResolutionImageStatus.Completed)
                                {
                                    EnableCurrentImageSuperResolution = (_config.IsEnabled && _config.AutoApplyOnView);
                                }
                                else if (ShouldAutoEnableSuperResolution(page, entry))
                                {
                                    EnableCurrentImageSuperResolution = (_config.IsEnabled && _config.AutoApplyOnView);
                                }
                                else
                                {
                                    EnableCurrentImageSuperResolution = false;
                                }
                                break;
                        }
                    }
                    finally
                    {
                        _isUpdatingFromDatabase = false;
                    }
                }
                else
                {
                    CurrentImageFullPath = "";
                    CurrentImageResolution = "";
                    CurrentImageStatus = SuperResolutionImageStatus.None;
                    SuperResolutionResolution = "";
                    SuperResolutionImagePath = "";
                    
                    _isUpdatingFromDatabase = true;
                    try
                    {
                        EnableCurrentImageSuperResolution = false;
                    }
                    finally
                    {
                        _isUpdatingFromDatabase = false;
                    }
                }
            }
            catch (Exception ex)
            {
                SuperResolutionLogger.Error($"更新当前图片信息失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 检查图片是否符合自动超分条件
        /// </summary>
        private bool ShouldAutoEnableSuperResolution(Page page, ArchiveEntry entry)
        {
            try
            {
                // 获取图片信息
                var pictureInfo = page.Content.PictureInfo;
                if (pictureInfo == null)
                    return false;

                var width = (int)pictureInfo.OriginalSize.Width;
                var height = (int)pictureInfo.OriginalSize.Height;
                var fileSize = entry.Length;

                // 检查宽度限制
                if (_config.AutoApplyMinWidth > 0 && width < _config.AutoApplyMinWidth)
                    return false;
                if (_config.AutoApplyMaxWidth > 0 && width > _config.AutoApplyMaxWidth)
                    return false;

                // 检查高度限制
                if (_config.AutoApplyMinHeight > 0 && height < _config.AutoApplyMinHeight)
                    return false;
                if (_config.AutoApplyMaxHeight > 0 && height > _config.AutoApplyMaxHeight)
                    return false;

                // 检查最大尺寸
                var maxDimension = Math.Max(width, height);
                if (_config.AutoApplyMaxSize > 0 && maxDimension > _config.AutoApplyMaxSize)
                    return false;

                // 检查文件大小
                if (fileSize > 0)
                {
                    var fileSizeKB = fileSize / 1024;
                    if (_config.AutoApplyMinFileSize > 0 && fileSizeKB < _config.AutoApplyMinFileSize)
                        return false;
                    if (_config.AutoApplyMaxFileSize > 0 && fileSizeKB > _config.AutoApplyMaxFileSize)
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                SuperResolutionLogger.Error($"检查自动超分条件失败: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 🎯 检查当前图片是否应该跳过自动超分 (静态方法,供 BitmapPictureSource 调用)
        /// </summary>
        public static bool ShouldSkipAutoSuperResolution(string imagePath)
        {
            return _currentImageNoSuperResolution == imagePath;
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

        /// <summary>
        /// 当前图片启用超分 (切换显示)
        /// 🎯 重构后:用户修改时保存到数据库,从数据库读取时不触发 HandleCurrentImageToggleAsync
        /// </summary>
        private bool _enableCurrentImageSuperResolution;
        public bool EnableCurrentImageSuperResolution
        {
            get => _enableCurrentImageSuperResolution;
            set
            {
                if (SetProperty(ref _enableCurrentImageSuperResolution, value))
                {
                    // 🎯 如果是从数据库更新来的,不触发切换逻辑(防止循环)
                    if (_isUpdatingFromDatabase)
                    {
                        return;
                    }

                    // 🎯 用户手动修改,保存到数据库
                    if (!string.IsNullOrEmpty(CurrentImageFullPath))
                    {
                        var preference = value ? SuperResolutionUserPreference.Enabled : SuperResolutionUserPreference.Disabled;
                        SuperResolutionStateManager.Current.SetUserPreference(CurrentImageFullPath, preference, "UserToggle");
                    }

                    // 触发切换逻辑
                    _ = HandleCurrentImageToggleAsync();
                }
            }
        }

        /// <summary>
        /// 当前图片超分状态
        /// </summary>
        private SuperResolutionImageStatus _currentImageStatus = SuperResolutionImageStatus.None;
        public SuperResolutionImageStatus CurrentImageStatus
        {
            get => _currentImageStatus;
            set
            {
                if (SetProperty(ref _currentImageStatus, value))
                {
                    RaisePropertyChanged(nameof(CurrentImageStatusText));
                }
            }
        }

        /// <summary>
        /// 当前图片状态文本
        /// </summary>
        public string CurrentImageStatusText
        {
            get
            {
                return CurrentImageStatus switch
                {
                    SuperResolutionImageStatus.None => "未超分",
                    SuperResolutionImageStatus.Processing => "超分中...",
                    SuperResolutionImageStatus.Completed => "已超分",
                    SuperResolutionImageStatus.Failed => "超分失败",
                    _ => "未知"
                };
            }
        }

        /// <summary>
        /// 当前图片路径
        /// </summary>
        private string _currentImagePath = "";
        private string _currentImageFullPath = "";  // 内部保存完整路径
        
        public string CurrentImagePath
        {
            get
            {
                // UI显示文件名,内部使用完整路径
                if (string.IsNullOrEmpty(_currentImagePath))
                    return "无图片";
                return System.IO.Path.GetFileName(_currentImagePath) ?? "无图片";
            }
        }

        /// <summary>
        /// 当前图片完整路径 (内部使用)
        /// </summary>
        public string CurrentImageFullPath
        {
            get => _currentImageFullPath;
            private set
            {
                if (SetProperty(ref _currentImageFullPath, value))
                {
                    _currentImagePath = value;
                    RaisePropertyChanged(nameof(CurrentImagePath));
                    RaisePropertyChanged(nameof(CurrentImageInfo));
                }
            }
        }

        /// <summary>
        /// 当前图片分辨率信息 (例如: "1920x1080")
        /// </summary>
        private string _currentImageResolution = "";
        public string CurrentImageResolution
        {
            get => _currentImageResolution;
            set
            {
                if (SetProperty(ref _currentImageResolution, value))
                {
                    RaisePropertyChanged(nameof(CurrentImageInfo));
                }
            }
        }

        /// <summary>
        /// 超分后的真实分辨率 (来自缓存)
        /// </summary>
        private string _superResolutionResolution = "";
        public string SuperResolutionResolution
        {
            get => _superResolutionResolution;
            set => SetProperty(ref _superResolutionResolution, value);
        }

        /// <summary>
        /// 超分后的图片路径 (临时文件)
        /// </summary>
        private string _superResolutionImagePath = "";
        public string SuperResolutionImagePath
        {
            get => _superResolutionImagePath;
            set => SetProperty(ref _superResolutionImagePath, value);
        }

        /// <summary>
        /// 当前图片详细信息 (路径 + 分辨率)
        /// </summary>
        public string CurrentImageInfo
        {
            get
            {
                if (string.IsNullOrEmpty(_currentImagePath))
                    return "无图片";
                
                var fileName = System.IO.Path.GetFileName(_currentImagePath);
                if (string.IsNullOrEmpty(CurrentImageResolution))
                    return fileName;
                
                return $"{fileName} - {CurrentImageResolution}";
            }
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

        /// <summary>
        /// 处理当前图片超分切换
        /// </summary>
        private async Task HandleCurrentImageToggleAsync()
        {
            if (string.IsNullOrEmpty(CurrentImageFullPath))
            {
                SuperResolutionLogger.Warning("当前没有打开的图片");
                return;
            }

            try
            {
                var book = BookOperation.Current.Book;
                if (book == null || book.CurrentPage == null)
                {
                    SuperResolutionLogger.Warning("当前没有打开的页面");
                    return;
                }

                if (EnableCurrentImageSuperResolution)
                {
                    // ✅ 启用超分
                    if (!_config.IsEnabled)
                    {
                        ToastService.Current.Show(new Toast("⚠️ 请先启用超分辨率功能", null, ToastIcon.Warning));
                        EnableCurrentImageSuperResolution = false;
                        return;
                    }

                    // 🎯 清除禁用标记,允许超分
                    _currentImageNoSuperResolution = null;
                    
                    CurrentImageStatus = SuperResolutionImageStatus.Processing;
                    StatusMessage = "正在超分当前图片...";
                    ToastService.Current.Show(new Toast("🔄 正在处理超分...", null, ToastIcon.Information));

                    // 卸载并重新加载 (会触发 BitmapPictureSource 的自动超分逻辑)
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        book.CurrentPage.Content.Unload();
                    });
                    
                    await Task.Delay(100);
                    
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        BookOperation.Current.BookControl.ReLoad();
                    });
                    
                    // 等待加载完成
                    await Task.Delay(500);
                    
                    // 🎯 从实际加载的图片获取尺寸
                    var currentPage = BookOperation.Current.Book?.CurrentPage;
                    if (currentPage != null)
                    {
                        var width = (int)currentPage.Size.Width;
                        var height = (int)currentPage.Size.Height;
                        CurrentImageResolution = $"{width}x{height}";
                        
                        CurrentImageStatus = SuperResolutionImageStatus.Completed;
                        StatusMessage = $"超分完成: {width}x{height}";
                        ToastService.Current.Show(new Toast("✅ 超分完成", $"{System.IO.Path.GetFileName(CurrentImageFullPath)}\n{width}x{height}", ToastIcon.Information));
                    }
                    else
                    {
                        CurrentImageStatus = SuperResolutionImageStatus.Completed;
                        StatusMessage = "超分已应用";
                        ToastService.Current.Show(new Toast("✅ 超分完成", null, ToastIcon.Information));
                    }
                }
                else
                {
                    // 🔄 切换回原图
                    // 🎯 设置禁用标记,阻止自动超分
                    _currentImageNoSuperResolution = CurrentImageFullPath;
                    
                    CurrentImageStatus = SuperResolutionImageStatus.None;
                    StatusMessage = "切换到原图";
                    
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        book.CurrentPage.Content.Unload();
                    });
                    
                    await Task.Delay(100);
                    
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        BookOperation.Current.BookControl.ReLoad();
                    });
                    
                    await Task.Delay(200);
                    
                    // 🎯 获取原图尺寸
                    var currentPage = BookOperation.Current.Book?.CurrentPage;
                    if (currentPage != null)
                    {
                        var width = (int)currentPage.Size.Width;
                        var height = (int)currentPage.Size.Height;
                        CurrentImageResolution = $"{width}x{height}";
                        
                        StatusMessage = $"原图: {width}x{height}";
                        ToastService.Current.Show(new Toast("🔄 已切换到原图", $"{System.IO.Path.GetFileName(CurrentImageFullPath)}\n{width}x{height}", ToastIcon.Information));
                    }
                    else
                    {
                        ToastService.Current.Show(new Toast("🔄 已切换到原图", null, ToastIcon.Information));
                    }
                }
            }
            catch (Exception ex)
            {
                SuperResolutionLogger.Error($"切换超分失败: {ex.Message}", ex);
                CurrentImageStatus = SuperResolutionImageStatus.Failed;
                StatusMessage = $"错误: {ex.Message}";
                ToastService.Current.Show(new Toast("❌ 切换超分失败", ex.Message, ToastIcon.Error));
                EnableCurrentImageSuperResolution = false;
                _currentImageNoSuperResolution = null;
            }
        }

        /// <summary>
        /// 显示超分图片
        /// </summary>
        private async Task ShowSuperResolutionImageAsync(byte[] imageData)
        {
            await Task.Run(() =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    var tempPath = System.IO.Path.Combine(
                        System.IO.Path.GetTempPath(),
                        "NeeView_SR_Current",
                        $"{System.IO.Path.GetFileNameWithoutExtension(CurrentImagePath)}_SR.png"
                    );

                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(tempPath)!);
                    System.IO.File.WriteAllBytes(tempPath, imageData);

                    BookHub.Current.RequestLoad(this, tempPath, null, BookLoadOption.None, true);
                });
            });
        }

        /// <summary>
        /// 显示原图
        /// </summary>
        private async Task ShowOriginalImageAsync(byte[] imageData)
        {
            await Task.Run(() =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    BookHub.Current.RequestLoad(this, CurrentImagePath, null, BookLoadOption.None, true);
                });
            });
        }

        /// <summary>
        /// 为切换功能处理当前图片
        /// </summary>
        private async Task ProcessCurrentImageForToggleAsync()
        {
            CurrentImageStatus = SuperResolutionImageStatus.Processing;
            StatusMessage = "正在超分当前图片...";

            try
            {
                // 获取图片数据
                var book = BookOperation.Current.Book;
                if (book == null || book.CurrentPage == null)
                {
                    CurrentImageStatus = SuperResolutionImageStatus.None;
                    return;
                }

                var currentPage = book.CurrentPage;
                var entry = currentPage.ArchiveEntry;
                byte[]? imageData = null;

                if (entry != null)
                {
                    var fileProxy = await entry.GetFileProxyAsync(false, System.Threading.CancellationToken.None);
                    imageData = await System.IO.File.ReadAllBytesAsync(fileProxy.Path);
                }

                if (imageData == null || imageData.Length == 0)
                {
                    CurrentImageStatus = SuperResolutionImageStatus.Failed;
                    return;
                }

                // 保存到缓存
                var cache = SuperResolutionImageCache.Current;
                cache.Update(CurrentImagePath, item =>
                {
                    item.OriginalData = imageData;
                    item.Status = SuperResolutionImageStatus.Processing;
                });

                // 执行超分
                var result = await _service.ProcessAsync(imageData, _config, System.Threading.CancellationToken.None);

                if (result.Success && result.OutputData != null)
                {
                    // 更新缓存
                    cache.Update(CurrentImagePath, item =>
                    {
                        item.SuperResolutionData = result.OutputData;
                        item.Status = SuperResolutionImageStatus.Completed;
                        item.OriginalWidth = result.OriginalWidth;
                        item.OriginalHeight = result.OriginalHeight;
                        item.SuperResolutionWidth = result.OutputWidth;
                        item.SuperResolutionHeight = result.OutputHeight;
                        item.ProcessingTime = result.ProcessingTime;
                    });

                    CurrentImageStatus = SuperResolutionImageStatus.Completed;

                    // 如果仍然勾选，显示超分图
                    if (EnableCurrentImageSuperResolution)
                    {
                        await ShowSuperResolutionImageAsync(result.OutputData);
                    }

                    SuperResolutionLogger.Info($"当前图片超分完成: {CurrentImagePath}");
                }
                else
                {
                    cache.Update(CurrentImagePath, item =>
                    {
                        item.Status = SuperResolutionImageStatus.Failed;
                        item.ErrorMessage = result.ErrorMessage;
                    });
                    CurrentImageStatus = SuperResolutionImageStatus.Failed;
                    SuperResolutionLogger.Error($"当前图片超分失败: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                CurrentImageStatus = SuperResolutionImageStatus.Failed;
                SuperResolutionLogger.Error($"处理当前图片失败: {ex.Message}", ex);
            }
        }

        #endregion
    }
}
