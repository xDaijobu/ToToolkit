using AndroidX.Camera.Core;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using static AndroidX.Camera.Core.ImageCapture;

namespace ToToolkit.Views
{
    public class ImageCapturedCallback : OnImageCapturedCallback
    {
        public TaskCompletionSource<bool> completionSource;
        private readonly Action<ImageCaptureException> onErrorCallback;
        private readonly Action<IImageProxy> onCapturedSuccessCallback;

        public ImageCapturedCallback(TaskCompletionSource<bool> completionSource, Action<IImageProxy> onCapturedSuccessCallback, Action<ImageCaptureException> onErrorCallback)
        {
            this.completionSource = completionSource;
            this.onCapturedSuccessCallback = onCapturedSuccessCallback;
            this.onErrorCallback = onErrorCallback;
        }

        public override void OnError(ImageCaptureException exc)
        {
            onErrorCallback?.Invoke(exc);
        }

        public override void OnCaptureSuccess(IImageProxy image)
        {
            onCapturedSuccessCallback?.Invoke(image);

            base.OnCaptureSuccess(image);
        }
    }
}

