using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using CoreGraphics;
using CoreImage;
using Foundation;
using ImageIO;
using MobileCoreServices;
using Photos;
using ToToolkit.Helpers;
using ToToolkit.Views;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(CameraView), typeof(CameraViewRenderer))]
namespace ToToolkit.Views
{
	// Source Code: https://github.com/xamarin/XamarinCommunityToolkit/blob/main/src/CommunityToolkit/Xamarin.CommunityToolkit/Views/CameraView/iOS/CameraViewRenderer.ios.cs
	public class CameraViewRenderer : ViewRenderer<CameraView, FormsCameraView>
	{
		bool disposed;
		DateTime timestamp;
		protected override void OnElementChanged(ElementChangedEventArgs<CameraView> e)
		{
			base.OnElementChanged(e);

			if (Control == null && !disposed)
			{
				SetNativeControl(new FormsCameraView(Element.MediaOptions));
				_ = Control ?? throw new NullReferenceException($"{nameof(Control)} cannot be null");
				Control.FinishCapture += FinishCapture;

				Control.SetBounds(Element.WidthRequest, Element.HeightRequest);
				Control.RetrieveCameraDevice(Element.CameraOptions);
				Control.SwitchFlash(Element.FlashMode);
			}

			if (e.OldElement != null)
				e.OldElement.CameraClick = null;

			if (e.NewElement != null)
				e.NewElement.CameraClick = new Command(() => TakePhoto());
		}

		private void FinishCapture(object sender, Tuple<NSObject, NSError> e)
		{
			if (Element == null || Control == null)
				return;

			if (e.Item2 != null)
			{
				Element.RaiseMediaCaptureFailed(e.Item2.LocalizedDescription);
				return;
			}

            NSData photoData = e.Item1 as NSData;

			PHObjectPlaceholder placeholder = null;
			PHPhotoLibrary.RequestAuthorization(status =>
			{
				if (status != PHAuthorizationStatus.Authorized)
                {
					Element.RaiseMediaCaptureFailed("Permission not granted to photos");
					return;
				}

				// Save the captured file to the photo library.
				PHPhotoLibrary.SharedPhotoLibrary.PerformChanges(() =>
				{
					timestamp = DateTime.Now;
                    PHAssetCreationRequest creationRequest = PHAssetCreationRequest.CreationRequestForAsset();
					if (photoData != null)
                    {
                        using NSData finalPhotoData = ResizeAndCompressImage(Element.MediaOptions, photoData);
                        creationRequest.AddResource(PHAssetResourceType.Photo, finalPhotoData, null);

                        placeholder = creationRequest.PlaceholderForCreatedAsset;

                        // iOS15 ?, cara baru utk setting gps a.k.a location 
                        Location location = Element.MediaOptions.Placemark.Location;
                        if (location != null)
                            creationRequest.Location = new CoreLocation.CLLocation(location.Latitude, location.Longitude);
                    }
                }, (success2, error2) =>
				{
					if (!success2)
					{
						Debug.WriteLine($"Could not save media to photo library: {error2}");
						if (error2 != null)
						{
							Element.RaiseMediaCaptureFailed(error2.LocalizedDescription);
							return;
						}
						Element.RaiseMediaCaptureFailed($"Could not save media to photo library");
						return;
					}

					_ = placeholder ?? throw new NullReferenceException();
					if (PHAsset.FetchAssetsUsingLocalIdentifiers(new[] { placeholder.LocalIdentifier }, null).firstObject is not PHAsset asset)
					{
						Element.RaiseMediaCaptureFailed($"Could not save media to photo library");
						return;
					}

					if (asset.MediaType == PHAssetMediaType.Image)
					{
						asset.RequestContentEditingInput(new PHContentEditingInputRequestOptions
						{
							CanHandleAdjustmentData = p => true
						}, (input, info) =>
						{
							Device.BeginInvokeOnMainThread(() =>
							{
								using (var memoryStream = new MemoryStream())
								{
									string path = input.FullSizeImageUrl?.Path;
									Stream imageData = Gallery.GetStreamImage(path);
									imageData.CopyTo(memoryStream);
									Element.RaiseMediaCaptured(new MediaCapturedEventArgs(path, memoryStream.ToArray()));
								}
							});
						});
					}
				});
			});

		}

		protected override void Dispose(bool disposing)
		{
			if (disposed)
				return;

			disposed = true;
			Control?.Dispose();
			base.Dispose(disposing);
		}

		protected override void OnElementPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			base.OnElementPropertyChanged(sender, e);

			if (Element == null || Control == null)
				return;

			switch (e.PropertyName)
			{
				case nameof(CameraView.CameraOptions):
					Control.RetrieveCameraDevice(Element.CameraOptions);
					break;
				case nameof(Element.Height):
				case nameof(Element.Width):
				case nameof(VisualElement.HeightProperty):
				case nameof(VisualElement.WidthProperty):
					Control.SetBounds(Element.Width, Element.Height);
					Control.SetOrientation();
					break;
				case nameof(CameraView.FlashMode):
					Control.SwitchFlash(Element.FlashMode);
					break;
			}
		}

