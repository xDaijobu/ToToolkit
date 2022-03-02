using System;
using System.Drawing;
using System.Threading.Tasks;
using CoreGraphics;
using UIKit;

namespace ToToolkit.Helpers
{
    public static partial class ImageResizer
    {
        static Task<byte[]> PlatformResizeImage(byte[] imageData, float width, float height)
        {
            return Task.Run(() =>
            {
                UIImage originalImage = ImageFromByteArray(imageData);
                UIImageOrientation orientation = originalImage.Orientation;

                //create a 24bit RGB image
                using (CGBitmapContext context = new CGBitmapContext(IntPtr.Zero,
                                                     (int)width, (int)height, 8,
                                                     4 * (int)width, CGColorSpace.CreateDeviceRGB(),
                                                     CGImageAlphaInfo.PremultipliedFirst))
                {

                    RectangleF imageRect = new RectangleF(0, 0, width, height);

                    // draw the image
                    context.DrawImage(imageRect, originalImage.CGImage);

                    UIKit.UIImage resizedImage = UIImage.FromImage(context.ToImage(), 0, orientation);

                    // save the image as a jpeg
                    return resizedImage.AsJPEG().ToArray();
                }
            });
        }

        static UIImage ImageFromByteArray(byte[] data)
        {
            if (data == null)
                return null;

            UIImage image;
            try
            {
                image = new UIKit.UIImage(Foundation.NSData.FromArray(data));
            }
            catch (Exception e)
            {
                Console.WriteLine("Image load failed: " + e.Message);
                return null;
            }
            return image;
        }
    }
}
