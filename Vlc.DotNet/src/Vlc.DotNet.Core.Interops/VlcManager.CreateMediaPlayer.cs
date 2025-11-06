using System;
using Vlc.DotNet.Core.Interops.Signatures;

namespace Vlc.DotNet.Core.Interops
{
    public sealed partial class VlcManager
    {
        public VlcMediaPlayerInstance CreateMediaPlayer()
        {
            if (disposedValue) throw new ObjectDisposedException(GetType().FullName);

            return new VlcMediaPlayerInstance(this, myLibraryLoader.GetInteropDelegate<CreateMediaPlayer>().Invoke(myVlcInstance));
        }
    }
}
