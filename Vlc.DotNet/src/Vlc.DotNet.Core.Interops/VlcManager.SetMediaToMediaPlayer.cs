using System;
using Vlc.DotNet.Core.Interops.Signatures;

namespace Vlc.DotNet.Core.Interops
{
    public sealed partial class VlcManager
    {
        public void SetMediaToMediaPlayer(VlcMediaPlayerInstance mediaPlayerInstance, VlcMediaInstance mediaInstance)
        {
            if (disposedValue) throw new ObjectDisposedException(GetType().FullName);

            if (mediaPlayerInstance == IntPtr.Zero)
                throw new ArgumentException("Media player instance is not initialized.");
            myLibraryLoader.GetInteropDelegate<SetMediaToMediaPlayer>().Invoke(mediaPlayerInstance, mediaInstance?.Pointer ?? IntPtr.Zero);
        }
    }
}
