using System;
using Vlc.DotNet.Core.Interops.Signatures;

namespace Vlc.DotNet.Core.Interops
{
    public sealed partial class VlcManager
    {
        public void SetMediaMeta(VlcMediaInstance mediaInstance, MediaMetadatas metadata, string value)
        {
            if (disposedValue) throw new ObjectDisposedException(GetType().FullName);

            if (mediaInstance == IntPtr.Zero)
                throw new ArgumentException("Media instance is not initialized.");
            using (var handle = Utf8InteropStringConverter.ToUtf8StringHandle(value))
            {
                myLibraryLoader.GetInteropDelegate<SetMediaMetadata>().Invoke(mediaInstance, metadata, handle);
            }
        }
    }
}
