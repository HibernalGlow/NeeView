using System;
using Vlc.DotNet.Core.Interops.Signatures;

namespace Vlc.DotNet.Core.Interops
{
    public sealed partial class VlcManager
    {
        public void SetFullScreen(VlcMediaPlayerInstance mediaPlayerInstance, bool fullScreen)
        {
            if (disposedValue) throw new ObjectDisposedException(GetType().FullName);

            if (mediaPlayerInstance == IntPtr.Zero)
                throw new ArgumentException("Media player instance is not initialized.");
            myLibraryLoader.GetInteropDelegate<SetFullScreen>().Invoke(mediaPlayerInstance, fullScreen ? 1 : 0);
        }
    }
}