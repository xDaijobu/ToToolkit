using UIKit;

namespace ToToolkit.Views
{
    public static class UIViewExtensions
    {
		public static UIImage ToUIImage(this UIView view)
		{
			UIGraphics.BeginImageContextWithOptions(view.Bounds.Size, false, 0);
			view.Layer.RenderInContext(UIGraphics.GetCurrentContext());
			var img = UIGraphics.GetImageFromCurrentImageContext();
			UIGraphics.EndImageContext();
			return img;
		}
	}
}
