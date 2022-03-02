using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CoreGraphics;
using Foundation;
using Photos;
using UIKit;

namespace ToToolkit.Helpers
{
	public static partial class Gallery
	{
		static List<string> GetPlatformPathImages(int limit)
        {
            try
            {
                List<string> listOfAllImages = new();

                var fetchOptions = new PHFetchOptions();
                fetchOptions.SortDescriptors = new NSSortDescriptor[] { new NSSortDescriptor("modificationDate", false) };
                PHFetchResult fetchResults = PHAsset.FetchAssets(PHAssetMediaType.Image, fetchOptions);

                // TODO Cris: hrus cri cara lain ( cara yg skrng msih lelet & ngga efisien )
                foreach (PHAsset phasset in fetchResults.Take(limit))
                {
                    var assetResources = PHAssetResource.GetAssetResources(phasset);
                    Regex fileURLpattern = new Regex(@"file:.*");
                    var path = string.Empty;
                    foreach (var assetResource in assetResources)
                    {
                        var des = assetResource.DebugDescription;
                        Match patternUrlMatch = fileURLpattern.Match(des);
                        path = patternUrlMatch.Groups[0].Value;
                    }

                    //System.Diagnostics.Debug.WriteLine(path);
                    listOfAllImages.Add(path);
                }

                return listOfAllImages;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in {nameof(GetPlatformPathImages)}: {ex}");
#if DEBUG
                throw ex;
#else
                return null;
#endif
            }
        }

		static Stream GetPlatformStreamImage(string contentUri)
        {
            if (string.IsNullOrEmpty(contentUri))
                return null;

            using NSUrl url = new(contentUri);

            Stream stream = new FileStream(url.Path, FileMode.Open, FileAccess.Read);
            return stream;
        }

        static async Task<byte[]> GetPlatformThumbnailImage(string contentUri, int width, int height)
        {
            using (NSUrl url = new(contentUri))
            {
                using (NSData data = NSData.FromUrl(url))
                {
                    using (UIImage image = UIImage.LoadFromData(data))
                    {
                        CGSize size = new CGSize(width, height);

                        if (image is null)
                            return null;

                        // hati2 dengan UIImage ( ada bbrp API yg sangat sensitif dengan BackgroundThread alias hrus dipanggil dr MainThread )

                        // method GetImageByPreparingThumbnail bukan ThreadSafe
                        //UIImage thumbnailImage = image.GetImageByPreparingThumbnail(size);

                        //var thumbnailImage = await image.PrepareThumbnailAsync(size);
                        //return thumbnailImage?.AsJPEG()?.AsStream();

                        TaskCompletionSource<UIImage> tcsUIImage = new TaskCompletionSource<UIImage>();
                        UIApplication.SharedApplication.InvokeOnMainThread(() =>
                        {
                            image.PrepareThumbnail(size, (image) =>
                            {
                                tcsUIImage.SetResult(image);
                            });
                        });
                        
                        var thumbnailImage = await tcsUIImage.Task;
                        return thumbnailImage?.AsJPEG()?.ToArray();
                    }
                }
            }
        }
    }
}
	