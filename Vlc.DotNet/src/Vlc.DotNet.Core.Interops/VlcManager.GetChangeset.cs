using System;
using Vlc.DotNet.Core.Interops.Signatures;

namespace Vlc.DotNet.Core.Interops
{
    public sealed partial class VlcManager
    {
        public string GetChangeset()
        {
            if (disposedValue) throw new ObjectDisposedException(GetType().FullName);

            return Utf8InteropStringConverter.Utf8InteropToString(myLibraryLoader.GetInteropDelegate<GetChangeset>().Invoke());
        }
    }
}
