using System;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;

namespace ToToolkit.Views
{
    public class PlacemarkView : View
    {
        private int _height, _width;
        private Placemark _placemark;
        public PlacemarkView(Context context, int height, int width, Placemark placemark) : base(context)
        {
            _height = height;
            _width = width;
            _placemark = placemark;
        }
        public PlacemarkView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }
        public PlacemarkView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            //canvas.Save();
            //canvas.Translate(_height, _width);

            CanvasHelper.DrawPlacemarkView(canvas, _width, _height, _placemark);
        }
    }
}

