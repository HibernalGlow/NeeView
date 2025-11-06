using System;
using Vlc.DotNet.Core.Interops.Signatures;

namespace Vlc.DotNet.Core.Interops
{
    public sealed partial class VlcManager
    {
        public void ReleaseMedia(VlcMediaInstance mediaInstance)
        {
            //if (disposedValue) throw new ObjectDisposedException(GetType().FullName);

            if (mediaInstance == IntPtr.Zero)
                throw new ArgumentException("Media instance is not initialized.");
            myLibraryLoader.GetInteropDelegate<ReleaseMedia>().Invoke(mediaInstance);
        }
    }
}
