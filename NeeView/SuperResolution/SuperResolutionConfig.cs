using NeeLaboratory.ComponentModel;
using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace NeeView.SuperResolution
{
    /// <summary>
    /// 超分辨率配置
    /// </summary>
    [DataContract]
    public class SuperResolutionConfig : BindableBase
    {
        /// <summary>
        /// 是否启用超分辨率
        /// </summary>
        private bool _isEnabled;
        [DataMember]
        [DefaultValue(false)]
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }

        /// <summary>
        /// 超分辨率算法类型
        /// </summary>
        private SuperResolutionType _algorithmType = SuperResolutionType.Waifu2x;
        [DataMember]
        [DefaultValue(SuperResolutionType.Waifu2x)]
        public SuperResolutionType AlgorithmType
        {
            get => _algorithmType;
            set => SetProperty(ref _algorithmType, value);
        }

        /// <summary>
        /// 超分辨率模型
        /// </summary>
        private SuperResolutionModel _model = SuperResolutionModel.Waifu2xAnime2x;
        [DataMember]
        [DefaultValue(SuperResolutionModel.Waifu2xAnime2x)]
        public SuperResolutionModel Model
        {
            get => _model;
            set => SetProperty(ref _model, value);
        }

        /// <summary>
        /// 缩放模式
        /// </summary>
        private ScaleMode _scaleMode = ScaleMode.ScaleFactor;
        [DataMember]
        [DefaultValue(ScaleMode.ScaleFactor)]
        public ScaleMode ScaleMode
        {
            get => _scaleMode;
            set => SetProperty(ref _scaleMode, value);
        }

        /// <summary>
        /// 缩放倍数 (当ScaleMode为ScaleFactor时使用)
        /// </summary>
        private double _scaleFactor = 2.0;
        [DataMember]
        [DefaultValue(2.0)]
        public double ScaleFactor
        {
            get => _scaleFactor;
            set => SetProperty(ref _scaleFactor, Math.Max(0.1, Math.Min(64.0, value)));
        }

        /// <summary>
        /// 目标宽度 (当ScaleMode为TargetSize时使用)
        /// </summary>
        private int _targetWidth = 1920;
        [DataMember]
        [DefaultValue(1920)]
        public int TargetWidth
        {
            get => _targetWidth;
            set => SetProperty(ref _targetWidth, Math.Max(1, value));
        }

        /// <summary>
        /// 目标高度 (当ScaleMode为TargetSize时使用)
        /// </summary>
        private int _targetHeight = 1080;
        [DataMember]
        [DefaultValue(1080)]
        public int TargetHeight
        {
            get => _targetHeight;
            set => SetProperty(ref _targetHeight, Math.Max(1, value));
        }

        /// <summary>
        /// 是否使用TTA模式 (Test-Time Augmentation)
        /// 提高画质但增加处理时间
        /// </summary>
        private bool _useTTA;
        [DataMember]
        [DefaultValue(false)]
        public bool UseTTA
        {
            get => _useTTA;
            set => SetProperty(ref _useTTA, value);
        }

        /// <summary>
        /// GPU设备ID (-1表示CPU，0+表示GPU编号)
        /// </summary>
        private int _gpuId = 0;
        [DataMember]
        [DefaultValue(0)]
        public int GpuId
        {
            get => _gpuId;
            set => SetProperty(ref _gpuId, value);
        }

        /// <summary>
        /// Tile大小 (显存不足时可以减小)
        /// </summary>
        private int _tileSize = 0;
        [DataMember]
        [DefaultValue(0)]
        public int TileSize
        {
            get => _tileSize;
            set => SetProperty(ref _tileSize, Math.Max(0, value));
        }

        /// <summary>
        /// 输出格式 (空字符串表示保持原格式)
        /// </summary>
        private string _outputFormat = "";
        [DataMember]
        [DefaultValue("")]
        public string OutputFormat
        {
            get => _outputFormat;
            set => SetProperty(ref _outputFormat, value ?? "");
        }

        /// <summary>
        /// 降噪等级 (-1, 0, 1, 2, 3)
        /// -1表示不降噪
        /// </summary>
        private int _noiseLevel = -1;
        [DataMember]
        [DefaultValue(-1)]
        public int NoiseLevel
        {
            get => _noiseLevel;
            set => SetProperty(ref _noiseLevel, Math.Max(-1, Math.Min(3, value)));
        }

        /// <summary>
        /// 最大并发处理数量
        /// </summary>
        private int _maxConcurrentProcessing = 2;
        [DataMember]
        [DefaultValue(2)]
        public int MaxConcurrentProcessing
        {
            get => _maxConcurrentProcessing;
            set => SetProperty(ref _maxConcurrentProcessing, Math.Max(1, Math.Min(8, value)));
        }

        /// <summary>
        /// 是否自动应用于查看的图片
        /// </summary>
        private bool _autoApplyOnView;
        [DataMember]
        [DefaultValue(false)]
        public bool AutoApplyOnView
        {
            get => _autoApplyOnView;
            set => SetProperty(ref _autoApplyOnView, value);
        }

        /// <summary>
        /// 是否缓存处理结果
        /// </summary>
        private bool _cacheResults = true;
        [DataMember]
        [DefaultValue(true)]
        public bool CacheResults
        {
            get => _cacheResults;
            set => SetProperty(ref _cacheResults, value);
        }

        /// <summary>
        /// 缓存路径
        /// </summary>
        private string _cachePath = "";
        [DataMember]
        [DefaultValue("")]
        public string CachePath
        {
            get => _cachePath;
            set => SetProperty(ref _cachePath, value ?? "");
        }

        /// <summary>
        /// Python 安装路径 (用于 Python 引擎)
        /// </summary>
        private string _pythonPath = "";
        [DataMember]
        [DefaultValue("")]
        public string PythonPath
        {
            get => _pythonPath;
            set => SetProperty(ref _pythonPath, value ?? "");
        }

        /// <summary>
        /// 单例实例
        /// </summary>
        [JsonIgnore]
        public static SuperResolutionConfig Current { get; set; } = new SuperResolutionConfig();
    }
}
