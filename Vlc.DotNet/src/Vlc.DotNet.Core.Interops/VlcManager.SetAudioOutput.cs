using System;
using Vlc.DotNet.Core.Interops.Signatures;

namespace Vlc.DotNet.Core.Interops
{
    public sealed partial class VlcManager
    {
        public int SetAudioOutput(VlcMediaPlayerInstance mediaPlayerInstance, AudioOutputDescriptionStructure output)
        {
            if (disposedValue) throw new ObjectDisposedException(GetType().FullName);

            return SetAudioOutput(mediaPlayerInstance, output.Name);
        }

        public int SetAudioOutput(VlcMediaPlayerInstance mediaPlayerInstance, string outputName)
        {
            if (disposedValue) throw new ObjectDisposedException(GetType().FullName);

            using (var outputInterop = Utf8InteropStringConverter.ToUtf8StringHandle(outputName))
            {
                return myLibraryLoader.GetInteropDelegate<SetAudioOutput>().Invoke(mediaPlayerInstance, outputInterop);
            }
        }
    }
}
