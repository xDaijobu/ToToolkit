using System;
using System.Linq;

namespace ToToolkit.Views
{
    public class MediaOptions
    {
        public MediaOptions()
        {

        }

        public MediaOptions(Xamarin.Essentials.Placemark placemarkEssentials)
        {
            if (placemarkEssentials is null)
                throw new NullReferenceException(nameof(placemarkEssentials));

            Placemark = new Placemark()
            {
                Location = new Location()
                {
                    Latitude = placemarkEssentials.Location.Latitude,
                    Longitude = placemarkEssentials.Location.Longitude,
                },
                SubLocality = placemarkEssentials.SubLocality,
                Locality = placemarkEssentials.Locality,
                SubAdminArea = placemarkEssentials.SubAdminArea,
                AdminArea = placemarkEssentials.AdminArea,
                PostalCode = placemarkEssentials.PostalCode,
                CountryName = placemarkEssentials.CountryName,
                Thoroughfare = placemarkEssentials.Thoroughfare,
                FeatureName = placemarkEssentials.FeatureName,
            };
        }

        public string Directory { get; set; }
        public string Name { get; set; }

        public int? MaxWidthHeight { get; set; }

        /// <summary>
        /// Gets or sets the size of the photo.
        /// </summary>
        /// <value>The size of the photo.</value>
        public PhotoSize PhotoSize { get; set; } = PhotoSize.Full;

        int customPhotoSize = 100;
        /// <summary>
        /// The custom photo size to use, 100 full size (same as Full),
        /// and 1 being smallest size at 1% of original
        /// Default is 100
        /// </summary>
        public int CustomPhotoSize
        {
            get { return customPhotoSize; }
            set
            {
                if (value > 100)
                    customPhotoSize = 100;
                else if (value < 1)
                    customPhotoSize = 1;
                else
                    customPhotoSize = value;
            }
        }

        int quality = 100;
        /// <summary>
        /// The compression quality to use, 0 is the maximum compression (worse quality),
        /// and 100 minimum compression (best quality)
        /// Default is 100
        /// </summary>
        public int CompressionQuality
        {
            get { return quality; }
            set
            {
                if (value > 100)
                    quality = 100;
                else if (value < 0)
                    quality = 0;
                else
                    quality = value;
            }
        }

        public Placemark Placemark { get; set; }
    }

    public class Location
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class Placemark
    {
        /// <summary>Gets or sets the location of the placemark.</summary>
        /// <value>The location of the placemark.</value>
        /// <remarks />
        public Location Location { get; set; }

        /// <summary>Gets or sets the country name.</summary>
        /// <value>The country name.</value>
        /// <remarks />
        public string CountryName { get; set; }

        /// <summary>Gets or sets the sub-administrative area name of the address, for example, "Santa Clara County", or null if it is unknown.</summary>
        /// <value>The sub-admin area.</value>
        /// <remarks />
        public string SubAdminArea { get; set; }

        /// <summary>Gets or sets the administrative area name of the address, for example, "CA", or null if it is unknown.</summary>
        /// <value>The admin area.</value>
        /// <remarks />
        public string AdminArea { get; set; }

        /// <summary>Gets or sets the postal code.</summary>
        /// <value>The postal code.</value>
        /// <remarks />
        public string PostalCode { get; set; }

        /// <summary>Gets or sets the sub locality.</summary>
        /// <value>The sub locality.</value>
        /// <remarks />
        public string SubLocality { get; set; }

        /// <summary>Gets or sets the city or town.</summary>
        /// <value>The city or town of the locality.</value>
        /// <remarks />
        public string Locality { get; set; }

        public string Thoroughfare { get; set; }

        public string FeatureName { get; set; }
        

        public override string ToString()
        {
            string[] strings = null;

#if __IOS__
            strings = new string[]
            {
                $"Lat: {Location?.Latitude}",
                $"Lon: {Location?.Longitude}",
                Locality,
                Thoroughfare,
                FeatureName,
                SubAdminArea,
                AdminArea,
                PostalCode,
                CountryName,
            };
#elif __ANDROID__
            strings = new string[]
            {
                $"Lat: {Location?.Latitude}",
                $"Lon: {Location?.Longitude}",
                SubLocality,
                Locality,
                SubAdminArea,
                AdminArea,
                PostalCode,
                CountryName,
            };
#endif

            return string.Join("\n", strings?.Where(text => !string.IsNullOrEmpty(text)));
        }
    }
}
