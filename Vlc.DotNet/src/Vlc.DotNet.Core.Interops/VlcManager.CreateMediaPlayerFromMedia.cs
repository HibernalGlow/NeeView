using System;
using Vlc.DotNet.Core.Interops.Signatures;

namespace Vlc.DotNet.Core.Interops
{
    public sealed partial class VlcManager
    {
        public VlcMediaPlayerInstance CreateMediaPlayerFromMedia(VlcMediaInstance mediaInstance)
        {
            if (disposedValue) throw new ObjectDisposedException(GetType().FullName);

            if (mediaInstance == IntPtr.Zero)
                throw new ArgumentException("Media instance is not initialized.");
            return new VlcMediaPlayerInstance(this, myLibraryLoader.GetInteropDelegate<CreateMediaPlayerFromMedia>().Invoke(mediaInstance));
        }
    }
}
