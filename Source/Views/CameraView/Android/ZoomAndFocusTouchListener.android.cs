using Android.Views;
using AndroidX.Camera.Core;

namespace ToToolkit.Views
{
    public class ZoomAndFocusTouchListener : Java.Lang.Object, View.IOnTouchListener
    {
        private readonly ScaleGestureDetector _scaleGestureDetector;
        private readonly MeteringPointFactory _meteringPointFactory;
        private readonly ICameraControl _cameraControl;

        public ZoomAndFocusTouchListener(ScaleGestureDetector scaleGestureDetector, MeteringPointFactory meteringPointFactory, ICameraControl cameraControl)
        {
            _scaleGestureDetector = scaleGestureDetector;
            _meteringPointFactory = meteringPointFactory;
            _cameraControl = cameraControl;
        }

        public bool OnTouch(View v, MotionEvent e)
        {
            // Scale Touch
            _scaleGestureDetector.OnTouchEvent(e);

            // Focus Touch
            switch (e.Action)
            {
                case MotionEventActions.Down:
                    //case MotionEventActions.Up:
                    {
                        //System.Diagnostics.Debug.WriteLine("Focus Touch");

                        // Convert UI coordinates into camera sensor coordinates
                        var point = _meteringPointFactory.CreatePoint(e.GetX(), e.GetY());

                        // Prepare focus action to be triggered
                        var action = new FocusMeteringAction.Builder(point).SetAutoCancelDuration(5, Java.Util.Concurrent.TimeUnit.Seconds).Build();

                        // Execute focus action
                        _cameraControl.StartFocusAndMetering(action);
                        break;
                    }
            }

            return true;
        }
    }
}

