using NeeLaboratory.ComponentModel;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;

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

            // 初始化服务
            _ = InitializeServiceAsync();
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
            StatusMessage = "Processing...";

            try
            {
                // TODO: 获取当前显示的图片
                // 这需要与NeeView的图片显示系统集成
                
                // 临时模拟处理
                await Task.Delay(1000);
                
                Progress = 100;
                StatusMessage = "Processing completed";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
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

        #endregion
    }
}
