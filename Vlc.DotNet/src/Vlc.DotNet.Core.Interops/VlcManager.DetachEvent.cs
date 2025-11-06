using System;
using Vlc.DotNet.Core.Interops.Signatures;

namespace Vlc.DotNet.Core.Interops
{
    public sealed partial class VlcManager
    {
        public void DetachEvent(VlcEventManagerInstance eventManagerInstance, EventTypes eventType, EventCallback callback)
        {
            if (disposedValue) throw new ObjectDisposedException(GetType().FullName);

            if (eventManagerInstance == IntPtr.Zero)
                throw new ArgumentException("Event manager is not initialized.");
            if (callback == null)
                return;
            myLibraryLoader.GetInteropDelegate<DetachEvent>().Invoke(eventManagerInstance, eventType, callback, IntPtr.Zero);
        }
    }
}
