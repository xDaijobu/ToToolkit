using System;
using System.Collections.Generic;
using System.IO;
using Android.App;
using Android.Content;
using Android.Provider;
using Android.OS;
using Android.Util;
using Android.Graphics;
using System.Threading.Tasks;

namespace ToToolkit.Helpers
{
    public static partial class Gallery
    {
        /*
        /// <summary>
        /// RealPath, ex: /storage/emulated/0/Pictures/ToCam/2022-01-10-10-11-16-141.jpg
        /// </summary>
        /// <returns></returns>
		static List<string> GetPlatformPathImages()
		{
            List<string> listOfAllImages = new List<string>();
            var uri = MediaStore.Images.Media.ExternalContentUri;

            //MediaStore.Images.ImageColumns.Data is deprecated
            string columnData = "_data"; // hardcode dl

            string[] projection = { MediaStore.Images.Media.InterfaceConsts.Id, columnData };
            var cursor = Application.Context.ContentResolver.Query(uri, projection, null, null, null);

            if (cursor != null)
            {
                var columnIndexData = cursor.GetColumnIndexOrThrow(columnData);

                while (cursor.MoveToNext())
                {
                    string pathOfImage = cursor.GetString(columnIndexData);
                    System.Diagnostics.Debug.WriteLine(pathOfImage);
                    listOfAllImages.Add(pathOfImage);
                }

                cursor.Close();
            }

            return listOfAllImages;
        }
        */

        /// <summary>
        /// ContentUri, ex: content://media/external/images/media/127
        /// </summary>
        /// <returns></returns>
        static List<string> GetPlatformPathImages(int limit)
        {
            try
            {
                int counter = 0;
                List<string> listOfAllImages = new List<string>();
                var uri = MediaStore.Images.Media.ExternalContentUri;

                string[] projection = { MediaStore.Images.Media.InterfaceConsts.Id };
                var cursor = Application.Context.ContentResolver.Query(uri, projection, null, null, MediaStore.Images.Media.InterfaceConsts.DateTaken);

                if (cursor != null)
                {
                    while (cursor.MoveToNext())
                    {
                        if (counter > limit)
                            break;

                        int columnIndexId = cursor.GetColumnIndexOrThrow(MediaStore.Images.Media.InterfaceConsts.Id);
                        Android.Net.Uri contentUri = ContentUris.WithAppendedId(uri, cursor.GetLong(columnIndexId));

                        string pathOfImage = contentUri.ToString();
                        counter++;
                        listOfAllImages.Add(pathOfImage);
                    }
                    cursor.Close();
                }

                //uri = MediaStore.Images.Media.InternalContentUri;
                //cursor = Application.Context.ContentResolver.Query(uri, projection, null, null, MediaStore.Images.Media.InterfaceConsts.DateTaken);

                //if (cursor != null)
                //{
                //    while (cursor.MoveToNext())
                //    {
                //        int columnIndexId = cursor.GetColumnIndexOrThrow(MediaStore.Images.Media.InterfaceConsts.Id);
                //        Android.Net.Uri contentUri = ContentUris.WithAppendedId(uri, cursor.GetLong(columnIndexId));

                //        string pathOfImage = contentUri.ToString();

                //        listOfAllImages.Add(pathOfImage);
                //    }
                //    cursor.Close();
                //}


                return listOfAllImages;
            }
            catch(Exception ex)
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

            return Application.Context.ContentResolver.OpenInputStream(Android.Net.Uri.Parse(contentUri));
        }

        static Task<byte[]> GetPlatformThumbnailImage(string contentUri, int width, int height)
        {
            var uri = Android.Net.Uri.Parse(contentUri);
            Size size = new Size(width, height);
            var cancellationSignal = new CancellationSignal();
            Bitmap bitmap = Application.Context.ContentResolver.LoadThumbnail(uri, size, cancellationSignal);


            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Compress(Bitmap.CompressFormat.Jpeg, 100, ms);
                return Task.FromResult(ms.ToArray());
            }
        }
    }
}
