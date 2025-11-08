using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace NeeView
{
    public class ArchivePageThumbnail : PageThumbnail
    {
        private readonly ArchivePageContent _content;

        public ArchivePageThumbnail(ArchivePageContent content) : base(content)
        {
            _content = content;
            this.Thumbnail.IsCacheEnabled = true;
        }

        public override async ValueTask<ThumbnailSource> LoadThumbnailAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            NVDebug.AssertMTA();

            // .nov 文件特殊处理:直接作为视频文件生成缩略图
            // .nov 文件本身就是视频文件,只是加了 .nov 后缀
            var archiveEntry = _content.ArchiveEntry;
            if (archiveEntry.SystemPath.EndsWith(".nov", System.StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // 直接使用 .nov 文件生成视频缩略图
                    var thumbnailRaw = await MediaPageThumbnail.GenerateVideoThumbnailAsync(archiveEntry.SystemPath, token);
                    if (thumbnailRaw != null)
                    {
                        return new ThumbnailSource(thumbnailRaw);
                    }
                }
                catch
                {
                    // 如果生成失败，继续使用默认处理
                }
            }

            var pageContent = await ArchivePageUtility.GetSelectedPageContentAsync(_content.ArchiveEntry, false, token);
            pageContent.Decrypt = false;
            if (pageContent is ArchivePageContent)
            {
                if (pageContent.ArchiveEntry.IsMedia())
                {
                    return new ThumbnailSource(ThumbnailType.Media);
                }
                else
                {
                    return new ThumbnailSource(ThumbnailType.Empty);
                }
            }
            else
            {
                Debug.Assert(pageContent is not ArchivePageContent);
                var pageThumbnail = PageThumbnailFactory.Create(pageContent);
                return await pageThumbnail.LoadThumbnailAsync(token);
            }
        }
    }

}
