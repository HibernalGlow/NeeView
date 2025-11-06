using System.Windows.Controls;

namespace NeeView.SuperResolution
{
    /// <summary>
    /// SuperResolutionView.xaml 的交互逻辑
    /// </summary>
    public partial class SuperResolutionView : UserControl
    {
        private readonly SuperResolutionViewModel _vm;

        public SuperResolutionView(SuperResolutionConfig config)
        {
            InitializeComponent();
            
            _vm = new SuperResolutionViewModel(config);
            DataContext = _vm;
        }

        public SuperResolutionViewModel ViewModel => _vm;

        private void BrowseModelPath_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择模型文件夹",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "选择文件夹"
            };

            // 使用 FolderBrowserDialog 的变通方法
            var folderBrowser = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "选择 sr_vulkan 模型文件夹",
                ShowNewFolderButton = true
            };

            if (!string.IsNullOrEmpty(_vm.Config.ModelPath))
            {
                folderBrowser.SelectedPath = _vm.Config.ModelPath;
            }

            if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _vm.Config.ModelPath = folderBrowser.SelectedPath;
            }
        }
    }
}
