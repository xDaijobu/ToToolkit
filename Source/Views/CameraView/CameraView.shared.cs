using System;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace ToToolkit.Views
{
    public class CameraView : ContentView
    {
        [Preserve(Conditional = true)]
        public event EventHandler<MediaCapturedEventArgs> MediaCaptured;

        [Preserve(Conditional = true)]
        public event EventHandler<string> MediaCaptureFailed;

        Command cameraClick;

        public static readonly BindableProperty CameraOptionsProperty = BindableProperty.Create(
            propertyName: nameof(CameraOptions),
            returnType: typeof(CameraOptions),
            declaringType: typeof(CameraView),
            defaultValue: CameraOptions.Rear);

        public static readonly BindableProperty FlashModeProperty = BindableProperty.Create(
            propertyName: nameof(FlashMode),
            returnType: typeof(FlashMode),
            declaringType: typeof(CameraView),
            defaultValue: FlashMode.Off);

        public static readonly BindableProperty MediaOptionsProperty = BindableProperty.Create(
            propertyName: nameof(MediaOptions),
            returnType: typeof(MediaOptions),
            declaringType: typeof(CameraView),
            defaultValue: default(MediaOptions));

        public CameraOptions CameraOptions
        {
            get { return (CameraOptions)GetValue(CameraOptionsProperty); }
            set { SetValue(CameraOptionsProperty, value); }
        }

        public FlashMode FlashMode
        {
            get { return (FlashMode)GetValue(FlashModeProperty); }
            set { SetValue(FlashModeProperty, value); }
        }

        public MediaOptions MediaOptions
        {
            get { return (MediaOptions)GetValue(MediaOptionsProperty); }
            set { SetValue(MediaOptionsProperty, value); }
        }

        public Command CameraClick
        {
            get { return cameraClick; }
            set { cameraClick = value; }
        }

        public void RaiseMediaCaptured(MediaCapturedEventArgs args)
        {
            MediaCaptured?.Invoke(this, args);
        }

        public void RaiseMediaCaptureFailed(string message)
        {
            MediaCaptureFailed?.Invoke(this, message);
        }
    }
}
