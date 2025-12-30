using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace Pm.Services
{
    public interface ICloudinaryService
    {
        Task<string?> UploadImageAsync(IFormFile file, string folder = "profile", bool resize = false, int maxWidth = 1920, int maxHeight = 1080);
        Task<bool> DeleteImageAsync(string publicId);
        string? GetPublicIdFromUrl(string imageUrl);
    }

    public class CloudinarySettings
    {
        public string CloudName { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
    }

    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;

        public CloudinaryService(IOptions<CloudinarySettings> config, ILogger<CloudinaryService> logger)
        {
            _logger = logger;

            var account = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }

        public async Task<string?> UploadImageAsync(IFormFile file, string folder = "profile", bool resize = false, int maxWidth = 1920, int maxHeight = 1080)
        {
            if (file == null || file.Length == 0)
                return null;

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/jpg", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                throw new Exception("Invalid file type. Only JPEG, PNG, JPG, GIF, and WEBP are allowed.");
            }

            // Validate file size (max 10MB)
            if (file.Length > 10 * 1024 * 1024)
            {
                throw new Exception("File size must not exceed 10MB.");
            }

            try
            {
                Stream uploadStream;

                // ✅ RESIZE IMAGE IF REQUESTED
                if (resize)
                {
                    _logger.LogInformation("🖼️ Resizing image: {FileName}", file.FileName);
                    
                    using var originalStream = file.OpenReadStream();
                    using var image = await Image.LoadAsync(originalStream);
                    
                    var originalWidth = image.Width;
                    var originalHeight = image.Height;
                    
                    _logger.LogInformation("📏 Original size: {Width}x{Height}", originalWidth, originalHeight);

                    // Only resize if image is larger than max dimensions
                    if (originalWidth > maxWidth || originalHeight > maxHeight)
                    {
                        // Calculate aspect ratio
                        var ratioX = (double)maxWidth / originalWidth;
                        var ratioY = (double)maxHeight / originalHeight;
                        var ratio = Math.Min(ratioX, ratioY);

                        var newWidth = (int)(originalWidth * ratio);
                        var newHeight = (int)(originalHeight * ratio);

                        _logger.LogInformation("🔄 Resizing to: {Width}x{Height}", newWidth, newHeight);

                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new SixLabors.ImageSharp.Size(newWidth, newHeight),
                            Mode = ResizeMode.Max,
                            Sampler = KnownResamplers.Lanczos3 // High quality resampling
                        }));

                        var resizedStream = new MemoryStream();
                        await image.SaveAsync(resizedStream, new JpegEncoder { Quality = 85 });
                        resizedStream.Position = 0;
                        uploadStream = resizedStream;
                        
                        _logger.LogInformation("✅ Resized successfully: {Size} KB", resizedStream.Length / 1024);
                    }
                    else
                    {
                        _logger.LogInformation("ℹ️ Image already within limits, uploading original");
                        uploadStream = file.OpenReadStream();
                    }
                }
                else
                {
                    uploadStream = file.OpenReadStream();
                }

                // ✅ UPLOAD TO CLOUDINARY WITHOUT TRANSFORMATION
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, uploadStream),
                    Folder = $"pm-app/{folder}",
                    PublicId = $"{Guid.NewGuid()}",
                    // ❌ REMOVE TRANSFORMATION - Let image keep original dimensions
                    // Transformation = new Transformation()
                    //     .Width(500).Height(500).Crop("fill")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                // Cleanup stream if it was resized
                if (resize && uploadStream != file.OpenReadStream())
                {
                    await uploadStream.DisposeAsync();
                }

                if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    _logger.LogInformation("✅ Image uploaded successfully: {PublicId} ({Width}x{Height})", 
                        uploadResult.PublicId, uploadResult.Width, uploadResult.Height);
                    return uploadResult.SecureUrl.ToString();
                }

                _logger.LogError("Failed to upload image to Cloudinary: {Error}", uploadResult.Error?.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image to Cloudinary");
                throw;
            }
        }

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId))
                return false;

            try
            {
                var deleteParams = new DeletionParams(publicId)
                {
                    ResourceType = ResourceType.Image
                };

                var result = await _cloudinary.DestroyAsync(deleteParams);

                if (result.Result == "ok")
                {
                    _logger.LogInformation("Image deleted successfully from Cloudinary: {PublicId}", publicId);
                    return true;
                }

                _logger.LogWarning("Failed to delete image from Cloudinary: {PublicId}, Result: {Result}", publicId, result.Result);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image from Cloudinary: {PublicId}", publicId);
                return false;
            }
        }

        public string? GetPublicIdFromUrl(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return null;

            try
            {
                // Extract public ID from Cloudinary URL
                // Example: https://res.cloudinary.com/dz3rhkitn/image/upload/v1234567890/pm-app/profile/guid.jpg
                var uri = new Uri(imageUrl);
                var segments = uri.AbsolutePath.Split('/');

                // Find the index of "upload" and get everything after version
                var uploadIndex = Array.IndexOf(segments, "upload");
                if (uploadIndex >= 0 && uploadIndex + 2 < segments.Length)
                {
                    // Join all segments after version (excluding file extension)
                    var publicIdParts = segments.Skip(uploadIndex + 2).ToArray();
                    var lastPart = publicIdParts[publicIdParts.Length - 1];
                    var extensionIndex = lastPart.LastIndexOf('.');
                    if (extensionIndex > 0)
                    {
                        publicIdParts[publicIdParts.Length - 1] = lastPart.Substring(0, extensionIndex);
                    }

                    return string.Join("/", publicIdParts);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting public ID from URL: {Url}", imageUrl);
            }

            return null;
        }
    }
}