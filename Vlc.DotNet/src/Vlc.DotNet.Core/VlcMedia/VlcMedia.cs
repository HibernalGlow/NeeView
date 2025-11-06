using System;
using System.IO;
using Vlc.DotNet.Core.Interops;
using Vlc.DotNet.Core.Interops.Signatures;

namespace Vlc.DotNet.Core
{
    public sealed partial class VlcMedia : IDisposable
    {
        private bool disposedValue;

        private readonly VlcMediaPlayer myVlcMediaPlayer;

        internal VlcMedia(VlcMediaPlayer player, FileInfo file, params string[] options)
            : this(player, player.Manager.CreateNewMediaFromPath(file.FullName).AddOptionToMedia(player.Manager, options))
        {
        }

        internal VlcMedia(VlcMediaPlayer player, Uri uri, params string[] options)
            : this(player, player.Manager.CreateNewMediaFromLocation(uri.AbsoluteUri).AddOptionToMedia(player.Manager, options))
        {
        }

        internal VlcMedia(VlcMediaPlayer player, string mrl, params string[] options)
            : this(player, player.Manager.CreateNewMediaFromLocation(mrl).AddOptionToMedia(player.Manager, options))
        {
        }

        internal VlcMedia(VlcMediaPlayer player, Stream stream, params string[] options)
            : this(player, player.Manager.CreateNewMediaFromStream(stream).AddOptionToMedia(player.Manager, options))
        {
        }

        internal VlcMedia(VlcMediaPlayer player, VlcMediaInstance mediaInstance)
        {
            MediaInstance = mediaInstance;
            myVlcMediaPlayer = player;
        }

        internal void Initialize()
        {
            if (disposedValue) return;

            RegisterEvents();
        }

        internal VlcMediaInstance MediaInstance { get; private set; }

        public string Mrl
        {
            get
            {
                if (disposedValue) return null;
                return myVlcMediaPlayer.Manager.GetMediaMrl(MediaInstance);
            }
        }

        public MediaStates State
        {
            get
            {
                if (disposedValue) return MediaStates.NothingSpecial;
                return myVlcMediaPlayer.Manager.GetMediaState(MediaInstance);
            }
        }

        public TimeSpan Duration
        {
            get
            {
                if (disposedValue) return default;
                return TimeSpan.FromMilliseconds(myVlcMediaPlayer.Manager.GetMediaDuration(MediaInstance));
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing && MediaInstance != IntPtr.Zero)
                {
                    UnregisterEvents();
                    MediaInstance.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public VlcMedia Clone()
        {
            if (disposedValue) return null;

            try
            {
                var cloned = myVlcMediaPlayer.Manager.CloneMedia(MediaInstance);
                if (cloned != IntPtr.Zero)
                    return new VlcMedia(myVlcMediaPlayer, cloned);
            }
            catch (ObjectDisposedException)
            {
            }
            return null;
        }

        public MediaStatsStructure Statistics
        {
            get
            {
                if (disposedValue) return default;
                return myVlcMediaPlayer.Manager.GetMediaStats(MediaInstance);
            }
        }

        public MediaTrack[] Tracks
        {
            get
            {
                if (disposedValue) return Array.Empty<MediaTrack>();
                return myVlcMediaPlayer.Manager.GetMediaTracks(MediaInstance);
            }
        }

        [Obsolete("Use Tracks instead")]
        public MediaTrackInfosStructure[] TracksInformations
        {
            get
            {
                if (disposedValue) return Array.Empty<MediaTrackInfosStructure>();
                return myVlcMediaPlayer.Manager.GetMediaTracksInformations(MediaInstance);
            }
        }

        private void RegisterEvents()
        {
            var eventManager = myVlcMediaPlayer.Manager.GetMediaEventManager(MediaInstance);
            //myVlcMediaPlayer.Manager.AttachEvent(eventManager, EventTypes.MediaDurationChanged, myOnMediaDurationChangedInternalEventCallback = OnMediaDurationChangedInternal);
            myVlcMediaPlayer.Manager.AttachEvent(eventManager, EventTypes.MediaFreed, myOnMediaFreedInternalEventCallback = OnMediaFreedInternal);
            myVlcMediaPlayer.Manager.AttachEvent(eventManager, EventTypes.MediaMetaChanged, myOnMediaMetaChangedInternalEventCallback = OnMediaMetaChangedInternal);
            myVlcMediaPlayer.Manager.AttachEvent(eventManager, EventTypes.MediaParsedChanged, myOnMediaParsedChangedInternalEventCallback = OnMediaParsedChangedInternal);
            myVlcMediaPlayer.Manager.AttachEvent(eventManager, EventTypes.MediaStateChanged, myOnMediaStateChangedInternalEventCallback = OnMediaStateChangedInternal);
            myVlcMediaPlayer.Manager.AttachEvent(eventManager, EventTypes.MediaSubItemAdded, myOnMediaSubItemAddedInternalEventCallback = OnMediaSubItemAddedInternal);
            myVlcMediaPlayer.Manager.AttachEvent(eventManager, EventTypes.MediaSubItemTreeAdded, myOnMediaSubItemTreeAddedInternalEventCallback = OnMediaSubItemTreeAddedInternal);
            eventManager.Dispose();
        }

        private void UnregisterEvents()
        {
            var eventManager = myVlcMediaPlayer.Manager.GetMediaEventManager(MediaInstance);
            //myVlcMediaPlayer.Manager.DetachEvent(eventManager, EventTypes.MediaDurationChanged, myOnMediaDurationChangedInternalEventCallback);
            myVlcMediaPlayer.Manager.DetachEvent(eventManager, EventTypes.MediaFreed, myOnMediaFreedInternalEventCallback);
            myVlcMediaPlayer.Manager.DetachEvent(eventManager, EventTypes.MediaMetaChanged, myOnMediaMetaChangedInternalEventCallback);
            myVlcMediaPlayer.Manager.DetachEvent(eventManager, EventTypes.MediaParsedChanged, myOnMediaParsedChangedInternalEventCallback);
            myVlcMediaPlayer.Manager.DetachEvent(eventManager, EventTypes.MediaStateChanged, myOnMediaStateChangedInternalEventCallback);
            myVlcMediaPlayer.Manager.DetachEvent(eventManager, EventTypes.MediaSubItemAdded, myOnMediaSubItemAddedInternalEventCallback);
            myVlcMediaPlayer.Manager.DetachEvent(eventManager, EventTypes.MediaSubItemTreeAdded, myOnMediaSubItemTreeAddedInternalEventCallback);
            eventManager.Dispose();
        }
    }
}