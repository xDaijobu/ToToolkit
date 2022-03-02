using Android.Views;
using AndroidX.Camera.Core;
using AndroidX.Camera.Core.Internal;
using static Android.Views.ScaleGestureDetector;

namespace ToToolkit.Views
{
    public class ScaleGestureListener : SimpleOnScaleGestureListener
    {
        private readonly ICameraControl _cameraControl;
        private readonly ICameraInfo _cameraInfo;

        public ScaleGestureListener(ICameraControl cameraControl, ICameraInfo cameraInfo)
        {
            _cameraControl = cameraControl;
            _cameraInfo = cameraInfo;
        }

        public override bool OnScale(ScaleGestureDetector detector)
        {
            ImmutableZoomState currentState = (ImmutableZoomState)_cameraInfo.ZoomState.Value;
            float currentZoomRatio = currentState.ZoomRatio;

            // get current scale 
            float delta = detector.ScaleFactor;

            // update the camera's zoom ratio
            _cameraControl.SetZoomRatio(currentZoomRatio * delta);
            return true;
        }

        public override bool OnScaleBegin(ScaleGestureDetector detector)
        {
            return base.OnScaleBegin(detector);
        }

        public override void OnScaleEnd(ScaleGestureDetector detector)
        {
            base.OnScaleEnd(detector);
        }
    }
}

