using Xamarin.Forms;

namespace ToToolkitSample.Pages.Views
{	
	public partial class ImagePreviewPage : ContentPage
	{	
		public ImagePreviewPage(ImageSource imageSource)
		{
			InitializeComponent();

			imgView.Source = imageSource;
		}
	}
}

