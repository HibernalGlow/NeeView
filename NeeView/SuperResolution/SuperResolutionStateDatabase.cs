using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;

namespace NeeView
{
    /// <summary>
    /// 超分辨率状态数据库连接
    /// 持久化存储每张图片的用户偏好和实际状态
    /// </summary>
    public class SuperResolutionStateDatabase : IDisposable
    {
        private const string _format = "1.0";
        private readonly SQLiteConnection _connection;
        private bool _disposedValue;

        public SuperResolutionStateDatabase(string path)
        {
            try
            {
                // 确保目录存在
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                _connection = new SQLiteConnection($"Data Source={path}");
                _connection.Open();

                InitializePragma();
                CreatePropertyTable();

                if (IsSupportFormat())
                {
                    CreateStateTable();
                }
                else
                {
                    // 格式不匹配,重建数据库
                    _connection.Close();
                    _connection.Dispose();
                    File.Delete(path);

                    _connection = new SQLiteConnection($"Data Source={path}");
                    _connection.Open();
                    InitializePragma();
                    CreatePropertyTable();
                    SaveProperty("Format", _format);
                    CreateStateTable();
                }
            }
            catch
            {
                _connection?.Close();
                _connection?.Dispose();
                throw;
            }
        }

        [Conditional("DEBUG")]
        private static void LocalWriteLine(string format, params object[] args)
        {
            //Debug.WriteLine($"[SRDB:{System.Environment.CurrentManagedThreadId}] {format}", args);
        }

