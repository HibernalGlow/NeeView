using System;
using Vlc.DotNet.Core.Interops.Signatures;

namespace Vlc.DotNet.Core.Interops
{
    public sealed partial class VlcManager
    {
        public void SetAudioTrack(VlcMediaPlayerInstance mediaPlayerInstance, TrackDescriptionStructure trackDescription)
        {
            if (disposedValue) throw new ObjectDisposedException(GetType().FullName);

            if (mediaPlayerInstance == IntPtr.Zero)
                throw new ArgumentException("Media player instance is not initialized.");
            SetAudioTrack(mediaPlayerInstance, trackDescription.Id);
        }

        public void SetAudioTrack(VlcMediaPlayerInstance mediaPlayerInstance, int id)
        {
            if (disposedValue) throw new ObjectDisposedException(GetType().FullName);

            if (mediaPlayerInstance == IntPtr.Zero)
                throw new ArgumentException("Media player instance is not initialized.");
            myLibraryLoader.GetInteropDelegate<SetAudioTrack>().Invoke(mediaPlayerInstance, id);
        }
    }
}
