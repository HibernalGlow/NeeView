using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using Vlc.DotNet.Wpf;

namespace Samples.Wpf.Image.MultiplePlayers
{
    public partial class MainWindow : Window
    {
        private VlcPlayerControl controlA;
        private VlcPlayerControl controlB;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.controlA = new VlcPlayerControl();
            this.controlA.Play(this.Dispatcher, this.VideoA);

            await Task.Delay(2000);

            this.controlB = new VlcPlayerControl();
            this.controlB.Play(this.Dispatcher, this.VideoB);
        }

        private void MuteVideoA_Click(object sender, RoutedEventArgs e)
        {
            this.controlA.IsMute = this.MuteVideoA.IsChecked == true;
        }

        private void MuteVideoB_Click(object sender, RoutedEventArgs e)
        {
            this.controlB.IsMute = this.MuteVideoB.IsChecked == true;
        }
    }

    public class VlcPlayerControl
    {
        private VlcVideoSourceProvider sourceProvider;

        public bool IsMute
        {
            get { return this.sourceProvider.MediaPlayer.Audio.IsMute; }
            set { this.sourceProvider.MediaPlayer.Audio.IsMute = value; }
        }

        public void Play(Dispatcher dispatcher, System.Windows.Controls.Image videoCanvas)
        {
            var currentAssembly = Assembly.GetEntryAssembly();
            var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;
            // Default installation path of VideoLAN.LibVLC.Windows
            var libDirectory = new DirectoryInfo(Path.Combine(currentDirectory, "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));

            this.sourceProvider = new VlcVideoSourceProvider(dispatcher);

            // In libvlc 3.0.20, there is a problem in multiplayer where the volume setting is applied to all players.
            // The workaround is to set "--aout=directsound".
            // Not sure what will happen in libvlc 4.
            // https://code.videolan.org/videolan/vlc/-/issues/28194
            var options = new string[] { "--aout=directsound" };
            this.sourceProvider.CreatePlayer(libDirectory, options);
            this.sourceProvider.MediaPlayer.Play(new Uri("http://download.blender.org/peach/bigbuckbunny_movies/big_buck_bunny_480p_surround-fix.avi" ));

            videoCanvas.SetBinding(System.Windows.Controls.Image.SourceProperty,
                new Binding(nameof(VlcVideoSourceProvider.VideoSource)) { Source = sourceProvider });
        }

    }
}
