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

            Icon = App.Current.MainWindow?.Resources["pic_ai_24px"] as ImageSource
                ?? throw new InvalidOperationException("Cannot found resource");
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