		private async void TakePhoto()
		{
			if (Control != null)
				await Control.TakePhoto();
		}

		private NSData ResizeAndCompressImage(MediaOptions mediaOptions, NSData photoData)
        {
            UIImage image = UIImage.LoadFromData(photoData);
            float percent = 1.0f;
            if (mediaOptions.PhotoSize != PhotoSize.Full)
            {
                try
                {
                    switch (mediaOptions.PhotoSize)
                    {
                        case PhotoSize.Large:
                            percent = .75f;
                            break;
                        case PhotoSize.Medium:
                            percent = .5f;
                            break;
                        case PhotoSize.Small:
                            percent = .25f;
                            break;
                        case PhotoSize.Custom:
                            percent = mediaOptions.CustomPhotoSize / 100f;
                            break;
                    }

                    if (mediaOptions.PhotoSize == PhotoSize.MaxWidthHeight && mediaOptions.MaxWidthHeight.HasValue)
                    {
                        double max = Math.Max(image.Size.Width, image.Size.Height);
                        if (max > mediaOptions.MaxWidthHeight.Value)
                            percent = mediaOptions.MaxWidthHeight.Value / (float)max;
                    }

                    //begin resizing image
                    if (percent < 1.0f)
                        image = image.ResizeImageWithAspectRatio(percent);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Unable to compress image: {ex}");
                }
            }

            NSDictionary meta = null;
            try
            {
                meta = PhotoLibraryAccess.GetPhotoLibraryMetadata(photoData);
                meta = SetGpsLocation(meta, mediaOptions.Placemark?.Location);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unable to get metadata: {ex}");
            }

            //iOS quality is 0.0-1.0
            float finalQuality = (mediaOptions.CompressionQuality / 100f);
            NSData imageData = image.AsJPEG(finalQuality);

            //continue to move down quality , rare instances
            while (imageData == null && finalQuality > 0)
            {
                finalQuality -= 0.05f;
                imageData = image.AsJPEG(finalQuality);
            }

            if (imageData == null)
                throw new NullReferenceException("Unable to convert image to jpeg, please ensure file exists or lower quality level");

            InvokeOnMainThread(() =>
            {
                using (UILabel placemarkLabel = new UILabel())
                {
                    placemarkLabel.Text = mediaOptions.Placemark.ToString();
                    placemarkLabel.TextColor = UIColor.White;
                    placemarkLabel.Font = UIFont.SystemFontOfSize(12, 5);
                    placemarkLabel.Lines = 0;
                    placemarkLabel.SizeToFit();
                    placemarkLabel.TextAlignment = UITextAlignment.Right;
                    placemarkLabel.BackgroundColor = UIColor.Clear;

                    nfloat rightMargin = 4;
                    // RightCenter a.k.a Horizontal.End + Vertical.Center 
                    placemarkLabel.Frame = new CGRect(
                                                x: UIScreen.MainScreen.Bounds.GetMaxX() - placemarkLabel.Bounds.Width - rightMargin,
                                                y: UIScreen.MainScreen.Bounds.GetMidY() - placemarkLabel.Bounds.Height / 2,
                                                width: placemarkLabel.Bounds.Width,
                                                height: placemarkLabel.Bounds.Height);

                    using UIImage placemarkImage = placemarkLabel.ToUIImage();
                    image = UIImage.LoadFromData(imageData);

					image = image.GetImageFlippedForRightToLeftLayoutDirection();
					
                    image = image.OverlayWith(placemarkImage);
                }

                image = image.RotateImage(degree: -90);
				
                imageData = image.AsJPEG();
            });

            NSData finalData = GetImageWithMetadata(imageData, meta);
            image?.Dispose();
            image = null;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);

