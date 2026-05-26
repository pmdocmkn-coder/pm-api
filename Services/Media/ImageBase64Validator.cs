using System.Text.RegularExpressions;
using Pm.Enums;

namespace Pm.Services.Media
{
    public class ImageBase64Validator : IImageBase64Validator
    {
        private static readonly Regex DataUriRegex = new(
            @"^data:image/(jpeg|jpg|png);base64,(.+)$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private const int MaxRadioPhotoBytes = 400 * 1024;
        private const int MaxSignatureBytes = 80 * 1024;

        public void ValidateRequired(string? base64, StoredImageKind kind, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(base64))
                throw new ArgumentException($"{fieldName} wajib diisi.");
            Validate(base64, kind, fieldName);
        }

        public void Validate(string? base64, StoredImageKind kind, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(base64)) return;

            var match = DataUriRegex.Match(base64.Trim());
            if (!match.Success)
                throw new ArgumentException($"{fieldName}: format harus data URI image/jpeg atau image/png.");

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(match.Groups[2].Value);
            }
            catch
            {
                throw new ArgumentException($"{fieldName}: base64 tidak valid.");
            }

            var max = kind == StoredImageKind.RadioPhoto ? MaxRadioPhotoBytes : MaxSignatureBytes;
            if (bytes.Length > max)
                throw new ArgumentException($"{fieldName}: ukuran gambar terlalu besar (max {max / 1024} KB).");
        }

        public void ValidatePhotoList(IReadOnlyList<string> photos, string fieldName, int maxCount = 5)
        {
            if (photos == null || photos.Count == 0)
                throw new ArgumentException($"{fieldName}: minimal 1 foto wajib diisi.");
            if (photos.Count > maxCount)
                throw new ArgumentException($"{fieldName}: maksimal {maxCount} foto.");

            for (var i = 0; i < photos.Count; i++)
                ValidateRequired(photos[i], StoredImageKind.RadioPhoto, $"{fieldName}[{i + 1}]");
        }
    }
}
