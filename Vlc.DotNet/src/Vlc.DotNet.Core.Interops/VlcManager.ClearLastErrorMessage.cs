using System;
using Vlc.DotNet.Core.Interops.Signatures;

namespace Vlc.DotNet.Core.Interops
{
    public sealed partial class VlcManager
    {
        public void ClearLastErrorMessage()
        {
            if (disposedValue) throw new ObjectDisposedException(GetType().FullName);

            myLibraryLoader.GetInteropDelegate<ClearLastErrorMessage>().Invoke();
        }
    }
}
