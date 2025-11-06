using System;
using Vlc.DotNet.Core.Interops.Signatures;

namespace Vlc.DotNet.Core.Interops
{
    public sealed partial class VlcManager
    {
        public void ReleaseTrackDescription(IntPtr trackDescription)
        {
            //if (disposedValue) throw new ObjectDisposedException(GetType().FullName);

            if (trackDescription == IntPtr.Zero) return;

            myLibraryLoader.GetInteropDelegate<ReleaseTrackDescription>().Invoke(trackDescription);
        }
    }
}
