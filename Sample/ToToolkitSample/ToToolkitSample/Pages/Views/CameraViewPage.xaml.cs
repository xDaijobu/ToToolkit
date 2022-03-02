using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using ToToolkit.Helpers;
using ToToolkit.Views;
using Xamarin.CommunityToolkit.ObjectModel;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace ToToolkitSample.Pages.Views
{	
	public partial class CameraViewPage : ContentPage
    {
        public CameraMode Mode
        {
            set
            {
                if (value == CameraMode.TakePhoto)
                {
                    ImagesView.IsVisible = false;
                }
                else
                {
                    ObservableCollection<ImagePreview> FinalImages = new();
                    ImagesView.ItemsSource = FinalImages;

                    _ = Task.Run(async () =>
                    {
                        var pathImages = Gallery.GetPathImages(250);

                        foreach (var path in pathImages)
                        {
                            try
                            {
                                // Get Thumbnail as byte[]
                                var imageData = await Gallery.GetThumbnailImage(contentUri: path, width: 150, height: 150);

                                if (imageData != null)
                                {
                                    // Byte[] To ImagEsource
                                    var imageSource = ImageSource.FromStream(() => new MemoryStream(imageData));

                                    FinalImages.Add(new ImagePreview(imageSource, path));

                                    // Update UI
                                    OnPropertyChanged(nameof(FinalImages));
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("Error: " + ex);
                            }
                        }
                    });
                }
            }
        }

        public ICommand TakePhotoCommand => new Command(() =>
        {
            SetEnabledButtons(false);
            cameraPreview.CameraClick.Execute(null);
        });

        public ICommand CloseCommand => new Command(async () =>
        {
            SetEnabledButtons(false);
            await Navigation.PopModalAsync();
            Result.TrySetResult(string.Empty);
        });

        public ICommand TorchSwitchCommand => new Command(() =>
        {
            cameraPreview.FlashMode = cameraPreview.FlashMode == FlashMode.Off ? FlashMode.On : FlashMode.Off;
        });

        public ICommand CameraSwitchCommand => new Command(async () =>
        {
            SetEnabledButtons(false);

            cameraPreview.CameraOptions = cameraPreview.CameraOptions == CameraOptions.Rear ? CameraOptions.Front : CameraOptions.Rear;

            await Task.Delay(1000);

            SetEnabledButtons(true);

            btnFlash.IsEnabled = cameraPreview.CameraOptions != CameraOptions.Front;
        });

        public IAsyncCommand<ImagePreview> PreviewPhotoCommand => new AsyncCommand<ImagePreview>(async (data) =>
        {
            MainLayout.IsEnabled = false;

            await BigImage.FadeTo(0.25, 100);
            await BigImage.FadeTo(0.5, 100);

            BackgroundColor = Color.Black;

            await cameraPreview.FadeTo(0, 100);
            await MainLayout.FadeTo(0, 100);

            BigImage.ClassId = data.Uri;
            // Uri To Stream
            Stream stream = Gallery.GetStreamImage(data.Uri);
            BigImage.Source = ImageSource.FromStream(() => stream);
            BigImage.IsVisible = true;
            BigImage.CloseCommand = HideCommand;
            BigImage.OkCommand = OkCommand;

            await BigImage.FadeTo(0.75, 100);
            await BigImage.FadeTo(1, 100);
            
        }, allowsMultipleExecutions: false);

        public IAsyncCommand HideCommand => new AsyncCommand(async () =>
        {
            await BigImage.FadeTo(0, 250);

            BigImage.IsVisible = false;

            // dispose
            BigImage.Source = null;
            BigImage.CloseCommand = null;
            BigImage.OkCommand = null;

            BackgroundColor = Color.Transparent;

            MainLayout.Opacity = 1;
            cameraPreview.Opacity = 1;

            MainLayout.IsEnabled = true;
        }, allowsMultipleExecutions: false);

        public IAsyncCommand OkCommand => new AsyncCommand(async () =>
        {
            var source = BigImage.Source;
            var path = BigImage.ClassId;

            await Navigation.PopModalAsync();
            Result.TrySetResult(path);
        }, allowsMultipleExecutions: false);

        public IAsyncCommand OpenGalleryCommand => new AsyncCommand(async () =>
        {
            var mediaFile = await MediaPicker.PickPhotoAsync();

            if (mediaFile != null)
            {
                //mediaFile.FullPath
                var stream = await mediaFile.OpenReadAsync();
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);

                    cameraPreview_MediaCaptured(null, new MediaCapturedEventArgs(path: mediaFile.FullPath, imageData: memoryStream.ToArray()));
                }
            }

        }, allowsMultipleExecutions: false);

        private TaskCompletionSource<string> Result;
        public CameraViewPage(TaskCompletionSource<string> result, MediaOptions mediaOptions)
        {
            InitializeComponent();

            BindingContext = this;

            Result = result;
            BigImage.IsVisible = false;
            cameraPreview.MediaOptions = mediaOptions;
        }

        protected override bool OnBackButtonPressed()
            => true;

        void SetEnabledButtons(bool isEnabled)
        {
            btnTakePhoto.IsEnabled = isEnabled;
            btnFlash.IsEnabled = isEnabled;
            btnClose.IsEnabled = isEnabled;
            btnSwitchCam.IsEnabled = isEnabled;
        }

        void cameraPreview_MediaCaptured(object sender, MediaCapturedEventArgs e)
        {
            Debug.WriteLine("Data: " + e.ImageData?.Length);
            Debug.WriteLine("Path: " + e.Path);

            
            Device.BeginInvokeOnMainThread(async () =>
            {
                SetEnabledButtons(true);

                var isPreviewPhoto = await DisplayAlert("Test", "Mau liat hasil foto? ", "ya", "tidak");

                await Navigation.PopModalAsync();

                if(isPreviewPhoto)
                {
                    var navigation = Application.Current.MainPage.Navigation;
                    await navigation.PushAsync(new ImagePreviewPage(e.Image));
                }

                Result.TrySetResult(e.Path);
            });
        }

        async void cameraPreview_MediaCaptureFailed(object sender, string e)
        {
            await Navigation.PopAsync();
            Result.SetException(new Exception(e));
        }

        public class ImagePreview
        {
            public ImageSource Source { get; set; }
            public string Uri { get; set; }

            public ImagePreview(ImageSource source, string uri)
            {
                Source = source;
                Uri = uri;
            }
        }

        public enum CameraMode
        {
            TakePhoto,
            TakeAndPickPhoto
        }

    }
}

