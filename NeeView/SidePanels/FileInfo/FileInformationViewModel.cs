using NeeLaboratory;
using NeeLaboratory.ComponentModel;
using NeeLaboratory.Windows.Input;
using NeeView.Media.Imaging.Metadata;
using NeeView.Properties;
using NeeView.Windows.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace NeeView
{
    public class FileInformationViewModel : BindableBase
    {
        private readonly FileInformation _model;
        private FileInformationSource? _selectedItem;
        private RelayCommand? _exportImageCommand;


        public FileInformationViewModel(FileInformation model)
        {
            _model = model;

            _model.AddPropertyChanged(nameof(_model.FileInformationCollection),
                Model_FileInformationCollectionChanged);
            
            SelectedItem = _model.GetMainFileInformation();

            MoreMenuDescription = new FileInformationMoreMenuDescription();
        }


        public List<FileInformationSource>? FileInformationCollection
        {
            get { return _model.FileInformationCollection; }
        }

        public FileInformationSource? SelectedItem
        {
            get { return _selectedItem; }
            set { SetProperty(ref _selectedItem, value); }
        }


        #region MoreMenu

        public FileInformationMoreMenuDescription MoreMenuDescription { get; }

        public class FileInformationMoreMenuDescription : MoreMenuDescription
        {
            public override ContextMenu Create()
            {
                var menu = new ContextMenu();
                menu.Items.Add(CreateCheckMenuItem(TextResources.GetString("InformationGroup.File"), new Binding(nameof(InformationConfig.IsVisibleFile)) { Source = Config.Current.Information }));
                menu.Items.Add(CreateCheckMenuItem(TextResources.GetString("InformationGroup.Image"), new Binding(nameof(InformationConfig.IsVisibleImage)) { Source = Config.Current.Information }));
                menu.Items.Add(CreateCheckMenuItem(TextResources.GetString("InformationGroup.Description"), new Binding(nameof(InformationConfig.IsVisibleDescription)) { Source = Config.Current.Information }));
                menu.Items.Add(CreateCheckMenuItem(TextResources.GetString("InformationGroup.Origin"), new Binding(nameof(InformationConfig.IsVisibleOrigin)) { Source = Config.Current.Information }));
                menu.Items.Add(CreateCheckMenuItem(TextResources.GetString("InformationGroup.Camera"), new Binding(nameof(InformationConfig.IsVisibleCamera)) { Source = Config.Current.Information }));
                menu.Items.Add(CreateCheckMenuItem(TextResources.GetString("InformationGroup.AdvancedPhoto"), new Binding(nameof(InformationConfig.IsVisibleAdvancedPhoto)) { Source = Config.Current.Information }));
                menu.Items.Add(CreateCheckMenuItem(TextResources.GetString("InformationGroup.Gps"), new Binding(nameof(InformationConfig.IsVisibleGps)) { Source = Config.Current.Information }));
                menu.Items.Add(CreateCheckMenuItem(TextResources.GetString("InformationGroup.Extras"), new Binding(nameof(InformationConfig.IsVisibleExtras)) { Source = Config.Current.Information }));
                return menu;
            }
        }

        #endregion MoreMenu


        private void Model_FileInformationCollectionChanged(object? sender, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(nameof(FileInformationCollection));

            if (SelectedItem is null)
            {
                SelectedItem = _model.GetMainFileInformation();
            }
        }

        public bool IsLRKeyEnabled()
        {
            return Config.Current.Panels.IsLeftRightKeyEnabled;
        }

        public void MoveSelectedItem(int delta)
        {
            if (FileInformationCollection is null) return;

            var index = SelectedItem is null ? 0 : FileInformationCollection.IndexOf(SelectedItem);
            index = MathUtility.Clamp(index + delta, 0, FileInformationCollection.Count - 1);
            if (index >= 0)
            {
                SelectedItem = FileInformationCollection[index];
            }
        }


        #region Export Command

        public RelayCommand ExportImageCommand
        {
            get { return _exportImageCommand ?? (_exportImageCommand = new RelayCommand(ExportImageExecute)); }
        }

        private async void ExportImageExecute()
        {
            if (SelectedItem is null) return;

            var page = SelectedItem.Page;
            if (page?.Content is null) return;

            // Get the image source
            var imageSource = (SelectedItem.ViewContent.ViewSource as IHasImageSource)?.ImageSource as BitmapSource;
            if (imageSource is null) return;

            // Show save file dialog
            var dialog = new SaveFileDialog
            {
                Filter = "PNG files (*.png)|*.png|JPEG files (*.jpg)|*.jpg|All files (*.*)|*.*",
                FileName = Path.GetFileNameWithoutExtension(page.EntryName) + ".png"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await Task.Run(() =>
                    {
                        using var fileStream = new FileStream(dialog.FileName, FileMode.Create);
                        
                        BitmapEncoder encoder;
                        var extension = Path.GetExtension(dialog.FileName).ToLowerInvariant();
                        
                        if (extension == ".jpg" || extension == ".jpeg")
                        {
                            encoder = new JpegBitmapEncoder
                            {
                                QualityLevel = 80  // Default quality
                            };
                        }
                        else
                        {
                            encoder = new PngBitmapEncoder();
                        }

                        encoder.Frames.Add(BitmapFrame.Create(imageSource));
                        encoder.Save(fileStream);
                    });

                    InfoMessage.Current.SetMessage(InfoMessageType.Notify, 
                        string.Format(Properties.TextResources.GetString("ExportImage.Message.Success"), 
                        Path.GetFileName(dialog.FileName)));
                }
                catch (Exception ex)
                {
                    new MessageDialog(ex.Message, Properties.TextResources.GetString("Word.Error")).ShowDialog();
                }
            }
        }

        #endregion Export Command
    }
}
