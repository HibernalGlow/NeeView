using System.Windows;
using System.Windows.Controls;

namespace NeeView
{
    /// <summary>
    /// FloatingControlPanelSidebarButton.xaml 的交互逻辑
    /// </summary>
    public partial class FloatingControlPanelSidebarButton : UserControl
    {
        public FloatingControlPanelSidebarButton()
        {
            InitializeComponent();
        }

        private void Root_Click(object sender, RoutedEventArgs e)
        {
            var vm = FloatingControlPanelViewModel.Current;
            if (vm != null)
            {
                if (vm.IsCollapsed)
                {
                    vm.ExpandCommand.Execute(null);
                }
                else
                {
                    vm.CollapseCommand.Execute(null);
                }
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var vm = FloatingControlPanelViewModel.Current;
            if (vm != null)
            {
                if (vm.IsCollapsed)
                {
                    vm.ExpandCommand.Execute(null);
                }
                vm.ShowSettingsCommand.Execute(null);
            }
        }
    }
}
