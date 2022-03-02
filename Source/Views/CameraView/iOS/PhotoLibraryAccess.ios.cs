using System;
using CoreImage;
using Foundation;

namespace ToToolkit.Views
{
    public static class PhotoLibraryAccess
    {
        public static NSDictionary GetPhotoLibraryMetadata(NSData photoData)
        {
            NSDictionary meta = null;

            try
            {
                var fullimage = CIImage.FromData(photoData);
                if (fullimage?.Properties != null)
                {
                    meta = new NSMutableDictionary
                    {
                        [ImageIO.CGImageProperties.Orientation] = NSNumber.FromNInt((int)(fullimage.Properties.Orientation ?? CIImageOrientation.TopLeft)),
                        [ImageIO.CGImageProperties.ExifDictionary] = fullimage.Properties.Exif?.Dictionary ?? new NSDictionary(),
                        [ImageIO.CGImageProperties.TIFFDictionary] = fullimage.Properties.Tiff?.Dictionary ?? new NSDictionary(),
                        [ImageIO.CGImageProperties.GPSDictionary] = fullimage.Properties.Gps?.Dictionary ?? new NSDictionary(),
                        [ImageIO.CGImageProperties.IPTCDictionary] = fullimage.Properties.Iptc?.Dictionary ?? new NSDictionary(),
                        [ImageIO.CGImageProperties.JFIFDictionary] = fullimage.Properties.Jfif?.Dictionary ?? new NSDictionary()
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return meta;
        }
    }
}
