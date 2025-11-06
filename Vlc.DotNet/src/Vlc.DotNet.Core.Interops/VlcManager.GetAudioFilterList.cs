using System;
using Vlc.DotNet.Core.Interops.Signatures;

namespace Vlc.DotNet.Core.Interops
{
    public sealed partial class VlcManager
    {
        public IntPtr GetAudioFilterList()
        {
            if (disposedValue) throw new ObjectDisposedException(GetType().FullName);

            return myLibraryLoader.GetInteropDelegate<GetAudioFilterList>().Invoke(myVlcInstance);
        }
    }
}
