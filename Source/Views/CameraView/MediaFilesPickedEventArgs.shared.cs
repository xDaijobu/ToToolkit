using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xamarin.Forms;

namespace ToToolkit.Views
{
    public class MediaFilesPickedEventArgs : EventArgs
    {
        public List<string> Paths;
        readonly Lazy<List<ImageSource>> imageSources;

        public MediaFilesPickedEventArgs(List<string> paths = null, List<byte[]> imageDatas = null)
        {
            Paths = paths;
            ImageDatas = imageDatas;
            imageSources = new Lazy<List<ImageSource>>(GetImageSources);
        }

        public List<byte[]> ImageDatas { get; }
        public List<ImageSource> Image => imageSources.Value;

        List<ImageSource> GetImageSources()
        {
            if (ImageDatas != null)
                return ImageDatas.Select(x => ImageSource.FromStream(() => new MemoryStream(x))).ToList();

            if (Paths?.Count() == 0)
                return null;

            return Paths.Select(x => ImageSource.FromFile(x)).ToList();
        }
    }
}