        /// <summary>
        /// 初始化 PRAGMA 设置
        /// </summary>
        private void InitializePragma()
        {
            using (SQLiteCommand command = _connection.CreateCommand())
            {
                // WAL 模式提高并发性能
                command.CommandText = "PRAGMA journal_mode = WAL;";
                command.ExecuteNonQuery();

                // 同步模式 NORMAL (平衡性能和安全)
                command.CommandText = "PRAGMA synchronous = NORMAL;";
                command.ExecuteNonQuery();

                // 临时文件存储在内存
                command.CommandText = "PRAGMA temp_store = MEMORY;";
                command.ExecuteNonQuery();

                // 缓存大小 10MB
                command.CommandText = "PRAGMA cache_size = -10000;";
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 创建属性表
        /// </summary>
        private void CreatePropertyTable()
        {
            using (SQLiteCommand command = _connection.CreateCommand())
            {
                command.CommandText = "CREATE TABLE IF NOT EXISTS Property (Key TEXT PRIMARY KEY, Value TEXT);";
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 格式检查
        /// </summary>
        private bool IsSupportFormat()
        {
            var format = LoadProperty("Format");
            return format == _format;
        }

        /// <summary>
        /// 保存属性
        /// </summary>
        private void SaveProperty(string key, string value)
        {
            using (SQLiteCommand command = _connection.CreateCommand())
            {
                command.CommandText = "INSERT OR REPLACE INTO Property (Key, Value) VALUES (@Key, @Value);";
                command.Parameters.AddWithValue("@Key", key);
                command.Parameters.AddWithValue("@Value", value);
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 读取属性
        /// </summary>
        private string? LoadProperty(string key)
        {
            using (SQLiteCommand command = _connection.CreateCommand())
            {
                command.CommandText = "SELECT Value FROM Property WHERE Key = @Key;";
                command.Parameters.AddWithValue("@Key", key);
                return command.ExecuteScalar() as string;
            }
        }

        /// <summary>
        /// 创建状态表
        /// </summary>
        private void CreateStateTable()
        {
            using (SQLiteCommand command = _connection.CreateCommand())
            {
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS SuperResolutionState (
                        ImagePath TEXT PRIMARY KEY,
                        UserPreference INTEGER NOT NULL DEFAULT 0,
                        ActualStatus INTEGER NOT NULL DEFAULT 0,
                        OriginalWidth INTEGER,
                        OriginalHeight INTEGER,
                        SuperResolutionWidth INTEGER,
                        SuperResolutionHeight INTEGER,
                        CacheLocation TEXT,
                        LastModified INTEGER NOT NULL,
                        CreatedTime INTEGER NOT NULL
                    );
                ";
                command.ExecuteNonQuery();

                // 创建索引
                command.CommandText = "CREATE INDEX IF NOT EXISTS idx_user_preference ON SuperResolutionState(UserPreference);";
                command.ExecuteNonQuery();

                command.CommandText = "CREATE INDEX IF NOT EXISTS idx_actual_status ON SuperResolutionState(ActualStatus);";
                command.ExecuteNonQuery();

                command.CommandText = "CREATE INDEX IF NOT EXISTS idx_last_modified ON SuperResolutionState(LastModified);";
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 查询图片状态
        /// </summary>
        public SuperResolutionStateRecord? GetState(string imagePath)
        {
            if (_disposedValue) return null;

            LocalWriteLine($"GetState: {imagePath}");

            using (SQLiteCommand command = _connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT ImagePath, UserPreference, ActualStatus, 
                           OriginalWidth, OriginalHeight, 
                           SuperResolutionWidth, SuperResolutionHeight,
                           CacheLocation, LastModified, CreatedTime
                    FROM SuperResolutionState 
                    WHERE ImagePath = @ImagePath;
                ";
                command.Parameters.AddWithValue("@ImagePath", imagePath);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new SuperResolutionStateRecord
                        {
                            ImagePath = reader.GetString(0),
                            UserPreference = (SuperResolutionUserPreference)reader.GetInt32(1),
                            ActualStatus = (SuperResolutionActualStatus)reader.GetInt32(2),
                            OriginalWidth = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                            OriginalHeight = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                            SuperResolutionWidth = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                            SuperResolutionHeight = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                            CacheLocation = reader.IsDBNull(7) ? null : reader.GetString(7),
                            LastModified = reader.GetInt64(8),
                            CreatedTime = reader.GetInt64(9)
                        };
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 保存或更新图片状态
        /// </summary>
        public void SaveState(SuperResolutionStateRecord record)
        {
            if (_disposedValue) return;

            LocalWriteLine($"SaveState: {record.ImagePath} | Pref={record.UserPreference}, Status={record.ActualStatus}");

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            record.LastModified = now;

            using (SQLiteCommand command = _connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT OR REPLACE INTO SuperResolutionState 
                    (ImagePath, UserPreference, ActualStatus, 
                     OriginalWidth, OriginalHeight, 
                     SuperResolutionWidth, SuperResolutionHeight,
                     CacheLocation, LastModified, CreatedTime)
                    VALUES 
                    (@ImagePath, @UserPreference, @ActualStatus,
                     @OriginalWidth, @OriginalHeight,
                     @SuperResolutionWidth, @SuperResolutionHeight,
                     @CacheLocation, @LastModified, 
                     COALESCE((SELECT CreatedTime FROM SuperResolutionState WHERE ImagePath = @ImagePath), @CreatedTime));
                ";

                command.Parameters.AddWithValue("@ImagePath", record.ImagePath);
                command.Parameters.AddWithValue("@UserPreference", (int)record.UserPreference);
                command.Parameters.AddWithValue("@ActualStatus", (int)record.ActualStatus);
                command.Parameters.AddWithValue("@OriginalWidth", (object?)record.OriginalWidth ?? DBNull.Value);
                command.Parameters.AddWithValue("@OriginalHeight", (object?)record.OriginalHeight ?? DBNull.Value);
                command.Parameters.AddWithValue("@SuperResolutionWidth", (object?)record.SuperResolutionWidth ?? DBNull.Value);
                command.Parameters.AddWithValue("@SuperResolutionHeight", (object?)record.SuperResolutionHeight ?? DBNull.Value);
                command.Parameters.AddWithValue("@CacheLocation", (object?)record.CacheLocation ?? DBNull.Value);
                command.Parameters.AddWithValue("@LastModified", record.LastModified);
                command.Parameters.AddWithValue("@CreatedTime", now);

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 更新用户偏好
        /// </summary>
        public void UpdateUserPreference(string imagePath, SuperResolutionUserPreference preference)
        {
            if (_disposedValue) return;

            LocalWriteLine($"UpdateUserPreference: {imagePath} -> {preference}");

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            using (SQLiteCommand command = _connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT OR REPLACE INTO SuperResolutionState 
                    (ImagePath, UserPreference, ActualStatus, LastModified, CreatedTime)
                    VALUES 
                    (@ImagePath, @UserPreference, 
                     COALESCE((SELECT ActualStatus FROM SuperResolutionState WHERE ImagePath = @ImagePath), 0),
                     @LastModified,
                     COALESCE((SELECT CreatedTime FROM SuperResolutionState WHERE ImagePath = @ImagePath), @CreatedTime));
                ";

                command.Parameters.AddWithValue("@ImagePath", imagePath);
                command.Parameters.AddWithValue("@UserPreference", (int)preference);
                command.Parameters.AddWithValue("@LastModified", now);
                command.Parameters.AddWithValue("@CreatedTime", now);

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 更新实际状态
        /// </summary>
        public void UpdateActualStatus(string imagePath, SuperResolutionActualStatus status)
        {
            if (_disposedValue) return;

            LocalWriteLine($"UpdateActualStatus: {imagePath} -> {status}");

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            using (SQLiteCommand command = _connection.CreateCommand())
            {
                command.CommandText = @"
                    INSERT OR REPLACE INTO SuperResolutionState 
                    (ImagePath, UserPreference, ActualStatus, LastModified, CreatedTime)
                    VALUES 
                    (@ImagePath, 
                     COALESCE((SELECT UserPreference FROM SuperResolutionState WHERE ImagePath = @ImagePath), 0),
                     @ActualStatus,
                     @LastModified,
                     COALESCE((SELECT CreatedTime FROM SuperResolutionState WHERE ImagePath = @ImagePath), @CreatedTime));
                ";

                command.Parameters.AddWithValue("@ImagePath", imagePath);
                command.Parameters.AddWithValue("@ActualStatus", (int)status);
                command.Parameters.AddWithValue("@LastModified", now);
                command.Parameters.AddWithValue("@CreatedTime", now);

                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 删除旧记录 (超过指定天数)
        /// </summary>
        public void DeleteOldRecords(int days)
        {
            if (_disposedValue) return;

            var threshold = DateTimeOffset.UtcNow.AddDays(-days).ToUnixTimeSeconds();

            using (SQLiteCommand command = _connection.CreateCommand())
            {
                command.CommandText = "DELETE FROM SuperResolutionState WHERE LastModified < @Threshold;";
                command.Parameters.AddWithValue("@Threshold", threshold);
                var count = command.ExecuteNonQuery();

                LocalWriteLine($"DeleteOldRecords: Deleted {count} records older than {days} days");
            }
        }

        /// <summary>
        /// VACUUM 优化数据库
        /// </summary>
        public void Vacuum()
        {
            if (_disposedValue) return;

            using (SQLiteCommand command = _connection.CreateCommand())
            {
                command.CommandText = "VACUUM;";
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 获取数据库统计信息
        /// </summary>
        public SuperResolutionDatabaseStats GetStats()
        {
            if (_disposedValue) return new SuperResolutionDatabaseStats();

            using (SQLiteCommand command = _connection.CreateCommand())
            {
                command.CommandText = @"
                    SELECT 
                        COUNT(*) as TotalRecords,
                        SUM(CASE WHEN UserPreference = 1 THEN 1 ELSE 0 END) as EnabledCount,
                        SUM(CASE WHEN UserPreference = 2 THEN 1 ELSE 0 END) as DisabledCount,
                        SUM(CASE WHEN ActualStatus = 2 THEN 1 ELSE 0 END) as CompletedCount,
                        SUM(CASE WHEN ActualStatus = 3 THEN 1 ELSE 0 END) as FailedCount
                    FROM SuperResolutionState;
                ";

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new SuperResolutionDatabaseStats
                        {
                            TotalRecords = reader.GetInt32(0),
                            EnabledCount = reader.GetInt32(1),
                            DisabledCount = reader.GetInt32(2),
                            CompletedCount = reader.GetInt32(3),
                            FailedCount = reader.GetInt32(4)
                        };
                    }
                }
            }

            return new SuperResolutionDatabaseStats();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _connection?.Close();
                    _connection?.Dispose();
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

    /// <summary>
    /// 用户偏好枚举
    /// </summary>
    public enum SuperResolutionUserPreference
    {
        /// <summary>无偏好 (自动根据条件判断)</summary>
        Auto = 0,

        /// <summary>用户明确启用</summary>
        Enabled = 1,

        /// <summary>用户明确禁用</summary>
        Disabled = 2
    }

    /// <summary>
    /// 实际状态枚举
    /// </summary>
    public enum SuperResolutionActualStatus
    {
        /// <summary>未超分</summary>
        None = 0,

        /// <summary>处理中</summary>
        Processing = 1,

        /// <summary>已完成</summary>
        Completed = 2,

        /// <summary>失败</summary>
        Failed = 3
    }

    /// <summary>
    /// 状态记录
    /// </summary>
    public class SuperResolutionStateRecord
    {
        public string ImagePath { get; set; } = string.Empty;
        public SuperResolutionUserPreference UserPreference { get; set; }
        public SuperResolutionActualStatus ActualStatus { get; set; }
        public int? OriginalWidth { get; set; }
        public int? OriginalHeight { get; set; }
        public int? SuperResolutionWidth { get; set; }
        public int? SuperResolutionHeight { get; set; }
        public string? CacheLocation { get; set; }
        public long LastModified { get; set; }
        public long CreatedTime { get; set; }
    }

    /// <summary>
    /// 数据库统计信息
    /// </summary>
    public class SuperResolutionDatabaseStats
    {
        public int TotalRecords { get; set; }
        public int EnabledCount { get; set; }
        public int DisabledCount { get; set; }
        public int CompletedCount { get; set; }
        public int FailedCount { get; set; }
    }
}
