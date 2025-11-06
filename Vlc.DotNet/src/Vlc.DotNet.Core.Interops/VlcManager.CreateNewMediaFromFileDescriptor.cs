using System;
using Vlc.DotNet.Core.Interops.Signatures;

namespace Vlc.DotNet.Core.Interops
{
    public sealed partial class VlcManager
    {
        public VlcMediaInstance CreateNewMediaFromFileDescriptor(int fileDescriptor)
        {
            if (disposedValue) throw new ObjectDisposedException(GetType().FullName);

            return VlcMediaInstance.New(this, myLibraryLoader.GetInteropDelegate<CreateNewMediaFromFileDescriptor>().Invoke(myVlcInstance, fileDescriptor));
        }
    }
}
