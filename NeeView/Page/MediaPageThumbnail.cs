using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;
using System;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows;

namespace NeeView
{
    public class MediaPageThumbnail : PageThumbnail
    {
        private readonly MediaPageContent _content;

        public MediaPageThumbnail(MediaPageContent content) : base(content)
        {
            _content = content;
        }

        public override async ValueTask<ThumbnailSource> LoadThumbnailAsync(CancellationToken token)
        {
            NVDebug.AssertMTA();
            token.ThrowIfCancellationRequested();

            // 尝试生成视频缩略图
            byte[]? thumbnailRaw = null;
            try
            {
                var path = _content.ArchiveEntry.SystemPath;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    thumbnailRaw = await GenerateVideoThumbnailAsync(path, token);
                }
            }
            catch
            {
                // 如果生成失败，使用默认媒体图标
            }

            // 如果没有生成缩略图，返回默认媒体图标
            if (thumbnailRaw == null)
            {
                return await Task.FromResult(new ThumbnailSource(ThumbnailType.Media));
            }

            return new ThumbnailSource(thumbnailRaw);
        }

        private async ValueTask<byte[]?> GenerateVideoThumbnailAsync(string path, CancellationToken token)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // 使用 Windows Shell API 提取缩略图
                    IntPtr hBitmap = IntPtr.Zero;
                    
                    try
                    {
                        // 创建 IShellItem
                        SHCreateItemFromParsingName(path, IntPtr.Zero, typeof(IShellItemImageFactory).GUID, out var shellItem);
                        var imageFactory = (IShellItemImageFactory)shellItem;
                        
                        // 获取缩略图大小
                        var size = new SIZE { cx = 256, cy = 256 };
                        
                        // 获取缩略图
                        imageFactory.GetImage(size, SIIGBF.SIIGBF_THUMBNAILONLY, out hBitmap);
                        
                        if (hBitmap != IntPtr.Zero)
                        {
                            // 转换为 BitmapSource
                            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                                hBitmap,
                                IntPtr.Zero,
                                Int32Rect.Empty,
                                BitmapSizeOptions.FromEmptyOptions());
                            
                            bitmapSource.Freeze();
                            
                            // 转换为 JPEG byte[]
                            using (var stream = new MemoryStream())
                            {
                                var encoder = new JpegBitmapEncoder();
                                encoder.QualityLevel = Config.Current.Thumbnail.Quality;
                                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                                encoder.Save(stream);
                                return stream.ToArray();
                            }
                        }
                    }
                    finally
                    {
                        if (hBitmap != IntPtr.Zero)
                        {
                            DeleteObject(hBitmap);
                        }
                    }
                }
                catch
                {
                    // 静默失败，返回 null 使用默认图标
                }
                
                return null;
            }, token);
        }

        #region Shell API Interop

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SHCreateItemFromParsingName(
            [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            [In] IntPtr pbc,
            [In][MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [Out][MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] out object ppv);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [ComImport]
        [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemImageFactory
        {
            void GetImage(
                [In][MarshalAs(UnmanagedType.Struct)] SIZE size,
                [In] SIIGBF flags,
                [Out] out IntPtr phbm);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE
        {
            public int cx;
            public int cy;
        }

        [Flags]
        private enum SIIGBF
        {
            SIIGBF_RESIZETOFIT = 0x00,
            SIIGBF_BIGGERSIZEOK = 0x01,
            SIIGBF_MEMORYONLY = 0x02,
            SIIGBF_ICONONLY = 0x04,
            SIIGBF_THUMBNAILONLY = 0x08,
            SIIGBF_INCACHEONLY = 0x10,
        }

        #endregion
    }

}
