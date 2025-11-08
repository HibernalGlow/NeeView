using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NeeView
{
    public class DatabaseInfo
    {
        public string? TranslatedTitle { get; set; }
        public string? OriginalTitle { get; set; }
        public double Rating { get; set; }
        public Dictionary<string, List<string>> Tags { get; set; } = new();
    }

    public class DatabaseService
    {
        private static DatabaseService? _instance;
        public static DatabaseService Instance => _instance ??= new DatabaseService();

        private Dictionary<string, string>? _tagTranslations;
        private DateTime _tagTranslationsLoadTime = DateTime.MinValue;

        public DatabaseInfo? GetDatabaseInfo(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;

            var info = new DatabaseInfo();
            var fileName = Path.GetFileNameWithoutExtension(filePath);

            // 获取翻译标题
            var titleInfo = GetTranslatedTitle(fileName);
            if (titleInfo.HasValue)
            {
                info.TranslatedTitle = titleInfo.Value.ChineseTitle;
                info.OriginalTitle = titleInfo.Value.OriginalEnglish ?? titleInfo.Value.OriginalJapanese;
            }

            // 获取元数据
            var metadata = GetMetadata(filePath);
            if (metadata.HasValue)
            {
                info.Rating = metadata.Value.Rating;
                info.Tags = metadata.Value.Tags;
            }

            return info;
        }

        private (string? ChineseTitle, string? OriginalEnglish, string? OriginalJapanese)? GetTranslatedTitle(string fileName)
        {
            try
            {
                if (!Config.Current.Information.UseTranslation) return null;

                var dbPath = Config.Current.Information.TranslationDatabasePath;
                if (!File.Exists(dbPath)) return null;

                using var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
                connection.Open();

                using var command = connection.CreateCommand();
                // 使用 hash 或 filename 字段查询
                command.CommandText = "SELECT chinese_title, original_english, original_japanese FROM translations WHERE filename LIKE @filename OR hash = @filename";
                command.Parameters.AddWithValue("@filename", $"%{fileName}%");

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return (
                        reader.IsDBNull(0) ? null : reader.GetString(0),
                        reader.IsDBNull(1) ? null : reader.GetString(1),
                        reader.IsDBNull(2) ? null : reader.GetString(2)
                    );
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DatabaseService] GetTranslatedTitle error: {ex.Message}");
            }
            return null;
        }

        private (double Rating, Dictionary<string, List<string>> Tags)? GetMetadata(string filePath)
        {
            try
            {
                var dbPath = Config.Current.Information.MetadataDatabasePath;
                if (!File.Exists(dbPath)) return null;

                using var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = "SELECT rating, tags FROM metadata WHERE file_path = @path";
                command.Parameters.AddWithValue("@path", filePath);

                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    var rating = reader.IsDBNull(0) ? 0.0 : reader.GetDouble(0);
                    var tagsJson = reader.IsDBNull(1) ? null : reader.GetString(1);

                    var tags = new Dictionary<string, List<string>>();
                    if (!string.IsNullOrEmpty(tagsJson))
                    {
                        try
                        {
                            var tagsList = JsonSerializer.Deserialize<List<string>>(tagsJson);
                            if (tagsList != null)
                            {
                                tags = ParseTags(tagsList);
                            }
                        }
                        catch { }
                    }

                    return (rating, tags);
                }
            }
            catch
            {
            }

            return null;
        }

        private Dictionary<string, List<string>> ParseTags(List<string> tagsList)
        {
            var result = new Dictionary<string, List<string>>
            {
                ["female"] = new List<string>(),
                ["male"] = new List<string>(),
                ["parody"] = new List<string>(),
                ["artist"] = new List<string>(),
                ["character"] = new List<string>(),
                ["group"] = new List<string>(),
                ["language"] = new List<string>(),
                ["other"] = new List<string>()
            };

            LoadTagTranslations();

            foreach (var tag in tagsList)
            {
                string tagName;
                string category;

                if (tag.StartsWith("female:"))
                {
                    category = "female";
                    tagName = tag.Substring(7);
                }
                else if (tag.StartsWith("male:"))
                {
                    category = "male";
                    tagName = tag.Substring(5);
                }
                else if (tag.StartsWith("parody:"))
                {
                    category = "parody";
                    tagName = tag.Substring(7);
                }
                else if (tag.StartsWith("artist:"))
                {
                    category = "artist";
                    tagName = tag.Substring(7);
                }
                else if (tag.StartsWith("character:"))
                {
                    category = "character";
                    tagName = tag.Substring(10);
                }
                else if (tag.StartsWith("group:"))
                {
                    category = "group";
                    tagName = tag.Substring(6);
                }
                else if (tag.StartsWith("language:"))
                {
                    category = "language";
                    tagName = tag.Substring(9);
                }
                else
                {
                    category = "other";
                    tagName = tag;
                }

                // 应用翻译
                if (Config.Current.Information.UseTagTranslation && _tagTranslations != null)
                {
                    if (_tagTranslations.TryGetValue(tagName, out var translated))
                    {
                        tagName = translated;
                    }
                }

                result[category].Add(tagName);
            }

            return result;
        }

        private void LoadTagTranslations()
        {
            try
            {
                if (!Config.Current.Information.UseTagTranslation)
                {
                    _tagTranslations = null;
                    return;
                }

                var jsonPath = Config.Current.Information.TagTranslationPath;
                if (!File.Exists(jsonPath)) return;

                // 检查文件修改时间,避免频繁加载
                var lastWriteTime = File.GetLastWriteTime(jsonPath);
                if (_tagTranslations != null && lastWriteTime <= _tagTranslationsLoadTime)
                {
                    return;
                }

                var json = File.ReadAllText(jsonPath);
                _tagTranslations = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                _tagTranslationsLoadTime = lastWriteTime;

                System.Diagnostics.Debug.WriteLine($"[DatabaseService] Loaded {_tagTranslations?.Count ?? 0} tag translations");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DatabaseService] LoadTagTranslations error: {ex.Message}");
                _tagTranslations = null;
            }
        }
    }
}
