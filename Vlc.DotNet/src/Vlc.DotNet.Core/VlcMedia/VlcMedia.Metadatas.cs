using System;
using Vlc.DotNet.Core.Interops.Signatures;

namespace Vlc.DotNet.Core
{
    public sealed partial class VlcMedia : IDisposable
    {
        public string Title
        {
            get { return GetMetaData(MediaMetadatas.Title); }
            set
            {
                if (disposedValue) return;
                myVlcMediaPlayer.Manager.SetMediaMeta(MediaInstance, MediaMetadatas.Title, value);
            }
        }

        public string Artist
        {
            get { return GetMetaData(MediaMetadatas.Artist); }
            set
            {
                if (disposedValue) return;
                myVlcMediaPlayer.Manager.SetMediaMeta(MediaInstance, MediaMetadatas.Artist, value);
            }
        }

        public string Genre
        {
            get { return GetMetaData(MediaMetadatas.Genre); }
            set
            {
                if (disposedValue) return;
                myVlcMediaPlayer.Manager.SetMediaMeta(MediaInstance, MediaMetadatas.Genre, value);
            }
        }

        public string Copyright
        {
            get { return GetMetaData(MediaMetadatas.Copyright); }
            set
            {
                if (disposedValue) return;
                myVlcMediaPlayer.Manager.SetMediaMeta(MediaInstance, MediaMetadatas.Copyright, value);
            }
        }

        public string Album
        {
            get { return GetMetaData(MediaMetadatas.Album); }
            set
            {
                if (disposedValue) return;
                myVlcMediaPlayer.Manager.SetMediaMeta(MediaInstance, MediaMetadatas.Album, value);
            }
        }

        public string TrackNumber
        {
            get { return GetMetaData(MediaMetadatas.TrackNumber); }
            set
            {
                if (disposedValue) return;
                myVlcMediaPlayer.Manager.SetMediaMeta(MediaInstance, MediaMetadatas.TrackNumber, value);
            }
        }

        public string Description
        {
            get { return GetMetaData(MediaMetadatas.Description); }
            set
            {
                if (disposedValue) return;
                myVlcMediaPlayer.Manager.SetMediaMeta(MediaInstance, MediaMetadatas.Description, value);
            }
        }

        public string Rating
        {
            get { return GetMetaData(MediaMetadatas.Rating); }
            set
            {
                if (disposedValue) return;
                myVlcMediaPlayer.Manager.SetMediaMeta(MediaInstance, MediaMetadatas.Rating, value);
            }
        }

        public string Date
        {
            get { return GetMetaData(MediaMetadatas.Date); }
            set
            {
                if (disposedValue) return;
                myVlcMediaPlayer.Manager.SetMediaMeta(MediaInstance, MediaMetadatas.Date, value);
            }
        }

        public string Setting
        {
            get { return GetMetaData(MediaMetadatas.Setting); }
            set
            {
                if (disposedValue) return;
                myVlcMediaPlayer.Manager.SetMediaMeta(MediaInstance, MediaMetadatas.Setting, value);
            }
        }

        public string URL
        {
            get { return GetMetaData(MediaMetadatas.URL); }
            set
            {
                if (disposedValue) return;
                myVlcMediaPlayer.Manager.SetMediaMeta(MediaInstance, MediaMetadatas.URL, value);
            }
        }

        public string Language
        {
            get { return GetMetaData(MediaMetadatas.Language); }
            set
            {
                if (disposedValue) return;
                myVlcMediaPlayer.Manager.SetMediaMeta(MediaInstance, MediaMetadatas.Language, value);
            }
        }

        public string NowPlaying
        {
            get { return GetMetaData(MediaMetadatas.NowPlaying); }
            set
            {
                if (disposedValue) return;
                myVlcMediaPlayer.Manager.SetMediaMeta(MediaInstance, MediaMetadatas.NowPlaying, value);
            }
        }

        public string Publisher
        {
            get { return GetMetaData(MediaMetadatas.Publisher); }
            set
            {
                if (disposedValue) return;
                myVlcMediaPlayer.Manager.SetMediaMeta(MediaInstance, MediaMetadatas.Publisher, value);
            }
        }

        public string EncodedBy
        {
            get { return GetMetaData(MediaMetadatas.EncodedBy); }
            set
            {
                if (disposedValue) return;
                myVlcMediaPlayer.Manager.SetMediaMeta(MediaInstance, MediaMetadatas.EncodedBy, value);
            }
        }

        public string ArtworkURL
        {
            get { return GetMetaData(MediaMetadatas.ArtworkURL); }
            set
            {
                if (disposedValue) return;
                myVlcMediaPlayer.Manager.SetMediaMeta(MediaInstance, MediaMetadatas.ArtworkURL, value);
            }
        }

        public string TrackID
        {
            get { return GetMetaData(MediaMetadatas.TrackID); }
            set
            {
                if (disposedValue) return;
                myVlcMediaPlayer.Manager.SetMediaMeta(MediaInstance, MediaMetadatas.TrackID, value);
            }
        }

        public void Parse()
        {
            if (disposedValue) return;

            myVlcMediaPlayer.Manager.ParseMedia(MediaInstance);
        }

        public void ParseAsync()
        {
            if (disposedValue) return;

            myVlcMediaPlayer.Manager.ParseMediaAsync(MediaInstance);
        }

        private string GetMetaData(MediaMetadatas metadata)
        {
            if (disposedValue)
                return null;
            if (MediaInstance == IntPtr.Zero)
                return null;
            if (myVlcMediaPlayer.Manager.IsParsedMedia(MediaInstance))
                myVlcMediaPlayer.Manager.ParseMedia(MediaInstance);
            return myVlcMediaPlayer.Manager.GetMediaMeta(MediaInstance, metadata);
        }
    }
}