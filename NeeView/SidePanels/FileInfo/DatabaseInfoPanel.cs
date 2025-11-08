using NeeLaboratory.ComponentModel;
using System;
using System.Windows;
using System.Windows.Media;

namespace NeeView
{
    /// <summary>
    /// 数据库信息面板
    /// </summary>
    public class DatabaseInfoPanel : BindableBase, IPanel
    {
        private readonly Lazy<FrameworkElement> _view;
        private readonly InformationConfig _config;

        public DatabaseInfoPanel(InformationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _view = new Lazy<FrameworkElement>(() => new DatabaseInfoView());

            // 使用数据库图标
            var geometry = App.Current.MainWindow?.Resources["g_database_24px"] as PathGeometry;
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
                    Geometry.Parse("M12,3C7.58,3 4,4.79 4,7C4,9.21 7.58,11 12,11C16.42,11 20,9.21 20,7C20,4.79 16.42,3 12,3M4,9V12C4,14.21 7.58,16 12,16C16.42,16 20,14.21 20,12V9C20,11.21 16.42,13 12,13C7.58,13 4,11.21 4,9M4,14V17C4,19.21 7.58,21 12,21C16.42,21 20,19.21 20,17V14C20,16.21 16.42,18 12,18C7.58,18 4,16.21 4,14Z")));
            }
        }

#pragma warning disable CS0067
        public event EventHandler? IsVisibleLockChanged;
#pragma warning restore CS0067

        public string TypeCode => nameof(DatabaseInfoPanel);

        public ImageSource Icon { get; private set; }

        public string IconTips => "Database Info";

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
