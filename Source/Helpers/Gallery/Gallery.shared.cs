using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ToToolkit.Helpers
{
    public static partial class Gallery
    {
        public static List<string> GetPathImages(int limit = 200)
            => GetPlatformPathImages(limit);

        public static Stream GetStreamImage(string contentUri)
            => GetPlatformStreamImage(contentUri);

        public static Task<byte[]> GetThumbnailImage(string contentUri, int width, int height)
            => GetPlatformThumbnailImage(contentUri, width, height);
    }
}
