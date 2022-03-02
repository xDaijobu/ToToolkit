using System.Threading.Tasks;

namespace ToToolkit.Helpers
{
    public static partial class ImageResizer
    {
        public static Task<byte[]> ResizeImage(byte[] imageData, float width, float height)
        {
            return PlatformResizeImage(imageData, width, height);
        }
    }
}
