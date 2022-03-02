using System;
using Android.Graphics;

namespace ToToolkit.Views
{
    public class CanvasHelper
    {
        public static void DrawPlacemarkView(Canvas canvas, float width, float height, Placemark placemark, bool isVisibleCreated = false)
        {
            //contoh datanya
            //SubLocality = "Pegangsaan Dua",
            //Locality = "Kecamatan Kelapa Gading",
            //SubAdminArea = "Kota Jakarta Utara",
            //AdminArea = "Daerah Khusus Ibukota Jakarta",
            //PostalCode = "14250",
            //CountryName = "Indonesia",

            if (placemark is null)
                return;

            string text = placemark.ToString();

            float y = height / 2;
            Paint paint = new Paint
            {
                Color = Color.White,
                AntiAlias = true,
                TextAlign = Paint.Align.Right
            };
            //paint.SetTypeface(Typeface.DefaultBold);

            // set text size for width
            float testTextSize = 45f;

            // Get the bounds of the text, using our testTextSize.  
            paint.TextSize = testTextSize;
            Rect bounds = new Rect();
            paint.GetTextBounds(text, 0, text.Length, bounds);

            // Calculate the desired size as a proportion of our testTextSize.
            float desiredTextSize = (float)(testTextSize * width / (bounds.Width() * 0.75) * 1.25);

            // Set the paint for that size.
            //System.Diagnostics.Debug.WriteLine("Desired Text Size: " + desiredTextSize);
            paint.TextSize = desiredTextSize;
            //System.Diagnostics.Debug.WriteLine("first y: " + y);
            string[] array = text.Split("\n");
            for (int i = 0; i < array.Length; i++)
            {
                string line = array[i];

                if (isVisibleCreated && i == 0)
                {
                    string createdDateTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss", new System.Globalization.CultureInfo("en-US"));
                    canvas.DrawText(createdDateTime, (float)(width * 0.99), y, paint);
                    y += paint.Descent() - paint.Ascent();
                }

                canvas.DrawText(line, (float)(width * 0.99), y, paint);
                y += paint.Descent() - paint.Ascent();

                //System.Diagnostics.Debug.WriteLine("Paint.Descent: " + paint.Descent());
                //System.Diagnostics.Debug.WriteLine("Paint.Ascent:" + paint.Ascent());
                //System.Diagnostics.Debug.WriteLine("y: " + y);
            }
        }
    }
}
