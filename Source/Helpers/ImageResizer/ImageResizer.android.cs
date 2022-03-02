using System.IO;
using System.Threading.Tasks;
using Android.Graphics;

namespace ToToolkit.Helpers
{
    public static partial class ImageResizer
    {
		static Task<byte[]> PlatformResizeImage(byte[] imageData, float width, float height)
		{
			return Task.Run(() =>
			{
				// Load the bitmap
				Bitmap originalImage = BitmapFactory.DecodeByteArray(imageData, 0, imageData.Length);
				Bitmap resizedImage = Bitmap.CreateScaledBitmap(originalImage, (int)width, (int)height, false);

				using (MemoryStream ms = new MemoryStream())
				{
					resizedImage.Compress(Bitmap.CompressFormat.Jpeg, 100, ms);
					return ms.ToArray();
				}
			});
		}
	}
}
