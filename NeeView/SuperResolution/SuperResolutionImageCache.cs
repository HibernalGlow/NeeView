using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NeeView.SuperResolution
{
    /// <summary>
    /// 超分辨率图片状态
    /// </summary>
    public enum SuperResolutionImageStatus
    {
        /// <summary>
        /// 未超分
        /// </summary>
        None,
        
        /// <summary>
        /// 超分中
        /// </summary>
        Processing,
        
        /// <summary>
        /// 已超分
        /// </summary>
        Completed,
        
        /// <summary>
        /// 超分失败
        /// </summary>
        Failed
    }

    /// <summary>
    /// 超分辨率图片缓存项
    /// </summary>
    public class SuperResolutionCacheItem
    {
        public string OriginalPath { get; set; } = "";
        public byte[]? OriginalData { get; set; }
        public byte[]? SuperResolutionData { get; set; }
        public SuperResolutionImageStatus Status { get; set; } = SuperResolutionImageStatus.None;
        public string ErrorMessage { get; set; } = "";
        public DateTime LastAccessTime { get; set; } = DateTime.Now;
        public int OriginalWidth { get; set; }
        public int OriginalHeight { get; set; }
        public int SuperResolutionWidth { get; set; }
        public int SuperResolutionHeight { get; set; }
        public double ProcessingTime { get; set; }
    }

    /// <summary>
    /// 超分辨率图片缓存管理器
    /// 管理原图和超分图，支持快速切换显示
    /// </summary>
    public class SuperResolutionImageCache
    {
        private static readonly Lazy<SuperResolutionImageCache> _instance = new(() => new SuperResolutionImageCache());
        public static SuperResolutionImageCache Current => _instance.Value;

        private readonly ConcurrentDictionary<string, SuperResolutionCacheItem> _cache = new();
        private readonly SemaphoreSlim _cleanupSemaphore = new(1, 1);
        private const int MaxCacheSize = 50; // 最多缓存50张图片
        private const int MaxCacheSizeMB = 500; // 最大缓存500MB

        private SuperResolutionImageCache()
        {
        }

        /// <summary>
        /// 获取或创建缓存项
        /// </summary>
        public SuperResolutionCacheItem GetOrCreate(string path)
        {
            return _cache.GetOrAdd(path, _ => new SuperResolutionCacheItem
            {
                OriginalPath = path,
                Status = SuperResolutionImageStatus.None,
                LastAccessTime = DateTime.Now
            });
        }

        /// <summary>
        /// 获取缓存项
        /// </summary>
        public SuperResolutionCacheItem? Get(string path)
        {
            if (_cache.TryGetValue(path, out var item))
            {
                item.LastAccessTime = DateTime.Now;
                return item;
            }
            return null;
        }

        /// <summary>
        /// 更新缓存项
        /// </summary>
        public void Update(string path, Action<SuperResolutionCacheItem> updateAction)
        {
            var item = GetOrCreate(path);
            updateAction(item);
            item.LastAccessTime = DateTime.Now;
        }

        /// <summary>
        /// 移除缓存项
        /// </summary>
        public void Remove(string path)
        {
            _cache.TryRemove(path, out _);
        }

        /// <summary>
        /// 清理缓存（异步）
        /// </summary>
        public async Task CleanupAsync()
        {
            await _cleanupSemaphore.WaitAsync();
            try
            {
                // 如果缓存数量超过限制
                if (_cache.Count > MaxCacheSize)
                {
                    var itemsToRemove = _cache.Values
                        .OrderBy(x => x.LastAccessTime)
                        .Take(_cache.Count - MaxCacheSize)
                        .ToList();

                    foreach (var item in itemsToRemove)
                    {
                        _cache.TryRemove(item.OriginalPath, out _);
                    }
                }

                // 如果缓存大小超过限制
                var totalSize = _cache.Values.Sum(x =>
                    (x.OriginalData?.Length ?? 0) + (x.SuperResolutionData?.Length ?? 0));

                if (totalSize > MaxCacheSizeMB * 1024 * 1024)
                {
                    var itemsToRemove = _cache.Values
                        .OrderBy(x => x.LastAccessTime)
                        .ToList();

                    foreach (var item in itemsToRemove)
                    {
                        _cache.TryRemove(item.OriginalPath, out _);
                        
                        totalSize -= (item.OriginalData?.Length ?? 0) + (item.SuperResolutionData?.Length ?? 0);
                        if (totalSize <= MaxCacheSizeMB * 1024 * 1024)
                        {
                            break;
                        }
                    }
                }
            }
            finally
            {
                _cleanupSemaphore.Release();
            }
        }

        /// <summary>
        /// 清空所有缓存
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public (int count, long totalSize) GetCacheStats()
        {
            var count = _cache.Count;
            var totalSize = _cache.Values.Sum(x =>
                (x.OriginalData?.Length ?? 0) + (x.SuperResolutionData?.Length ?? 0));
            return (count, totalSize);
        }
    }
}
