using NeeLaboratory.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;

namespace NeeView
{
    public partial class DatabaseInfoView : UserControl
    {
        private DatabaseInfoViewModel? _viewModel;

        public DatabaseInfoView()
        {
            InitializeComponent();

            _viewModel = new DatabaseInfoViewModel();
            this.DataContext = _viewModel;

            // 连接到 FileInformation.Current 的 PropertyChanged 事件
            FileInformation.Current.PropertyChanged += FileInformation_PropertyChanged;
            
            // 初始化
            UpdateSelectedSource();
        }

        private void FileInformation_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FileInformation.FileInformationCollection))
            {
                UpdateSelectedSource();
            }
        }

        private void UpdateSelectedSource()
        {
            var selectedSource = FileInformation.Current?.GetMainFileInformation();
            _viewModel?.SetSource(selectedSource);
        }
    }

    public class TagGroup
    {
        public string GroupName { get; set; } = "";
        public List<string> Tags { get; set; } = new();
    }

    public class DatabaseInfoViewModel : BindableBase
    {
        private FileInformationSource? _source;
        private string? _translatedTitle;
        private string? _originalTitle;
        private double _rating;
        private List<TagGroup> _tagGroups = new();

        public DatabaseInfoViewModel()
        {
        }

        public void SetSource(FileInformationSource? source)
        {
            _source = source;
            UpdateDatabaseInfo();
        }

        public string? TranslatedTitle
        {
            get => _translatedTitle;
            set => SetProperty(ref _translatedTitle, value);
        }

        public string? OriginalTitle
        {
            get => _originalTitle;
            set => SetProperty(ref _originalTitle, value);
        }

        public double Rating
        {
            get => _rating;
            set => SetProperty(ref _rating, value);
        }

        public List<TagGroup> TagGroups
        {
            get => _tagGroups;
            set => SetProperty(ref _tagGroups, value);
        }

        public bool HasTitle => !string.IsNullOrEmpty(DisplayTitle);
        public bool HasRating => Rating > 0;
        public bool HasTags => TagGroups.Any(g => g.Tags.Any());
        public bool HasAnyData => HasTitle || HasRating || HasTags;

        public string TitleLabel => Config.Current.Information.UseTranslation && !string.IsNullOrEmpty(TranslatedTitle) 
            ? "翻译标题:" 
            : "原标题:";

        public string? DisplayTitle
        {
            get
            {
                if (Config.Current.Information.UseTranslation && !string.IsNullOrEmpty(TranslatedTitle))
                    return TranslatedTitle;
                return OriginalTitle;
            }
        }

        public string? SecondaryTitle
        {
            get
            {
                if (Config.Current.Information.UseTranslation && !string.IsNullOrEmpty(TranslatedTitle))
                    return string.IsNullOrEmpty(OriginalTitle) ? null : $"原标题: {OriginalTitle}";
                return null;
            }
        }

        public Brush TitleColor
        {
            get
            {
                if (Config.Current.Information.UseTranslation && !string.IsNullOrEmpty(TranslatedTitle))
                    return new SolidColorBrush(Color.FromRgb(74, 144, 226)); // 蓝色
                return new SolidColorBrush(Colors.White);
            }
        }

        public string RatingText => Rating > 0 ? $"★ {Rating:F1}" : "";

        public Brush RatingColor
        {
            get
            {
                if (Rating >= 4.5) return new SolidColorBrush(Color.FromRgb(0, 200, 0)); // 绿色
                if (Rating >= 4.0) return new SolidColorBrush(Color.FromRgb(100, 200, 0)); // 黄绿色
                if (Rating >= 3.5) return new SolidColorBrush(Color.FromRgb(200, 200, 0)); // 黄色
                if (Rating >= 3.0) return new SolidColorBrush(Color.FromRgb(255, 150, 0)); // 橙色
                return new SolidColorBrush(Color.FromRgb(200, 0, 0)); // 红色
            }
        }

        private void UpdateDatabaseInfo()
        {
            if (_source == null)
            {
                TranslatedTitle = null;
                OriginalTitle = null;
                Rating = 0;
                TagGroups = new();
                NotifyAllProperties();
                return;
            }

            var filePath = _source.Page?.EntryFullName;
            if (string.IsNullOrEmpty(filePath))
            {
                TranslatedTitle = null;
                OriginalTitle = null;
                Rating = 0;
                TagGroups = new();
                NotifyAllProperties();
                return;
            }

            var dbInfo = DatabaseService.Instance.GetDatabaseInfo(filePath);
            if (dbInfo != null)
            {
                TranslatedTitle = dbInfo.TranslatedTitle;
                OriginalTitle = dbInfo.OriginalTitle;
                Rating = dbInfo.Rating;

                var groups = new List<TagGroup>();
                var tagNameMap = new Dictionary<string, string>
                {
                    ["female"] = "女性",
                    ["male"] = "男性",
                    ["parody"] = "原作",
                    ["artist"] = "画师",
                    ["character"] = "角色",
                    ["group"] = "团体",
                    ["language"] = "语言",
                    ["other"] = "其他"
                };

                foreach (var kvp in dbInfo.Tags)
                {
                    if (kvp.Value.Any())
                    {
                        groups.Add(new TagGroup
                        {
                            GroupName = tagNameMap.ContainsKey(kvp.Key) ? tagNameMap[kvp.Key] : kvp.Key,
                            Tags = kvp.Value
                        });
                    }
                }

                TagGroups = groups;
            }
            else
            {
                TranslatedTitle = null;
                OriginalTitle = null;
                Rating = 0;
                TagGroups = new();
            }

            NotifyAllProperties();
        }

        private void NotifyAllProperties()
        {
            RaisePropertyChanged(nameof(HasTitle));
            RaisePropertyChanged(nameof(HasRating));
            RaisePropertyChanged(nameof(HasTags));
            RaisePropertyChanged(nameof(HasAnyData));
            RaisePropertyChanged(nameof(TitleLabel));
            RaisePropertyChanged(nameof(DisplayTitle));
            RaisePropertyChanged(nameof(SecondaryTitle));
            RaisePropertyChanged(nameof(TitleColor));
            RaisePropertyChanged(nameof(RatingText));
            RaisePropertyChanged(nameof(RatingColor));
        }
    }
}
