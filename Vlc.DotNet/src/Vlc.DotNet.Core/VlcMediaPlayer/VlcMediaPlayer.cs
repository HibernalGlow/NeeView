using System;
using System.Collections.Generic;
using System.IO;
using Vlc.DotNet.Core.Interops;
using Vlc.DotNet.Core.Interops.Signatures;

namespace Vlc.DotNet.Core
{
    public sealed partial class VlcMediaPlayer : IDisposable
    {
        private bool disposedValue;

        private readonly VlcMediaPlayerInstance myMediaPlayerInstance;
        private VlcMedia myCurrentMedia;

        public VlcMediaPlayer(DirectoryInfo vlcLibDirectory)
            : this(new VlcManager(vlcLibDirectory, new string[0]))
        {
        }

        public VlcMediaPlayer(DirectoryInfo vlcLibDirectory, string[] options)
            : this(new VlcManager(vlcLibDirectory, options))
        {
        }

        public VlcMediaPlayer(VlcManager manager)
        {
            Manager = manager;
            myMediaPlayerInstance = manager.CreateMediaPlayer();
            RegisterEvents();
            Chapters = new ChapterManagement(manager, myMediaPlayerInstance);
            SubTitles = new SubTitlesManagement(manager, myMediaPlayerInstance);
            Video = new VideoManagement(manager, myMediaPlayerInstance);
            Audio = new AudioManagement(manager, myMediaPlayerInstance);
#if !NET35 && !NET40
            Dialogs = new DialogsManagement(manager, myMediaPlayerInstance);
#endif
        }

        /// <summary>
        /// WARNING : USE AT YOUR OWN RISK!
        /// Gets the low-level interop manager that calls the methods on the libvlc library.
        /// This is useful if a higher-level API is missing.
        /// </summary>
        public VlcManager Manager { get; }

        /// <summary>
        /// Sets some meta-information about the application. 
        /// </summary>
        /// <seealso cref="SetUserAgent" />
        /// <param name="id">Java-style application identifier, e.g. "com.acme.foobar"</param>
        /// <param name="version">application version numbers, e.g. "1.2.3"</param>
        /// <param name="icon">application icon name, e.g. "foobar"</param>
        public void SetAppId(string id, string version, string icon)
        {
            if (disposedValue) return;

            this.Manager.SetAppId(id, version, icon);
        }

        /// <summary>
        /// Sets the application name.
        /// LibVLC passes this as the user agent string when a protocol requires it.
        /// </summary>
        /// <param name="name">human-readable application name, e.g. "FooBar player 1.2.3"</param>
        /// <param name="http">HTTP User Agent, e.g. "FooBar/1.2.3 Python/2.6.0"</param>
        public void SetUserAgent(string name, string http)
        {
            if (disposedValue) return;

            this.Manager.SetUserAgent(name, http);
        }

        public IntPtr VideoHostControlHandle
        {
            get
            {
                if (disposedValue) return IntPtr.Zero;
                return Manager.GetMediaPlayerVideoHostHandle(myMediaPlayerInstance);
            }
            set
            {
                if (disposedValue) return;
                Manager.SetMediaPlayerVideoHostHandle(myMediaPlayerInstance, value);
            }
        }

        public int SetAudioOutput(string outputName)
        {
            if (disposedValue) return default;

            return this.Manager.SetAudioOutput(myMediaPlayerInstance, outputName);
        }

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                if (myMediaPlayerInstance == IntPtr.Zero)
                    return;
                UnregisterEvents();
                if (IsPlaying())
                    Stop();

                myCurrentMedia?.Dispose();
                myMediaPlayerInstance.Dispose();
                Manager.Dispose();

