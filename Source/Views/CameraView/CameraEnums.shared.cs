namespace ToToolkit.Views
{
    public enum CameraOptions
    {
        Rear,
        Front
    }

    public enum FlashMode
    {
        Off,
        On,
        Auto,
    }

    /// <summary>
    /// Photo size enum.
    /// </summary>
    public enum PhotoSize
    {
        /// <summary>
        /// 25% of original
        /// </summary>
        Small,
        /// <summary>
        /// 50% of the original
        /// </summary>
        Medium,
        /// <summary>
        /// 75% of the original
        /// </summary>
        Large,
        /// <summary>
        /// Untouched
        /// </summary>
        Full,
        /// <summary>
        /// Custom size between 1-100
        /// Must set the CustomPhotoSize value
        /// Only applies to iOS and Android
        /// Windows will auto configure back to small, medium, large, and full
        /// </summary>
        Custom,
        /// <summary>
        /// Use the Max Width or Height photo size.
        /// The property ManualSize must be set to a value. The MaxWidthHeight will be the max width or height of the image
        /// Currently this works on iOS and Android only.
        /// On Windows the PhotoSize will fall back to Full
        /// </summary>
        MaxWidthHeight
    }
}
