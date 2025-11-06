using System;
using Vlc.DotNet.Core.Interops.Signatures;

namespace Vlc.DotNet.Core.Interops
{
    public sealed partial class VlcManager
    {
        public void SetAudioOutputDevice(VlcMediaPlayerInstance mediaPlayerInstance, string audioOutputDescriptionName, string deviceName)
        {
            if (disposedValue) throw new ObjectDisposedException(GetType().FullName);

            using (var audioOutputInterop = Utf8InteropStringConverter.ToUtf8StringHandle(audioOutputDescriptionName))
            using (var deviceNameInterop = Utf8InteropStringConverter.ToUtf8StringHandle(audioOutputDescriptionName))
            {
                myLibraryLoader.GetInteropDelegate<SetAudioOutputDevice>().Invoke(mediaPlayerInstance, audioOutputInterop, deviceNameInterop);
            }
        }
    }
}