                disposedValue = true;
            }
        }

        ~VlcMediaPlayer()
        {
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public VlcMedia SetMedia(FileInfo file, params string[] options)
        {
            if (disposedValue) return null;

            return SetMedia(new VlcMedia(this, file, options));
        }

        public VlcMedia SetMedia(Uri uri, params string[] options)
        {
            if (disposedValue) return null;

            return SetMedia(new VlcMedia(this, uri, options));
        }

        public VlcMedia SetMedia(string mrl, params string[] options)
        {
            if (disposedValue) return null;

            return SetMedia(new VlcMedia(this, mrl, options));
        }

        public VlcMedia SetMedia(Stream stream, params string[] options)
        {
            if (disposedValue) return null;

            return SetMedia(new VlcMedia(this, stream, options));
        }

        public void ResetMedia()
        {
            if (disposedValue) return;

            SetMedia((VlcMedia)null);
        }

        private VlcMedia SetMedia(VlcMedia media)
        {
            // If there is a previous media, dispose it.
            myCurrentMedia?.Dispose();

            // Set it to the media player.
            Manager.SetMediaToMediaPlayer(myMediaPlayerInstance, media?.MediaInstance);

            // Register Events.
            media?.Initialize();
            myCurrentMedia = media;

            return media;
        }

        public VlcMedia GetMedia()
        {
            return myCurrentMedia;
        }

        public void Play()
        {
            if (disposedValue) return;

            Manager.Play(myMediaPlayerInstance);
        }

        /// <summary>
        /// Overload, provided for convenience that calls <see cref="SetMedia(System.IO.FileInfo,string[])"/> before <see cref="Play()"/>
        /// </summary>
        /// <param name="file">The file to play</param>
        /// <param name="options">The options to be given</param>
        public void Play(FileInfo file, params string[] options)
        {
            if (disposedValue) return;

            this.SetMedia(file, options);
            this.Play();
        }

        /// <summary>
        /// Overload, provided for convenience that calls <see cref="SetMedia(System.Uri,string[])"/> before <see cref="Play()"/>
        /// </summary>
        /// <param name="uri">The uri to play</param>
        /// <param name="options">The options to be given</param>
        public void Play(Uri uri, params string[] options)
        {
            if (disposedValue) return;

            this.SetMedia(uri, options);
            this.Play();
        }

        /// <summary>
        /// Overload, provided for convenience that calls <see cref="SetMedia(string,string[])"/> before <see cref="Play()"/>
        /// </summary>
        /// <param name="mrl">The mrl to play</param>
        /// <param name="options">The options to be given</param>
        public void Play(string mrl, params string[] options)
        {
            if (disposedValue) return;

            this.SetMedia(mrl, options);
            this.Play();
        }

        /// <summary>
        /// Overload, provided for convenience that calls <see cref="SetMedia(System.IO.Stream,string[])"/> before <see cref="Play()"/>
        /// </summary>
        /// <param name="stream">The stream to play</param>
        /// <param name="options">The options to be given</param>
        public void Play(Stream stream, params string[] options)
        {
            if (disposedValue) return;

            this.SetMedia(stream, options);
            this.Play();
        }

        /// <summary>
        /// Toggle pause (no effect if there is no media) 
        /// </summary>
        public void Pause()
        {
            if (disposedValue) return;

            Manager.Pause(myMediaPlayerInstance);
        }

        /// <summary>
        /// Pause or resume (no effect if there is no media) 
        /// </summary>
        /// <param name="doPause">If set to <c>true</c>, pauses the media, resumes if <c>false</c></param>
        public void SetPause(bool doPause)
        {
            if (disposedValue) return;

            Manager.SetPause(myMediaPlayerInstance, doPause);
        }

        public void Stop()
        {
            if (disposedValue) return;

            Manager.Stop(myMediaPlayerInstance);
        }

        public bool IsPlaying()
        {
            if (disposedValue) return false;

            return Manager.IsPlaying(myMediaPlayerInstance);
        }

        public bool IsPausable()
        {
            if (disposedValue) return false;

            return Manager.IsPausable(myMediaPlayerInstance);
        }

        public void NextFrame()
        {
            if (disposedValue) return;

            Manager.NextFrame(myMediaPlayerInstance);
        }

        public IEnumerable<FilterModuleDescription> GetAudioFilters()
        {
            if (disposedValue) return Array.Empty< FilterModuleDescription>();

            var module = Manager.GetAudioFilterList();
            if (module == IntPtr.Zero)
                return Array.Empty< FilterModuleDescription>();
            ModuleDescriptionStructure nextModule = MarshalHelper.PtrToStructure<ModuleDescriptionStructure>(module);
            var result = GetSubFilter(nextModule);
            if (module != IntPtr.Zero)
                Manager.ReleaseModuleDescriptionInstance(module);
            return result;
        }

        private List<FilterModuleDescription> GetSubFilter(ModuleDescriptionStructure module)
        {
            if (disposedValue) return new List<FilterModuleDescription>();

            var result = new List<FilterModuleDescription>();
            var filterModule = FilterModuleDescription.GetFilterModuleDescription(module);
            if (filterModule == null)
            {
                return result;
            }
            result.Add(filterModule);
            if (module.NextModule != IntPtr.Zero)
            {
                ModuleDescriptionStructure nextModule = MarshalHelper.PtrToStructure<ModuleDescriptionStructure>(module.NextModule);
                var data = GetSubFilter(nextModule);
                if (data.Count > 0)
                    result.AddRange(data);
            }
            return result;
        }

        public IEnumerable<FilterModuleDescription> GetVideoFilters()
        {
            if (disposedValue) return Array.Empty<FilterModuleDescription>();

            var module = Manager.GetVideoFilterList();
            if (module == IntPtr.Zero)
                return Array.Empty<FilterModuleDescription>();
            ModuleDescriptionStructure nextModule = MarshalHelper.PtrToStructure<ModuleDescriptionStructure>(module);
            var result = GetSubFilter(nextModule);
            if (module != IntPtr.Zero)
                Manager.ReleaseModuleDescriptionInstance(module);
            return result;
        }

        public float Position
        {
            get
            {
                if (disposedValue) return default;
                return Manager.GetMediaPosition(myMediaPlayerInstance);
            }
            set
            {
                if (disposedValue) return;
                Manager.SetMediaPosition(myMediaPlayerInstance, value);
            }
        }

        public bool CouldPlay
        {
            get
            { 
                if (disposedValue) return false;
                return Manager.CouldPlay(myMediaPlayerInstance);
            }
        }

        public IChapterManagement Chapters { get; private set; }

        public float Rate
        {
            get
            {
                if (disposedValue) return default;
                return Manager.GetRate(myMediaPlayerInstance);
            }
            set
            {
                if (disposedValue) return;
                Manager.SetRate(myMediaPlayerInstance, value);
            }
        }

        public MediaStates State
        {
            get
            {
                if (disposedValue) return default;
                return Manager.GetMediaPlayerState(myMediaPlayerInstance);
            }
        }

        public float FramesPerSecond
        {
            get
            {
                if (disposedValue) return default;
                return Manager.GetFramesPerSecond(myMediaPlayerInstance);
            }
        }

        public bool IsSeekable
        {
            get
            {
                if (disposedValue) return false;
                return Manager.IsSeekable(myMediaPlayerInstance);
            }
        }

        public void Navigate(NavigateModes navigateMode)
        {
            if (disposedValue) return;
            
            Manager.Navigate(myMediaPlayerInstance, navigateMode);
        }

        public ISubTitlesManagement SubTitles { get; }

        public IVideoManagement Video { get; }

        public IAudioManagement Audio { get; }

#if !NET35 && !NET40
        public IDialogsManagement Dialogs { get; }
#endif

        public long Length
        {
            get
            {
                if (disposedValue) return default;
                return Manager.GetLength(myMediaPlayerInstance);
            }
        }

        public long Time
        {
            get
            {
                if (disposedValue) return default;
                return Manager.GetTime(myMediaPlayerInstance);
            }
            set
            {
                if (disposedValue) return;
                Manager.SetTime(myMediaPlayerInstance, value);
            }
        }

        public int Spu
        {
            get
            {
                if (disposedValue) return default;
                return Manager.GetVideoSpu(myMediaPlayerInstance);
            }
            set
            {
                if (disposedValue) return;
                Manager.SetVideoSpu(myMediaPlayerInstance, value);
            }
        }

        public bool TakeSnapshot(FileInfo file)
        {
            if (disposedValue) return false;

            return TakeSnapshot(file, 0, 0);
        }

        public bool TakeSnapshot(FileInfo file, uint width, uint height)
        {
            if (disposedValue) return false;

            return TakeSnapshot(0, file.FullName, width, height);
        }

        public void SetVideoTitleDisplay(Position position, int timeout)
        {
            if (disposedValue) return;

            Manager.SetVideoTitleDisplay(myMediaPlayerInstance, position, timeout);
        }

        /// <summary>
        /// Take a snapshot of the current video window.
        /// </summary>
        /// <param name="outputNumber">The number of video output (typically 0 for the first/only one)</param>
        /// <param name="file">The path of a file or a folder to save the screenshot into</param>
        /// <param name="width">the snapshot's width</param>
        /// <param name="height">the snapshot's height</param>
        /// <returns>A boolean indicating whether the screenshot was sucessfully taken</returns>
        /// <remarks>
        /// If i_width AND i_height is 0, original size is used.
        /// If i_width XOR i_height is 0, original aspect-ratio is preserved.
        /// </remarks>
        public bool TakeSnapshot(uint outputNumber, string file, uint width, uint height)
        {
            if (disposedValue) return false;

            return Manager.TakeSnapshot(myMediaPlayerInstance, outputNumber, file, width, height);
        }

        private void RegisterEvents()
        {
            var vlcEventManager = Manager.GetMediaPlayerEventManager(myMediaPlayerInstance);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerBackward, myOnMediaPlayerBackwardInternalEventCallback = OnMediaPlayerBackwardInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerBuffering, myOnMediaPlayerBufferingInternalEventCallback = OnMediaPlayerBufferingInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerEncounteredError, myOnMediaPlayerEncounteredErrorInternalEventCallback = OnMediaPlayerEncounteredErrorInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerEndReached, myOnMediaPlayerEndReachedInternalEventCallback = OnMediaPlayerEndReachedInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerForward, myOnMediaPlayerForwardInternalEventCallback = OnMediaPlayerForwardInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerLengthChanged, myOnMediaPlayerLengthChangedInternalEventCallback = OnMediaPlayerLengthChangedInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerMediaChanged, myOnMediaPlayerMediaChangedInternalEventCallback = OnMediaPlayerMediaChangedInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerOpening, myOnMediaPlayerOpeningInternalEventCallback = OnMediaPlayerOpeningInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerPausableChanged, myOnMediaPlayerPausableChangedInternalEventCallback = OnMediaPlayerPausableChangedInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerPaused, myOnMediaPlayerPausedInternalEventCallback = OnMediaPlayerPausedInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerPlaying, myOnMediaPlayerPlayingInternalEventCallback = OnMediaPlayerPlayingInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerPositionChanged, myOnMediaPlayerPositionChangedInternalEventCallback = OnMediaPlayerPositionChangedInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerScrambledChanged, myOnMediaPlayerScrambledChangedInternalEventCallback = OnMediaPlayerScrambledChangedInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerSeekableChanged, myOnMediaPlayerSeekableChangedInternalEventCallback = OnMediaPlayerSeekableChangedInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerSnapshotTaken, myOnMediaPlayerSnapshotTakenInternalEventCallback = OnMediaPlayerSnapshotTakenInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerStopped, myOnMediaPlayerStoppedInternalEventCallback = OnMediaPlayerStoppedInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerTimeChanged, myOnMediaPlayerTimeChangedInternalEventCallback = OnMediaPlayerTimeChangedInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerTitleChanged, myOnMediaPlayerTitleChangedInternalEventCallback = OnMediaPlayerTitleChangedInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerVout, myOnMediaPlayerVideoOutChangedInternalEventCallback = OnMediaPlayerVideoOutChangedInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerEsAdded, myOnMediaPlayerEsAddedInternalEventCallback = OnMediaPlayerEsAddedInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerEsDeleted, myOnMediaPlayerEsDeletedInternalEventCallback = OnMediaPlayerEsDeletedInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerEsSelected, myOnMediaPlayerEsSelectedInternalEventCallback = OnMediaPlayerEsSelectedInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerCorked, myOnMediaPlayerCorkedInternalEventCallback = OnMediaPlayerCorkedInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerUncorked, myOnMediaPlayerUncorkedInternalEventCallback = OnMediaPlayerUncorkedInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerMuted, myOnMediaPlayerMutedInternalEventCallback = OnMediaPlayerMutedInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerUnmuted, myOnMediaPlayerUnmutedInternalEventCallback = OnMediaPlayerUnmutedInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerAudioVolume, myOnMediaPlayerAudioVolumeInternalEventCallback = OnMediaPlayerAudioVolumeInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerAudioDevice, myOnMediaPlayerAudioDeviceInternalEventCallback = OnMediaPlayerAudioDeviceInternal);
            Manager.AttachEvent(vlcEventManager, EventTypes.MediaPlayerChapterChanged, myOnMediaPlayerChapterChangedInternalEventCallback = OnMediaPlayerChapterChangedInternal);
            vlcEventManager.Dispose();
        }

        private void UnregisterEvents()
        {
            var vlcEventManager = Manager.GetMediaPlayerEventManager(myMediaPlayerInstance);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerBackward, myOnMediaPlayerBackwardInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerBuffering, myOnMediaPlayerBufferingInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerEncounteredError, myOnMediaPlayerEncounteredErrorInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerEndReached, myOnMediaPlayerEndReachedInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerForward, myOnMediaPlayerForwardInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerLengthChanged, myOnMediaPlayerLengthChangedInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerMediaChanged, myOnMediaPlayerMediaChangedInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerOpening, myOnMediaPlayerOpeningInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerPausableChanged, myOnMediaPlayerPausableChangedInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerPaused, myOnMediaPlayerPausedInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerPlaying, myOnMediaPlayerPlayingInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerPositionChanged, myOnMediaPlayerPositionChangedInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerScrambledChanged, myOnMediaPlayerScrambledChangedInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerSeekableChanged, myOnMediaPlayerSeekableChangedInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerSnapshotTaken, myOnMediaPlayerSnapshotTakenInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerStopped, myOnMediaPlayerStoppedInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerTimeChanged, myOnMediaPlayerTimeChangedInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerTitleChanged, myOnMediaPlayerTitleChangedInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerVout, myOnMediaPlayerVideoOutChangedInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerEsAdded, myOnMediaPlayerEsAddedInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerEsDeleted, myOnMediaPlayerEsDeletedInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerEsSelected, myOnMediaPlayerEsSelectedInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerCorked, myOnMediaPlayerCorkedInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerUncorked, myOnMediaPlayerUncorkedInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerMuted, myOnMediaPlayerMutedInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerUnmuted, myOnMediaPlayerUnmutedInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerAudioVolume, myOnMediaPlayerAudioVolumeInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerAudioDevice, myOnMediaPlayerAudioDeviceInternalEventCallback);
            Manager.DetachEvent(vlcEventManager, EventTypes.MediaPlayerChapterChanged, myOnMediaPlayerChapterChangedInternalEventCallback);
            vlcEventManager.Dispose();
        }
    }
}