            return finalData;
        }

        private NSData GetImageWithMetadataiOS13(NSData imageData, NSDictionary meta)
		{
			try
			{
				// Copy over meta data
				using CIImage ciImage = CIImage.FromData(imageData);
				using CIImage newImageSource = ciImage.CreateBySettingProperties(meta);
				using CIContext ciContext = new CIContext();

				return ciContext.GetJpegRepresentation(newImageSource, CGColorSpace.CreateSrgb(), new NSDictionary());
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Unable to save image with metadata: {ex}");
				throw ex;
			}
		}

		private NSData GetImageWithMetadata(NSData imageData, NSDictionary meta)
		{
			if (UIDevice.CurrentDevice.CheckSystemVersion(13, 0))
				return GetImageWithMetadataiOS13(imageData, meta);

			try
			{
                CGDataProvider dataProvider = new CGDataProvider(imageData);
                CGImage cgImageFromJpeg = CGImage.FromJPEG(dataProvider, null, false, CGColorRenderingIntent.Default);
                NSMutableData imageWithExif = new NSMutableData();
                CGImageDestination destination = CGImageDestination.Create(imageWithExif, UTType.JPEG, 1);
                CGMutableImageMetadata cgImageMetadata = new CGMutableImageMetadata();
                CGImageDestinationOptions destinationOptions = new CGImageDestinationOptions();

				if (meta.ContainsKey(ImageIO.CGImageProperties.Orientation))
					destinationOptions.Dictionary[ImageIO.CGImageProperties.Orientation] = meta[ImageIO.CGImageProperties.Orientation];

				if (meta.ContainsKey(ImageIO.CGImageProperties.DPIWidth))
					destinationOptions.Dictionary[ImageIO.CGImageProperties.DPIWidth] = meta[ImageIO.CGImageProperties.DPIWidth];

				if (meta.ContainsKey(ImageIO.CGImageProperties.DPIHeight))
					destinationOptions.Dictionary[ImageIO.CGImageProperties.DPIHeight] = meta[ImageIO.CGImageProperties.DPIHeight];

				if (meta.ContainsKey(ImageIO.CGImageProperties.ExifDictionary))
					destinationOptions.ExifDictionary = new CGImagePropertiesExif(meta[ImageIO.CGImageProperties.ExifDictionary] as NSDictionary);

				if (meta.ContainsKey(ImageIO.CGImageProperties.TIFFDictionary))
				{
                    NSDictionary existingTiffDict = meta[ImageIO.CGImageProperties.TIFFDictionary] as NSDictionary;
					if (existingTiffDict != null)
					{
                        NSMutableDictionary newTiffDict = new NSMutableDictionary();
						newTiffDict.SetValuesForKeysWithDictionary(existingTiffDict);
						newTiffDict.SetValueForKey(meta[ImageIO.CGImageProperties.Orientation], ImageIO.CGImageProperties.TIFFOrientation);
						destinationOptions.TiffDictionary = new CGImagePropertiesTiff(newTiffDict);
					}
				}

				if (meta.ContainsKey(ImageIO.CGImageProperties.GPSDictionary))
					destinationOptions.GpsDictionary = new CGImagePropertiesGps(meta[ImageIO.CGImageProperties.GPSDictionary] as NSDictionary);

				if (meta.ContainsKey(ImageIO.CGImageProperties.JFIFDictionary))
					destinationOptions.JfifDictionary = new CGImagePropertiesJfif(meta[ImageIO.CGImageProperties.JFIFDictionary] as NSDictionary);

				if (meta.ContainsKey(ImageIO.CGImageProperties.IPTCDictionary))
					destinationOptions.IptcDictionary = new CGImagePropertiesIptc(meta[ImageIO.CGImageProperties.IPTCDictionary] as NSDictionary);

				destination.AddImageAndMetadata(cgImageFromJpeg, cgImageMetadata, destinationOptions);
                bool success = destination.Close();
				if (success)
					return imageWithExif;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Unable to save image with metadata: {ex}");
				throw ex;
			}

			return null;
		}

		private NSDictionary SetGpsLocation(NSDictionary meta, Location location)
		{
            NSMutableDictionary newMeta = new NSMutableDictionary();
			newMeta.SetValuesForKeysWithDictionary(meta);
            NSMutableDictionary newGpsDict = new NSMutableDictionary();
			newGpsDict.SetValueForKey(new NSNumber(Math.Abs(location.Latitude)), ImageIO.CGImageProperties.GPSLatitude);
			newGpsDict.SetValueForKey(new NSString(location.Latitude > 0 ? "N" : "S"), ImageIO.CGImageProperties.GPSLatitudeRef);
			newGpsDict.SetValueForKey(new NSNumber(Math.Abs(location.Longitude)), ImageIO.CGImageProperties.GPSLongitude);
			newGpsDict.SetValueForKey(new NSString(location.Longitude > 0 ? "E" : "W"), ImageIO.CGImageProperties.GPSLongitudeRef);
			//newGpsDict.SetValueForKey(new NSNumber(location.Altitude), ImageIO.CGImageProperties.GPSAltitude);
			newGpsDict.SetValueForKey(new NSNumber(0), ImageIO.CGImageProperties.GPSAltitudeRef);
			//newGpsDict.SetValueForKey(new NSNumber(location.Speed), ImageIO.CGImageProperties.GPSSpeed);
			newGpsDict.SetValueForKey(new NSString("K"), ImageIO.CGImageProperties.GPSSpeedRef);
			//newGpsDict.SetValueForKey(new NSNumber(location.Direction), ImageIO.CGImageProperties.GPSImgDirection);
			newGpsDict.SetValueForKey(new NSString("T"), ImageIO.CGImageProperties.GPSImgDirectionRef);
			newGpsDict.SetValueForKey(new NSString(timestamp.ToString("hh:mm:ss", CultureInfo.InvariantCulture)), ImageIO.CGImageProperties.GPSTimeStamp);
			newGpsDict.SetValueForKey(new NSString(timestamp.ToString("yyyy:MM:dd", CultureInfo.InvariantCulture)), ImageIO.CGImageProperties.GPSDateStamp);
			newMeta[ImageIO.CGImageProperties.GPSDictionary] = newGpsDict;
			return newMeta;
		}
	}
}
