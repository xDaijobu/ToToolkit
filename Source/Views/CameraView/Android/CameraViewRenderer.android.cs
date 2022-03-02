using System.IO;
using System.Threading.Tasks;
using System.ComponentModel;
using Android.Util;
using Android.Content;
using Android.Graphics;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using AndroidX.ExifInterface.Media;
using Android.Widget;
using Android.Views;
using Java.Lang;
using Java.Util.Concurrent;
using ToToolkit.Views;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using AndroidX.Lifecycle;
using static AndroidX.Camera.Core.ImageCapture;
using Math = Java.Lang.Math;
using Exception = System.Exception;
using File = Java.IO.File;
using Environment = Android.OS.Environment;
using Android.OS;
using AndroidX.Camera.Core.Internal.Utils;
using Android.Content.PM;
using System;
using Xamarin.Essentials;
using Locale = Java.Util.Locale;
using CameraView = ToToolkit.Views.CameraView;

[assembly: ExportRenderer(typeof(CameraView), typeof(CameraViewRenderer))]
namespace ToToolkit.Views
{
    public class CameraViewRenderer : ViewRenderer<CameraView, PreviewView>
    {
        bool isDisposed;
        #region Camera Manager
        File outputDirectory;
        const string TAG = nameof(CameraViewRenderer);
        const string FILENAME_FORMAT = "yyyy-MM-dd-HH-mm-ss-SSS";
        string AppName
        {
            get
            {
                ApplicationInfo applicationInfo = _context.PackageManager.GetApplicationInfo(_context.ApplicationInfo.PackageName, 0);
                return applicationInfo != null ? _context.PackageManager.GetApplicationLabel(applicationInfo) : "ToCam";
            }
        }

        Preview cameraPreview;
        PreviewView previewView;
        IExecutorService cameraExecutor;
        CameraSelector cameraSelector;
        ProcessCameraProvider cameraProvider;
        ICamera camera;
        ImageCapture imageCapture;
        CameraOptions cameraOptions = CameraOptions.Rear;
        PreviewObserver previewObserver;
        ILifecycleOwner lifecycleOwner
        {
            get
            {
                return _context is ILifecycleOwner _lifecycleOwner ? _lifecycleOwner : _context.GetActivity() as ILifecycleOwner;
            }
        }

        MediaOptions mediaOptions;
        #endregion
        CameraView _currentElement;
        readonly Context _context;

        public CameraViewRenderer(Context context) : base(context)
        {
            _context = context;
        }

        protected async Task<PreviewView> InitPreviewView()
        {
            if (!await CheckAndRequestPermissions())
            {
                Element.RaiseMediaCaptureFailed("Permission not granted to photos");
                //ShowToast("Permission not granted to photos.");
                return null;
            }

            var previewView = new PreviewView(_context);
            UpdateCameraOptions(Element.CameraOptions);
            mediaOptions = Element.MediaOptions;
            outputDirectory = GetOutputDirectory();
            cameraExecutor = Executors.NewSingleThreadExecutor();
            Connect();

            if (!previewView.PreviewStreamState.HasActiveObservers)
            {
                // Callback for preview visibility
                previewObserver = new PreviewObserver(camera, previewView, mediaOptions?.Placemark);

                previewView.PreviewStreamState.Observe(lifecycleOwner, previewObserver);
            }

            return previewView;
        }

        protected override async void OnElementChanged(ElementChangedEventArgs<CameraView> e)
        {
            base.OnElementChanged(e);

            _currentElement = e.NewElement;
            previewView = await InitPreviewView();

            if (previewView != null)
                SetNativeControl(previewView);
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (Element == null || Control == null)
                return;

            if (e.PropertyName == nameof(CameraView.CameraOptions))
                UpdateCameraOptions(Element.CameraOptions);

            if (e.PropertyName == nameof(CameraView.FlashMode))
                SetFlash(Element.FlashMode);

            if (e.PropertyName == nameof(CameraView.MediaOptions))
            {
                mediaOptions = Element.MediaOptions;
                previewObserver?.UpdatePlacemark(mediaOptions?.Placemark);
            }
        }

