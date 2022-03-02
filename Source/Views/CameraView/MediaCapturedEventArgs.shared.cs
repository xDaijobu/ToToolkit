using System;
using System.IO;
using Xamarin.Forms;

namespace ToToolkit.Views
{
	public class MediaCapturedEventArgs : EventArgs
	{
		public string Path;
		readonly Lazy<ImageSource> imageSource;

		public MediaCapturedEventArgs(
			string path = null,
			byte[] imageData = null,
			double rotation = 0)
		{
			Path = path;
			Rotation = rotation;
			ImageData = imageData;
			imageSource = new Lazy<ImageSource>(GetImageSource);
		}

		public byte[] ImageData { get; }

		public double Rotation { get; }

		public ImageSource Image => imageSource.Value;

		ImageSource GetImageSource()
		{
			if (ImageData != null)
				return ImageSource.FromStream(() => new MemoryStream(ImageData));

			return !string.IsNullOrEmpty(Path) ? Path : null;
		}
	}
}
