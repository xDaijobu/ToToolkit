using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ToToolkit.Helpers
{
	public static partial class Gallery
	{
		static List<string> GetPlatformPathImages(int limit)
			=> throw new PlatformNotSupportedException();

		static Stream GetPlatformStreamImage(string contentUri)
			=> throw new PlatformNotSupportedException();

		static Task<byte[]> GetPlatformThumbnailImage(string contentUri, int width, int height)
			=> throw new PlatformNotSupportedException();
	}
}