        private void Connect()
        {
            var cameraProviderFuture = ProcessCameraProvider.GetInstance(_context);

            cameraProviderFuture.AddListener(new Runnable(() =>
            {
                try
                {
                    //System.Diagnostics.Debug.WriteLine("Add Listener");

                    // Used to bind the lifecycle of cameras to the lifecycle owner
                    cameraProvider = (ProcessCameraProvider)cameraProviderFuture.Get();

                    // Preview
                    cameraPreview = new Preview.Builder().Build();
                    cameraPreview.SetSurfaceProvider(previewView.SurfaceProvider);

                    // Take Photo
                    imageCapture = new Builder().Build();

                    UpdateCamera();

                    _currentElement.CameraClick = new Command(async () =>
                    {
                        _ = await TakePicture();
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in cameraProviderFuture.AddListener ! : " + ex);
                }

            }), ContextCompat.GetMainExecutor(_context)); //GetMainExecutor: returns an Executor that runs on the main thread.
        }

        private async Task<bool> TakePicture()
        {
            TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();

            // Get a stable reference of the modifiable image capture use case   
            var imageCapture = this.imageCapture;
            if (imageCapture == null)
                return false;

            // Set up image capture listener, which is triggered after photo has been taken
            #region Versi 1: This method provides an in-memory buffer of the captured image.
            imageCapture.TakePicture(ContextCompat.GetMainExecutor(_context), new ImageCapturedCallback(
                completionSource,
                onErrorCallback: (exc) =>
                {
                    var msg = $"Photo capture failed: {exc.Message}";

                    if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
                    {
                        if (!Environment.IsExternalStorageManager)
                        {
                            _context.GetActivity().StartActivityForResult(new Intent(Android.Provider.Settings.ActionManageAllFilesAccessPermission), 0);
                        }
                    }

                    _currentElement?.RaiseMediaCaptured(new MediaCapturedEventArgs(imageData: null, path: null));
                    Log.Error(TAG, msg, exc);
                    ShowToast(msg);

                    completionSource.TrySetException(exc);
                },
                onCapturedSuccessCallback: async (imageProxy) =>
                {
                    string path = $"{outputDirectory}/{new Java.Text.SimpleDateFormat(FILENAME_FORMAT, Locale.Us).Format(JavaSystem.CurrentTimeMillis())}{".jpg"}";

                    /* https://developer.android.com/reference/androidx/camera/core/ImageInfo#getRotationDegrees() */
                    int correctRotation = imageProxy.ImageInfo.RotationDegrees;

                    //System.Diagnostics.Debug.WriteLine("Correct Rotation: " + correctRotation);

                    if (Build.VERSION.SdkInt == BuildVersionCodes.LollipopMr1 || Build.VERSION.SdkInt == BuildVersionCodes.Lollipop)
                        correctRotation = cameraSelector == CameraSelector.DefaultFrontCamera ? -90 : 90;

                    /* imageproxy to bitmap */
                    var data = ImageUtil.ImageToJpegByteArray(imageProxy);
                    imageProxy.Close();
                    var imageBitmap = BitmapFactory.DecodeByteArray(data, 0, data.Length);

                    var result = await ProcessImage(mediaOptions, imageBitmap, correctRotation, path);

                    imageBitmap.Recycle();
                    imageBitmap.Dispose();
                    GC.Collect();

                    if (!result)
                        completionSource.SetCanceled();
                    else
                        completionSource.TrySetResult(true);
                }
            ));

            var result = await completionSource.Task;
            return result;
            #endregion
            //#region Versi 2: This method saves the captured image to the provided file location.
            //imageCapture.TakePicture(outputOptions, ContextCompat.GetMainExecutor(_context), new ImageSaveCallback(

            //    onErrorCallback: (exc) =>
            //    {
            //        var msg = $"Photo capture failed: {exc.Message}";


            //        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
            //        {
            //            if (!Android.OS.Environment.IsExternalStorageManager)
            //            {
            //                _context.GetActivity().StartActivityForResult(new Intent(Android.Provider.Settings.ActionManageAllFilesAccessPermission), 0);
            //            }
            //        }

            //        Log.Error(TAG, msg, exc);
            //        Toast.MakeText(_context, msg, ToastLength.Short).Show();
            //    },

            //    onImageSaveCallback: (output) =>
            //    {
            //        var data = System.IO.File.ReadAllBytes(photoFile?.Path);
            //        _currentElement?.RaiseMediaCaptured(new MediaCapturedEventArgs(imageData: data, path: photoFile?.Path));
            //    }
            //));
            //#endregion
        }

        // Save photos to => /Pictures/CameraX/
        private File GetOutputDirectory()
        {
            File mediaDir;
            if (string.IsNullOrEmpty(mediaOptions?.Directory))
#pragma warning disable CS0618 // Type or member is obsolete
                // solusi utk Android 11
                // _context.GetActivity().StartActivityForResult(new Intent(Android.Provider.Settings.ActionManageAllFilesAccessPermission), 0);
                mediaDir = Environment.GetExternalStoragePublicDirectory(System.IO.Path.Combine(Environment.DirectoryPictures, AppName));
#pragma warning restore CS0618 // Type or member is obsolete
            else
                mediaDir = new File(mediaOptions.Directory);

            System.Diagnostics.Debug.WriteLine("MediaDir Path: " + mediaDir.Path);
            if (mediaDir != null && mediaDir.Exists())
                return mediaDir;

            var file = new File(mediaDir, string.Empty);
            file.Mkdirs();
            return file;
        }

        public void Disconnect()
        { }

        private void UpdateCameraOptions(CameraOptions cameraOptions)
        {
            this.cameraOptions = cameraOptions;
            UpdateCamera();
        }

        /// <summary>
        /// Set On/Off Tourch
        /// </summary>
        /// <param name="flashMode"></param>
        private void SetFlash(FlashMode flashMode)
        {
            if (cameraOptions == CameraOptions.Front)
                return;

            switch (flashMode)
            {
                case FlashMode.On:
                    camera?.CameraControl?.EnableTorch(true);
                    break;
                case FlashMode.Off:
                    camera?.CameraControl?.EnableTorch(false);
                    break;
            }
        }

        private void SetZoomAndFocusTouchListener()
        {
            //#region Pinch to Zoom
            //ScaleGestureListener listener = new ScaleGestureListener(camera.CameraControl, camera.CameraInfo);
            //ScaleGestureDetector scaleGestureDetector = new ScaleGestureDetector(_context, listener);
            //ScaleTouchListener scaleTouchListener = new ScaleTouchListener(scaleGestureDetector);
            //previewView.SetOnTouchListener(scaleTouchListener);
            //#endregion

            //#region Tap to Focus
            //FocusTouchListener focusTouchListener = new FocusTouchListener(previewView.MeteringPointFactory, camera.CameraControl);
            //previewView.SetOnTouchListener(focusTouchListener);
            ////previewView.listen
            //#endregion

            #region Pinch to Zoom & Tap To Focus
            ScaleGestureListener listener = new ScaleGestureListener(camera.CameraControl, camera.CameraInfo);
            ScaleGestureDetector scaleGestureDetector = new ScaleGestureDetector(_context, listener);
            ZoomAndFocusTouchListener zoomAndFocusTouchListener = new ZoomAndFocusTouchListener(scaleGestureDetector, previewView.MeteringPointFactory, camera.CameraControl);
            previewView.SetOnTouchListener(zoomAndFocusTouchListener);
            #endregion
        }


        private void UpdateCamera()
        {
            if (cameraProvider != null)
            {
                // Unbind use cases before rebinding
                cameraProvider.UnbindAll();

                // Select back camera as a default, or front camera otherwise
                switch (cameraOptions)
                {
                    case CameraOptions.Rear when cameraProvider.HasCamera(CameraSelector.DefaultBackCamera):
                        cameraSelector = CameraSelector.DefaultBackCamera;
                        break;
                    case CameraOptions.Front when cameraProvider.HasCamera(CameraSelector.DefaultFrontCamera):
                        cameraSelector = CameraSelector.DefaultFrontCamera;
                        break;
                    default:
                        cameraSelector = CameraSelector.DefaultBackCamera;
                        break;
                }

                if (cameraSelector == null)
                    throw new Exception("Camera not found");

                // The Context here SHOULD be something that's a lifecycle owner
                if (lifecycleOwner != null)
                    camera = cameraProvider.BindToLifecycle(lifecycleOwner, cameraSelector, imageCapture, cameraPreview);//, imageAnalyzer);

                SetFlash(Element.FlashMode);

                SetZoomAndFocusTouchListener();
            }
        }

        private void UpdateTorch(bool on)
        {
            camera?.CameraControl?.EnableTorch(on);
        }

        protected override void Dispose(bool disposing)
        {
            if (isDisposed)
                return;

            isDisposed = true;

            //System.Diagnostics.Debug.WriteLine("Dispose !");
            cameraExecutor?.Shutdown();
            cameraExecutor?.Dispose();
            imageCapture?.Dispose();
            cameraSelector?.Dispose();
            cameraProvider?.Dispose();
            camera?.Dispose();
            previewView?.PreviewStreamState?.RemoveObservers(lifecycleOwner);
            previewObserver?.Dispose();
            cameraPreview?.Dispose();

            base.Dispose(disposing);
        }

        // Fix
        // - Orientation
        // - Resize
        // - Flip
        // - Scaled
        // - Placemark
        private Task<bool> ProcessImage(MediaOptions options, Bitmap originalImage, int rotation, string outputPath)
        {
            if (originalImage == null)
                return Task.FromResult(false);

            return Task.Run(async () =>
            {
                try
                {
                    var percent = 1.0f;
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

                    if (originalImage == null)
                        return false;

                    if (mediaOptions.PhotoSize == PhotoSize.MaxWidthHeight && mediaOptions.MaxWidthHeight.HasValue)
                    {
                        var max = Math.Max(originalImage.Width, originalImage.Height);
                        if (max > mediaOptions.MaxWidthHeight)
                        {
                            percent = (float)mediaOptions.MaxWidthHeight / max;
                        }
                    }

                    var finalWidth = (int)(originalImage.Width * percent);
                    var finalHeight = (int)(originalImage.Height * percent);

                    // Scale Image
                    if (finalWidth != originalImage.Width || finalHeight != originalImage.Height)
                        originalImage = Bitmap.CreateScaledBitmap(originalImage, finalWidth, finalHeight, true);

                    // Rotate Image
                    if (rotation != 0)
                    {
                        var matrix = new Matrix();
                        matrix.PostRotate(rotation);
                        originalImage = Bitmap.CreateBitmap(originalImage, 0, 0, originalImage.Width, originalImage.Height, matrix, true);
                    }

                    // Flip Image
                    originalImage = DoFlipHorizontal(originalImage, rotation);
                    // dd-mmm-yyyy hh24:mm:ss
                    // Compress
                    var compressFormat = Bitmap.CompressFormat.Jpeg;
                    using MemoryStream memoryStream = new MemoryStream();
                    await originalImage.CompressAsync(compressFormat, mediaOptions.CompressionQuality, memoryStream);
                    memoryStream.Position = 0;
                    originalImage = await BitmapFactory.DecodeStreamAsync(memoryStream);

                    // Draw Placemark
                    if (mediaOptions?.Placemark != null)
                        originalImage = DrawPlacemark(originalImage, mediaOptions?.Placemark);
                    
                    // Final
                    using FileStream stream = new FileStream(outputPath, FileMode.Create);
                    await originalImage.CompressAsync(compressFormat, 100, stream);

                    #region Metadata
                    ExifInterface exif = new ExifInterface(outputPath);

                    //set scaled and rotated image dimensions
                    exif?.SetAttribute(ExifInterface.TagPixelXDimension, Integer.ToString(finalWidth));
                    exif?.SetAttribute(ExifInterface.TagPixelYDimension, Integer.ToString(finalHeight));

                    var location = mediaOptions?.Placemark?.Location;
                    if (location != null)
                    {
                        //https://stackoverflow.com/questions/5280479/how-to-save-gps-coordinates-in-exif-data-on-android
                        exif?.SetAttribute(ExifInterface.TagGpsLatitude, CoordinateToRational(location.Latitude));
                        exif?.SetAttribute(ExifInterface.TagGpsLongitude, CoordinateToRational(location.Longitude));

                        exif?.SetAttribute(ExifInterface.TagGpsLatitudeRef, location.Latitude < 0 ? "S" : "N");
                        exif?.SetAttribute(ExifInterface.TagGpsLongitudeRef, location.Longitude < 0 ? "W" : "E");
                    }

                    exif?.SetAttribute(ExifInterface.TagMake, Build.Manufacturer);
                    exif?.SetAttribute(ExifInterface.TagModel, Build.Model);
                    exif?.SetAttribute(ExifInterface.TagDatetime, DateTime.Now.ToString("yyyy:MM:dd hh:mm:ss"));
                    exif?.SetAttribute(ExifInterface.TagOrientation, Integer.ToString(rotation));

                    try
                    {
                        exif?.SaveAttributes();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Unable to save exif {ex}");
                    }
                    
                    exif?.Dispose();

                    #endregion

                    var bytes = await GetBytesAsync(stream);
                    _currentElement?.RaiseMediaCaptured(new MediaCapturedEventArgs(imageData: bytes, path: outputPath));

                    originalImage.Recycle();
                    originalImage.Dispose();
                    // Dispose of the Java side bitmap.
                    GC.Collect();
                    return true;
                }
                catch (Exception ex)
                {
#if DEBUG
                    throw ex;
#else
                        return false;
#endif
                }
            });
        }

        private Bitmap DrawPlacemark(Bitmap originalImage, Placemark placemark)
        {
            Bitmap mutableBitmap = originalImage.Copy(Bitmap.Config.Argb8888, true);
            Canvas canvas = new Canvas(mutableBitmap);

            CanvasHelper.DrawPlacemarkView(canvas, mutableBitmap.Width, mutableBitmap.Height, placemark, isVisibleCreated: true);

            canvas.Dispose();
            return mutableBitmap;
        }

        /// <summary>
        /// Front camera ( mirror ), jdi harus gambar nya harus di flip
        /// </summary>
        private Bitmap DoFlipHorizontal(Bitmap originalImage, int rotation)
        {
            if (rotation <= 90 && rotation >= 0 || cameraOptions != CameraOptions.Front)
                return originalImage;

            Bitmap mutableBitmap = originalImage.Copy(Bitmap.Config.Argb8888, true);
            Canvas canvas = new Canvas(originalImage);
            Matrix flipHorizontalMatrix = new Matrix();
            flipHorizontalMatrix.SetScale(-1, 1);
            flipHorizontalMatrix.PostTranslate(mutableBitmap.Width, 0);
            canvas.DrawBitmap(mutableBitmap, flipHorizontalMatrix, null);
            canvas.Dispose();

            return originalImage;
        }

        private async Task<byte[]> GetBytesAsync(Stream input)
        {
            using MemoryStream memoryStream = new MemoryStream();
            input.Position = 0;

            await input.CopyToAsync(memoryStream);

            return memoryStream.ToArray();
        }

        #region Permissions
        private async Task<bool> CheckAndRequestPermissions()
        {
            return
                (await Permissions.RequestAsync<Permissions.Camera>()) == PermissionStatus.Granted &&
                (await Permissions.RequestAsync<Permissions.StorageRead>()) == PermissionStatus.Granted &&
                (await Permissions.RequestAsync<Permissions.StorageWrite>()) == PermissionStatus.Granted;
        }

        private void ShowToast(string message)
        {
            Toast.MakeText(_context, message, ToastLength.Short).Show();
        }
        #endregion

        private string CoordinateToRational(double coord)
        {
            coord = coord > 0 ? coord : -coord;
            var degrees = (int)coord;
            coord = (coord % 1) * 60;
            var minutes = (int)coord;
            coord = (coord % 1) * 60000;
            var sec = (int)coord;

            return $"{degrees}/1,{minutes}/1,{sec}/1000";
        }
    }
}