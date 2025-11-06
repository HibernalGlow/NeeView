using System;
using System.Runtime.InteropServices;
using Vlc.DotNet.Core.Interops.Signatures;

namespace Vlc.DotNet.Core
{
    public partial class VlcMedia
    {
        private EventCallback myOnMediaFreedInternalEventCallback;
        public event EventHandler<VlcMediaFreedEventArgs> Freed;

        private void OnMediaFreedInternal(IntPtr ptr)
        {
            OnMediaFreed();
        }

        public void OnMediaFreed()
        {
            if (disposedValue) return;

            Freed?.Invoke(this, new VlcMediaFreedEventArgs());
        }
    }
}