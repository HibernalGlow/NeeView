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
        private bool _isDragging = false;
        private Point _dragStartPoint;

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

        private void ExpandedPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_vm?.IsDraggable == true && e.ClickCount == 1)
            {
                _isDragging = true;
                _dragStartPoint = e.GetPosition(this.Parent as UIElement);
                ExpandedPanel.CaptureMouse();
                e.Handled = true;
            }
        }

        private void ExpandedPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && _vm?.IsDraggable == true)
            {
                var currentPosition = e.GetPosition(this.Parent as UIElement);
                var offset = currentPosition - _dragStartPoint;
                
                _vm.OffsetX += offset.X;
                _vm.OffsetY += offset.Y;
                
                _dragStartPoint = currentPosition;
            }
        }

        private void ExpandedPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                ExpandedPanel.ReleaseMouseCapture();
            }
        }
    }
}
