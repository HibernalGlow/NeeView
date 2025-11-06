using NeeLaboratory.ComponentModel;
using System;
using System.Windows;
using System.Windows.Media;

namespace NeeView.SuperResolution
{
    /// <summary>
    /// 超分辨率面板
    /// </summary>
    public class SuperResolutionPanel : BindableBase, IPanel
    {
        private readonly Lazy<FrameworkElement> _view;
        private readonly SuperResolutionConfig _config;

        public SuperResolutionPanel(SuperResolutionConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _view = new Lazy<FrameworkElement>(() => new SuperResolutionView(_config));

            // 使用星形图标作为超分辨率面板图标
            var geometry = App.Current.MainWindow?.Resources["g_star_24px"] as PathGeometry;
            if (geometry != null)
            {
                Icon = new DrawingImage(new GeometryDrawing(
                    Brushes.Transparent,
                    new Pen(new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80)), 1),
                    geometry));
            }
            else
            {
                // 如果找不到资源,使用默认图标
                Icon = new DrawingImage(new GeometryDrawing(
                    Brushes.LightGray,
                    new Pen(Brushes.Gray, 1),
                    Geometry.Parse("M12,2L9,8.5L2,9.5L7,14L6,21L12,17.5L18,21L17,14L22,9.5L15,8.5L12,2Z")));
            }
        }

#pragma warning disable CS0067
        public event EventHandler? IsVisibleLockChanged;
#pragma warning restore CS0067

        public string TypeCode => nameof(SuperResolutionPanel);

        public ImageSource Icon { get; private set; }

        public string IconTips => "Super Resolution";

        public Lazy<FrameworkElement> View => _view;

        public bool IsVisibleLock => false;

        public PanelPlace DefaultPlace => PanelPlace.Right;

        public void Refresh()
        {
            // nop.
        }

        public void Focus()
        {
            if (_view.IsValueCreated)
            {
                _view.Value.Focus();
            }
        }
    }
}
