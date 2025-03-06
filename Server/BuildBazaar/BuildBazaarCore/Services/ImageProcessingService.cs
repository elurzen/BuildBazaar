using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace BuildBazaarCore.Services
{
    public interface IImageProcessingService
    {
        Task<Stream> StripExifData(IFormFile file);
        Task<Stream> ResizeAndStripExif(IFormFile file, int maxWidth = 0, int maxHeight = 0, bool preserveAspectRatio = true);
    }

    public class ImageProcessingService : IImageProcessingService
    {
        public async Task<Stream> StripExifData(IFormFile file)
        {
            using (var fileStream = file.OpenReadStream())
            {

                
                using var image = await Image.LoadAsync(fileStream);
                
                // Create a new memory stream for the processed image
                var outputStream = new MemoryStream();

                // Get the original format
                IImageFormat format = image.Metadata.DecodedImageFormat;

                // If format couldn't be determined, default to PNG
                if (format == null)
                {
                    format = SixLabors.ImageSharp.Formats.Png.PngFormat.Instance;
                }

                // Create an appropriate encoder based on the format
                IImageEncoder encoder;

                if (format is PngFormat)
                {
                    encoder = new PngEncoder { SkipMetadata = true };
                }
                else if (format is JpegFormat)
                {
                    encoder = new JpegEncoder { SkipMetadata = true };
                }
                else if (format is GifFormat)
                {
                    encoder = new GifEncoder { SkipMetadata = true };
                }
                else if (format is BmpFormat)
                {
                    encoder = new BmpEncoder { SkipMetadata = true };
                }
                else if (format is WebpFormat)
                {
                    encoder = new WebpEncoder { SkipMetadata = true };
                }
                else
                {
                    // Fallback to PNG if format is unknown
                    encoder = new PngEncoder { SkipMetadata = true };
                }


                // Save the image to the new stream without EXIF data
                await image.SaveAsync(outputStream, encoder);

                // Reset the position of the output stream
                outputStream.Position = 0;

                return outputStream;
            }
        }

        public async Task<Stream> ResizeAndStripExif(IFormFile file, int maxWidth = 0, int maxHeight = 0, bool preserveAspectRatio = true)
        {

            using (var fileStream = file.OpenReadStream())
            {

                // Load the image using ImageSharp (this will not preserve EXIF data)
                using var image = await Image.LoadAsync(fileStream);

                // Create a new memory stream for the processed image
                var outputStream = new MemoryStream();

                // Get the original format
                IImageFormat format = image.Metadata.DecodedImageFormat;

                // If format couldn't be determined, default to PNG
                if (format == null)
                {
                    format = SixLabors.ImageSharp.Formats.Png.PngFormat.Instance;
                }


                int newWidth = maxWidth;
                int newHeight = maxHeight;

                if (preserveAspectRatio && maxWidth > 0 && maxHeight > 0)
                {
                    double widthRatio = (double)maxWidth / image.Width;
                    double heightRatio = (double)maxHeight / image.Height;
                    double ratio = Math.Min(widthRatio, heightRatio);
                    newWidth = (int)(image.Width * ratio);
                    newHeight = (int)(image.Height * ratio);
                }
                else if (preserveAspectRatio)
                {
                    if (maxWidth > 0 && maxHeight == 0)
                    {
                        double ratio = (double)maxWidth / image.Width;
                        newHeight = (int)(image.Height * ratio);
                    }
                    else if (maxHeight > 0 && maxWidth == 0)
                    {
                        double ratio = (double)maxHeight / image.Height;
                        newWidth = (int)(image.Width * ratio);
                    }
                    else if (maxWidth == 0 && maxHeight == 0)
                    {
                        newWidth = image.Width;
                        newHeight = image.Height;
                    }
                }

                // Only resize if dimensions are different
                if (newWidth != image.Width || newHeight != image.Height)
                {
                    image.Mutate(x => x.Resize(newWidth, newHeight));
                }

                // Create an appropriate encoder based on the format
                IImageEncoder encoder;

                if (format is PngFormat)
                {
                    encoder = new PngEncoder { SkipMetadata = true };
                }
                else if (format is JpegFormat)
                {
                    encoder = new JpegEncoder { SkipMetadata = true };
                }
                else if (format is GifFormat)
                {
                    encoder = new GifEncoder { SkipMetadata = true };
                }
                else if (format is BmpFormat)
                {
                    encoder = new BmpEncoder { SkipMetadata = true };
                }
                else if (format is WebpFormat)
                {
                    encoder = new WebpEncoder { SkipMetadata = true };
                }
                else
                {
                    // Fallback to PNG if format is unknown
                    encoder = new PngEncoder { SkipMetadata = true };
                }


                // Save the image to the new stream without EXIF data
                await image.SaveAsync(outputStream, encoder);

                // Reset the position of the output stream
                outputStream.Position = 0;

                return outputStream;
            }
        }
    }
}