using System;
using System.Threading.Tasks;

namespace ToToolkit.Helpers
{
    public static partial class ImageResizer
    {
        static Task<byte[]> PlatformResizeImage(byte[] imageData, float width, float height)
            => throw new PlatformNotSupportedException();
    }
}