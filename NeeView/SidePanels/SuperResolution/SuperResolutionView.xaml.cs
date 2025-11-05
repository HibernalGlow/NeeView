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
    }
}
