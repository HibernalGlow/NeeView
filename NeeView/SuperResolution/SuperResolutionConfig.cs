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
        private bool _isEnabled = true;
        [DataMember]
        [DefaultValue(true)]
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
        /// 预加载后续图片数量 (翻页时自动超分后续N张图片)
        /// </summary>
        private int _preloadCount = 3;
        [DataMember]
        [DefaultValue(3)]
        public int PreloadCount
        {
            get => _preloadCount;
            set => SetProperty(ref _preloadCount, Math.Max(0, Math.Min(10, value)));
        }

        // 🎯 注意: AutoApplyOnView 已删除,默认启用条件筛选
        
        /// <summary>
        /// 自动超分的最大图片尺寸 (宽或高,像素)
        /// 超过此尺寸的图片不会自动超分,避免内存溢出
        /// </summary>
        private int _autoApplyMaxSize = 4096;
        [DataMember]
        [DefaultValue(4096)]
        public int AutoApplyMaxSize
        {
            get => _autoApplyMaxSize;
            set => SetProperty(ref _autoApplyMaxSize, Math.Max(256, value));
        }

        /// <summary>
        /// 自动超分的最小图片宽度 (像素, -1 表示无限制)
        /// </summary>
        private int _autoApplyMinWidth = -1;
        [DataMember]
        [DefaultValue(-1)]
        public int AutoApplyMinWidth
        {
            get => _autoApplyMinWidth;
            set => SetProperty(ref _autoApplyMinWidth, value);
        }

        /// <summary>
        /// 自动超分的最大图片宽度 (像素, -1 表示无限制)
        /// </summary>
        private int _autoApplyMaxWidth = -1;
        [DataMember]
        [DefaultValue(-1)]
        public int AutoApplyMaxWidth
        {
            get => _autoApplyMaxWidth;
            set => SetProperty(ref _autoApplyMaxWidth, value);
        }

        /// <summary>
        /// 自动超分的最小图片高度 (像素, -1 表示无限制)
        /// </summary>
        private int _autoApplyMinHeight = -1;
        [DataMember]
        [DefaultValue(-1)]
        public int AutoApplyMinHeight
        {
            get => _autoApplyMinHeight;
            set => SetProperty(ref _autoApplyMinHeight, value);
        }

        /// <summary>
        /// 自动超分的最大图片高度 (像素, -1 表示无限制)
        /// </summary>
        private int _autoApplyMaxHeight = -1;
        [DataMember]
        [DefaultValue(-1)]
        public int AutoApplyMaxHeight
        {
            get => _autoApplyMaxHeight;
            set => SetProperty(ref _autoApplyMaxHeight, value);
        }

        /// <summary>
        /// 自动超分的最小文件大小 (KB, -1 表示无限制)
        /// </summary>
        private int _autoApplyMinFileSize = -1;
        [DataMember]
        [DefaultValue(-1)]
        public int AutoApplyMinFileSize
        {
            get => _autoApplyMinFileSize;
            set => SetProperty(ref _autoApplyMinFileSize, value);
        }

        /// <summary>
        /// 自动超分的最大文件大小 (KB, -1 表示无限制)
        /// </summary>
        private int _autoApplyMaxFileSize = -1;
        [DataMember]
        [DefaultValue(-1)]
        public int AutoApplyMaxFileSize
        {
            get => _autoApplyMaxFileSize;
            set => SetProperty(ref _autoApplyMaxFileSize, value);
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
        /// 是否启用混合缓存（内存+磁盘）
        /// </summary>
        private bool _enableHybridCache = true;
        [DataMember]
        [DefaultValue(true)]
        public bool EnableHybridCache
        {
            get => _enableHybridCache;
            set => SetProperty(ref _enableHybridCache, value);
        }

        /// <summary>
        /// 内存缓存最大数量
        /// </summary>
        private int _memoryCacheMaxCount = 10;
        [DataMember]
        [DefaultValue(10)]
        public int MemoryCacheMaxCount
        {
            get => _memoryCacheMaxCount;
            set => SetProperty(ref _memoryCacheMaxCount, Math.Max(1, Math.Min(50, value)));
        }

        /// <summary>
        /// 内存缓存最大大小 (MB)
        /// </summary>
        private int _memoryCacheMaxSizeMB = 100;
        [DataMember]
        [DefaultValue(100)]
        public int MemoryCacheMaxSizeMB
        {
            get => _memoryCacheMaxSizeMB;
            set => SetProperty(ref _memoryCacheMaxSizeMB, Math.Max(10, Math.Min(1024, value)));
        }

        /// <summary>
        /// 磁盘缓存最大大小 (MB)
        /// </summary>
        private int _diskCacheMaxSizeMB = 5120;
        [DataMember]
        [DefaultValue(5120)]
        public int DiskCacheMaxSizeMB
        {
            get => _diskCacheMaxSizeMB;
            set => SetProperty(ref _diskCacheMaxSizeMB, Math.Max(100, Math.Min(10240, value)));
        }

        /// <summary>
        /// 磁盘缓存最大文件数量
        /// </summary>
        private int _diskCacheMaxFiles = 10000;
        [DataMember]
        [DefaultValue(10000)]
        public int DiskCacheMaxFiles
        {
            get => _diskCacheMaxFiles;
            set => SetProperty(ref _diskCacheMaxFiles, Math.Max(100, Math.Min(50000, value)));
        }

        /// <summary>
        /// 内存缓存过期时间 (小时)
        /// </summary>
        private int _memoryCacheExpirationHours = 2;
        [DataMember]
        [DefaultValue(2)]
        public int MemoryCacheExpirationHours
        {
            get => _memoryCacheExpirationHours;
            set => SetProperty(ref _memoryCacheExpirationHours, Math.Max(1, Math.Min(24, value)));
        }

        /// <summary>
        /// 磁盘缓存过期时间 (天)
        /// </summary>
        private int _diskCacheExpirationDays = 7;
        [DataMember]
        [DefaultValue(7)]
        public int DiskCacheExpirationDays
        {
            get => _diskCacheExpirationDays;
            set => SetProperty(ref _diskCacheExpirationDays, Math.Max(1, Math.Min(90, value)));
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
        /// sr_vulkan 模型文件路径
        /// 空字符串表示使用默认路径 (~/.cache/sr-vulkan/)
        /// </summary>
        private string _modelPath = "";
        [DataMember]
        [DefaultValue("")]
        public string ModelPath
        {
            get => _modelPath;
            set => SetProperty(ref _modelPath, value ?? "");
        }

        /// <summary>
        /// 单例实例
        /// </summary>
        [JsonIgnore]
        public static SuperResolutionConfig Current { get; set; } = new SuperResolutionConfig();
    }
}
