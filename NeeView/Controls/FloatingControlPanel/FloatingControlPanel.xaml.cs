using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NeeView
{
    /// <summary>
    /// FloatingControlPanel.xaml 的交互逻辑
    /// </summary>
    public partial class FloatingControlPanel : UserControl
    {
        private FloatingControlPanelViewModel? _vm;

        public FloatingControlPanel()
        {
            InitializeComponent();
            
            this.Loaded += FloatingControlPanel_Loaded;
            this.Unloaded += FloatingControlPanel_Unloaded;
        }

        private void FloatingControlPanel_Loaded(object sender, RoutedEventArgs e)
        {
            if (_vm == null)
            {
                _vm = new FloatingControlPanelViewModel();
                this.DataContext = _vm;
            }
        }

        private void FloatingControlPanel_Unloaded(object sender, RoutedEventArgs e)
        {
            _vm?.Dispose();
            _vm = null;
        }

        private void CollapsedButton_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _vm?.ExpandCommand.Execute(null);
            e.Handled = true;
        }
    }
}
