using System;
using System.Linq;
using System.Threading.Tasks;
using ToToolkit.Views;
using ToToolkitSample.Pages.Views;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace ToToolkitSample
{
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();
		}

        async void Button_Clicked(object sender, EventArgs e)
        {
            var appName = AppInfo.Name;
            var packageName = AppInfo.PackageName;
            TaskCompletionSource<string> result = new TaskCompletionSource<string>();
            MediaOptions mediaOptions;

            var location = await Geolocation.GetLocationAsync();
            var placemarks = await Geocoding.GetPlacemarksAsync(location);

            var placemark = placemarks.FirstOrDefault();

            mediaOptions = new MediaOptions(placemark)
            {
                CompressionQuality = 75, /*SATO SAMA HRTO PAKAINYA 50*/
                PhotoSize = PhotoSize.Small,
                //Directory = $"/storage/emulated/0/Android/data/{packageName}/files/Pictures/{appName}",
                MaxWidthHeight = 2000,
            };


            await Navigation.PushModalAsync(new CameraViewPage(result, mediaOptions)
            {
                Mode = CameraViewPage.CameraMode.TakeAndPickPhoto
            });
            var path = await result.Task;

            if (!string.IsNullOrEmpty(path))
                await DisplayAlert("Sukses", "Path: " + path, "Ok");
        }
    }
}