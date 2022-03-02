using System.Windows.Input;
using Xamarin.CommunityToolkit.Effects;
using Xamarin.Forms;

namespace ToToolkitSample.Controls
{
    public partial class ImagePreview : ContentView
    {
        public static readonly BindableProperty SourceProperty
            = BindableProperty.Create(nameof(Source), typeof(ImageSource), typeof(ImagePreview), null, BindingMode.OneWay, propertyChanged: OnSourceChanged);

        public static readonly BindableProperty CloseCommandProperty
            = BindableProperty.Create(nameof(CloseCommand), typeof(ICommand), typeof(ImagePreview), null, BindingMode.OneWay, propertyChanged: CloseCommandChanged);

        public static readonly BindableProperty OkCommandProperty
            = BindableProperty.Create(nameof(OkCommand), typeof(ICommand), typeof(ImagePreview), null, BindingMode.OneWay, propertyChanged: OkCommandChanged);

        public ImageSource Source
        {
            get { return (ImageSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public ICommand CloseCommand
        {
            get { return (ICommand)GetValue(CloseCommandProperty); }
            set { SetValue(CloseCommandProperty, value); }
        }

        public ICommand OkCommand
        {
            get { return (ICommand)GetValue(OkCommandProperty); }
            set { SetValue(OkCommandProperty, value); }
        }

        static void OnSourceChanged(BindableObject bindable, object oldValue, object newValue)
            => (bindable as ImagePreview).imageView.Source = (ImageSource)newValue;

        static void CloseCommandChanged(BindableObject bindable, object oldValue, object newValue)
            => TouchEffect.SetCommand((bindable as ImagePreview).btnClose, (ICommand)newValue);

        static void OkCommandChanged(BindableObject bindable, object oldValue, object newValue)
            => TouchEffect.SetCommand((bindable as ImagePreview).btnOk, (ICommand)newValue);

        public ImagePreview()
        {
            InitializeComponent();
        }
    }
}