using System;
using Vlc.DotNet.Core.Interops.Signatures;

namespace Vlc.DotNet.Core.Interops
{
    public sealed partial class VlcManager
    {
        public VlcMediaInstance CreateNewMediaFromPath(string mrl)
        {
            if (disposedValue) throw new ObjectDisposedException(this.GetType().FullName);

            using (var handle = Utf8InteropStringConverter.ToUtf8StringHandle(mrl))
            {
                return VlcMediaInstance.New(this, myLibraryLoader.GetInteropDelegate<CreateNewMediaFromPath>().Invoke(myVlcInstance, handle));
            }
        }
    }
}
