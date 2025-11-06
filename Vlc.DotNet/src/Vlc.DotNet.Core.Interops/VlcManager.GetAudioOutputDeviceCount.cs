using System;
using Vlc.DotNet.Core.Interops.Signatures;

namespace Vlc.DotNet.Core.Interops
{
    public sealed partial class VlcManager
    {
        [Obsolete("Use GetAudioOutputDeviceList instead")]
        public int GetAudioOutputDeviceCount(string outputName)
        {
            if (disposedValue) throw new ObjectDisposedException(GetType().FullName);

            using (var outputNameHandle = Utf8InteropStringConverter.ToUtf8StringHandle(outputName))
            {
                return myLibraryLoader.GetInteropDelegate<GetAudioOutputDeviceCount>().Invoke(myVlcInstance, outputNameHandle);
            }
        }
    }
}
