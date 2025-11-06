using NeeLaboratory.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
            InitializeServiceCommand = new RelayCommand(async () => await InitializeServiceAsync());
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
        }

        #region Properties

        /// <summary>
        /// 配置
        /// </summary>
        public SuperResolutionConfig Config => _config;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled
        {
            get => _config.IsEnabled;
            set
            {
                if (_config.IsEnabled != value)
                {
                    _config.IsEnabled = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// 是否正在处理
        /// </summary>
        private bool _isProcessing;
        public bool IsProcessing
        {
            get => _isProcessing;
            set => SetProperty(ref _isProcessing, value);
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
        /// 服务是否可用
        /// </summary>
        private bool _isServiceAvailable;
        public bool IsServiceAvailable
        {
            get => _isServiceAvailable;
            set => SetProperty(ref _isServiceAvailable, value);
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
            set => SetProperty(ref _selectedModel, value);
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
        private async Task InitializeServiceAsync()
        {
            StatusMessage = "Initializing super resolution service...";
            IsServiceAvailable = await _service.InitializeAsync();

            if (IsServiceAvailable)
            {
                StatusMessage = "Service ready";
            }
            else
            {
                StatusMessage = $"Service initialization failed: {_service.GetLastError()}";
            }
        }

        /// <summary>
        /// 是否可以处理当前图片
        /// </summary>
        private bool CanProcessCurrentImage()
        {
            return IsServiceAvailable && !IsProcessing;
        }

        /// <summary>
        /// 处理当前图片
        /// </summary>
        private async void ProcessCurrentImage()
        {
            if (!CanProcessCurrentImage()) return;

            IsProcessing = true;
            Progress = 0;
            StatusMessage = "正在处理...";

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
                
                // 获取图片数据
                byte[]? imageData = null;
                
                // 从 ArchiveEntry 获取原始数据
                var entry = currentPage.ArchiveEntry;
                if (entry != null)
                {
                    try
                    {
                        var fileProxy = await entry.GetFileProxyAsync(false, System.Threading.CancellationToken.None);
                        imageData = await System.IO.File.ReadAllBytesAsync(fileProxy.Path);
                        
                        SuperResolutionLogger.Info($"成功读取图片数据: {imageData.Length / 1024.0:F2} KB");
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

        #endregion
    }
}
