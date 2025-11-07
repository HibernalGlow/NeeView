using System;
using System.Diagnostics;
using System.IO;

namespace NeeView
{
    /// <summary>
    /// 超分辨率状态管理器
    /// 单例模式,负责协调数据库和缓存,记录状态变更日志
    /// </summary>
    public class SuperResolutionStateManager : IDisposable
    {
        private static SuperResolutionStateManager? _current;
        private static readonly object _lock = new object();

        public static SuperResolutionStateManager Current
        {
            get
            {
                if (_current == null)
                {
                    lock (_lock)
                    {
                        if (_current == null)
                        {
                            _current = new SuperResolutionStateManager();
                        }
                    }
                }
                return _current;
            }
        }

        private SuperResolutionStateDatabase? _database;
        private readonly System.Threading.Lock _dbLock = new();
        private bool _disposedValue;
        private string? _databasePath;

        private SuperResolutionStateManager()
        {
        }

        /// <summary>
        /// 初始化数据库路径
        /// </summary>
        public void Initialize(string databasePath)
        {
            lock (_dbLock)
            {
                if (_database != null && _databasePath == databasePath)
                {
                    return; // 已初始化
                }

                _databasePath = databasePath;

                try
                {
                    _database?.Dispose();
                    _database = new SuperResolutionStateDatabase(databasePath);
                    LogStateChange("System", "Database initialized", $"Path={databasePath}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SuperResolutionStateManager] Failed to initialize database: {ex.Message}");
                    _database = null;
                }
            }
        }

        /// <summary>
        /// 获取用户偏好
        /// </summary>
        public SuperResolutionUserPreference GetUserPreference(string imagePath)
        {
            lock (_dbLock)
            {
                if (_database == null) return SuperResolutionUserPreference.Auto;

                try
                {
                    var record = _database.GetState(imagePath);
                    return record?.UserPreference ?? SuperResolutionUserPreference.Auto;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SuperResolutionStateManager] GetUserPreference failed: {ex.Message}");
                    return SuperResolutionUserPreference.Auto;
                }
            }
        }

        /// <summary>
        /// 设置用户偏好
        /// </summary>
        public void SetUserPreference(string imagePath, SuperResolutionUserPreference preference, string trigger)
        {
            lock (_dbLock)
            {
                if (_database == null) return;

                try
                {
                    var oldPreference = GetUserPreference(imagePath);
                    _database.UpdateUserPreference(imagePath, preference);
                    LogStateChange(imagePath, $"UserPreference: {oldPreference} → {preference}", $"Trigger={trigger}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SuperResolutionStateManager] SetUserPreference failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 获取实际状态
        /// </summary>
        public SuperResolutionActualStatus GetActualStatus(string imagePath)
        {
            lock (_dbLock)
            {
                if (_database == null) return SuperResolutionActualStatus.None;

                try
                {
                    var record = _database.GetState(imagePath);
                    return record?.ActualStatus ?? SuperResolutionActualStatus.None;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SuperResolutionStateManager] GetActualStatus failed: {ex.Message}");
                    return SuperResolutionActualStatus.None;
                }
            }
        }

        /// <summary>
        /// 设置实际状态
        /// </summary>
        public void SetActualStatus(string imagePath, SuperResolutionActualStatus status, string trigger)
        {
            lock (_dbLock)
            {
                if (_database == null) return;

                try
                {
                    var oldStatus = GetActualStatus(imagePath);
                    _database.UpdateActualStatus(imagePath, status);
                    LogStateChange(imagePath, $"ActualStatus: {oldStatus} → {status}", $"Trigger={trigger}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SuperResolutionStateManager] SetActualStatus failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 获取完整记录
        /// </summary>
        public SuperResolutionStateRecord? GetRecord(string imagePath)
        {
            lock (_dbLock)
            {
                if (_database == null) return null;

                try
                {
                    return _database.GetState(imagePath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SuperResolutionStateManager] GetRecord failed: {ex.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// 保存完整记录
        /// </summary>
        public void SaveRecord(SuperResolutionStateRecord record, string trigger)
        {
            lock (_dbLock)
            {
                if (_database == null) return;

                try
                {
                    var oldRecord = _database.GetState(record.ImagePath);
                    _database.SaveState(record);

                    var changes = "";
                    if (oldRecord == null)
                    {
                        changes = $"New record | Pref={record.UserPreference}, Status={record.ActualStatus}";
                    }
                    else
                    {
                        if (oldRecord.UserPreference != record.UserPreference)
                            changes += $"Pref: {oldRecord.UserPreference}→{record.UserPreference} ";
                        if (oldRecord.ActualStatus != record.ActualStatus)
                            changes += $"Status: {oldRecord.ActualStatus}→{record.ActualStatus} ";
                    }

                    LogStateChange(record.ImagePath, changes, $"Trigger={trigger}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SuperResolutionStateManager] SaveRecord failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 判断是否应该跳过超分
        /// 严格的决策逻辑:
        /// 1. 如果用户偏好是 Disabled,始终跳过
        /// 2. 如果用户偏好是 Enabled,始终不跳过 (强制超分)
        /// 3. 如果用户偏好是 Auto,由调用者根据其他条件决定
        /// </summary>
        public bool ShouldSkipSuperResolution(string imagePath, out SuperResolutionUserPreference preference)
        {
            preference = GetUserPreference(imagePath);

            switch (preference)
            {
                case SuperResolutionUserPreference.Disabled:
                    LogStateChange(imagePath, "Decision: SKIP", "Reason=UserDisabled");
                    return true;

                case SuperResolutionUserPreference.Enabled:
                    LogStateChange(imagePath, "Decision: PROCESS", "Reason=UserEnabled");
                    return false;

                case SuperResolutionUserPreference.Auto:
                    // 自动模式,不在这里决定,返回 false 让调用者检查其他条件
                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// 记录状态变更日志
        /// 格式: [时间] [STATE] 图片路径 | 变更内容 | 触发原因
        /// </summary>
        private void LogStateChange(string imagePath, string change, string trigger)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var shortPath = string.IsNullOrEmpty(imagePath) ? imagePath : Path.GetFileName(imagePath);
            var message = $"[{timestamp}] [STATE] {shortPath} | {change} | {trigger}";

            // 写入开发日志
            SuperResolution.SuperResolutionLogger.DebugLog(message);

            // 同时输出到调试窗口
            Debug.WriteLine($"[SuperResolutionStateManager] {message}");
        }

        /// <summary>
        /// 清理旧记录
        /// </summary>
        public void CleanupOldRecords(int days = 30)
        {
            lock (_dbLock)
            {
                if (_database == null) return;

                try
                {
                    _database.DeleteOldRecords(days);
                    LogStateChange("System", $"Cleanup completed", $"DeletedOlderThan={days}days");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SuperResolutionStateManager] CleanupOldRecords failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 优化数据库
        /// </summary>
        public void OptimizeDatabase()
        {
            lock (_dbLock)
            {
                if (_database == null) return;

                try
                {
                    _database.Vacuum();
                    LogStateChange("System", "Database optimized", "VACUUM completed");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SuperResolutionStateManager] OptimizeDatabase failed: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 获取统计信息
        /// </summary>
        public SuperResolutionDatabaseStats? GetStats()
        {
            lock (_dbLock)
            {
                if (_database == null) return null;

                try
                {
                    return _database.GetStats();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SuperResolutionStateManager] GetStats failed: {ex.Message}");
                    return null;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    lock (_dbLock)
                    {
                        _database?.Dispose();
                        _database = null;
                    }
                    LogStateChange("System", "StateManager disposed", "");
                }
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
