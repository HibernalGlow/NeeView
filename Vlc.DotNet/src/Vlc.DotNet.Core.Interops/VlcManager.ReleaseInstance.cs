using System;
using Vlc.DotNet.Core.Interops.Signatures;

namespace Vlc.DotNet.Core.Interops
{
    public sealed partial class VlcManager
    {
        public void ReleaseInstance(VlcInstance instance)
        {
            //if (disposedValue) throw new ObjectDisposedException(GetType().FullName);

            if (instance == IntPtr.Zero)
                return;
            myLibraryLoader.GetInteropDelegate<ReleaseInstance>().Invoke(instance);
        }
    }
}
