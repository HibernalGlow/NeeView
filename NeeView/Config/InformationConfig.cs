using NeeLaboratory.ComponentModel;
using NeeView.Properties;
using NeeView.Windows.Controls;
using NeeView.Windows.Property;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows;

namespace NeeView
{
    public class InformationConfig : BindableBase
    {
        private static readonly string _defaultDateTimeFormat = TextResources.GetString("Information.DateFormat");
        private static readonly string _defaultMapProgramFormat = @"https://www.google.com/maps/place/$Lat+$Lon/";
        private GridLength _propertyHeaderWidth = new(128.0);

        private readonly Dictionary<InformationGroup, bool> _groupVisibilityMap = new()
        {
            [InformationGroup.File] = true,
            [InformationGroup.Image] = true,
            [InformationGroup.Description] = true,
            [InformationGroup.Origin] = true,
            [InformationGroup.Camera] = true,
            [InformationGroup.AdvancedPhoto] = true,
            [InformationGroup.Gps] = true,
            [InformationGroup.Extras] = false,
        };

        [JsonInclude, JsonPropertyName(nameof(DateTimeFormat))]
        public string? _dateTimeFormat = null;
        [JsonInclude, JsonPropertyName(nameof(MapProgramFormat))]
        public string? _mapProgramFormat;


        private bool SetVisibleGroup(InformationGroup group, bool isVisible, [CallerMemberName] string? propertyName = null)
        {
            if (_groupVisibilityMap[group] == isVisible) return false;

            _groupVisibilityMap[group] = isVisible;
            RaisePropertyChanged(propertyName);
            return true;
        }

        public bool IsVisibleGroup(InformationGroup group)
        {
            return _groupVisibilityMap[group];
        }


        [JsonIgnore]
        [PropertyMember]
        public string DateTimeFormat
        {
            get { return _dateTimeFormat ?? _defaultDateTimeFormat; }
            set { SetProperty(ref _dateTimeFormat, (string.IsNullOrWhiteSpace(value) || value == _defaultDateTimeFormat) ? null : value); }
        }

        [JsonIgnore]
        [PropertyMember]
        public string MapProgramFormat
        {
            get { return _mapProgramFormat ?? _defaultMapProgramFormat; }
            set { SetProperty(ref _mapProgramFormat, (string.IsNullOrWhiteSpace(value) || value == _defaultMapProgramFormat) ? null : value); }
        }


        [PropertyMember]
        public bool IsVisibleFile
        {
            get { return IsVisibleGroup(InformationGroup.File); }
            set { SetVisibleGroup(InformationGroup.File, value); }
        }

        [PropertyMember]
        public bool IsVisibleImage
        {
            get { return IsVisibleGroup(InformationGroup.Image); }
            set { SetVisibleGroup(InformationGroup.Image, value); }
        }

        [PropertyMember]
        public bool IsVisibleDescription
        {
            get { return IsVisibleGroup(InformationGroup.Description); }
            set { SetVisibleGroup(InformationGroup.Description, value); }
        }

        [PropertyMember]
        public bool IsVisibleOrigin
        {
            get { return IsVisibleGroup(InformationGroup.Origin); }
            set { SetVisibleGroup(InformationGroup.Origin, value); }
        }

        [PropertyMember]
        public bool IsVisibleCamera
        {
            get { return IsVisibleGroup(InformationGroup.Camera); }
            set { SetVisibleGroup(InformationGroup.Camera, value); }
        }

        [PropertyMember]
        public bool IsVisibleAdvancedPhoto
        {
            get { return IsVisibleGroup(InformationGroup.AdvancedPhoto); }
            set { SetVisibleGroup(InformationGroup.AdvancedPhoto, value); }
        }

        [PropertyMember]
        public bool IsVisibleGps
        {
            get { return IsVisibleGroup(InformationGroup.Gps); }
            set { SetVisibleGroup(InformationGroup.Gps, value); }
        }

        [PropertyMember]
        public bool IsVisibleExtras
        {
            get { return IsVisibleGroup(InformationGroup.Extras); }
            set { SetVisibleGroup(InformationGroup.Extras, value); }
        }

        #region Info Overlay

        private bool _isInfoOverlayEnabled = false;
        private double _infoOverlayFontSize = 10.0;
        private double _infoOverlayOpacity = 0.7;

        [PropertyMember]
        public bool IsInfoOverlayEnabled
        {
            get { return _isInfoOverlayEnabled; }
            set { SetProperty(ref _isInfoOverlayEnabled, value); }
        }

        [PropertyRange(8.0, 20.0, TickFrequency = 1)]
        [PropertyMember]
        public double InfoOverlayFontSize
        {
            get { return _infoOverlayFontSize; }
            set { SetProperty(ref _infoOverlayFontSize, value); }
        }

        [PropertyRange(0.3, 1.0, TickFrequency = 0.1)]
        [PropertyMember]
        public double InfoOverlayOpacity
        {
            get { return _infoOverlayOpacity; }
            set { SetProperty(ref _infoOverlayOpacity, value); }
        }

        #endregion Info Overlay

        #region Database

        private string _translationDatabasePath = @"D:\1SoftLink\AppData\Roaming\exhentai-manga-manager\translations.db";
        private string _metadataDatabasePath = @"D:\1SoftLink\AppData\Roaming\exhentai-manga-manager\database.sqlite";
        private string _tagTranslationPath = @"D:\1SoftLink\AppData\Roaming\exhentai-manga-manager\tag-translations.json";
        private bool _useTranslation = true;
        private bool _useTagTranslation = true;

        [PropertyPath(FileDialogType = FileDialogType.SaveFile, Filter = "SQLite Database|*.db;*.sqlite|All Files|*.*")]
        [PropertyMember]
        public string TranslationDatabasePath
        {
            get { return _translationDatabasePath; }
            set { SetProperty(ref _translationDatabasePath, value); }
        }

        [PropertyPath(FileDialogType = FileDialogType.SaveFile, Filter = "SQLite Database|*.db;*.sqlite|All Files|*.*")]
        [PropertyMember]
        public string MetadataDatabasePath
        {
            get { return _metadataDatabasePath; }
            set { SetProperty(ref _metadataDatabasePath, value); }
        }

        [PropertyPath(FileDialogType = FileDialogType.SaveFile, Filter = "JSON File|*.json|All Files|*.*")]
        [PropertyMember]
        public string TagTranslationPath
        {
            get { return _tagTranslationPath; }
            set { SetProperty(ref _tagTranslationPath, value); }
        }

        [PropertyMember]
        public bool UseTranslation
        {
            get { return _useTranslation; }
            set { SetProperty(ref _useTranslation, value); }
        }

        [PropertyMember]
        public bool UseTagTranslation
        {
            get { return _useTagTranslation; }
            set { SetProperty(ref _useTagTranslation, value); }
        }

        #endregion Database

        #region HiddenParameters

        [JsonIgnore]
        [PropertyMapIgnore]
        public ReadOnlyDictionary<InformationGroup, bool> GroupVisibilityMap => new(_groupVisibilityMap);

        [PropertyMapIgnore]
        public GridLength PropertyHeaderWidth
        {
            get { return _propertyHeaderWidth; }
            set { SetProperty(ref _propertyHeaderWidth, value); }
        }

        #endregion HiddenParameters
    }
}


