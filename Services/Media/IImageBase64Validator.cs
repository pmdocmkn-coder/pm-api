using Pm.Enums;

namespace Pm.Services.Media
{
    public interface IImageBase64Validator
    {
        void Validate(string? base64, StoredImageKind kind, string fieldName);
        void ValidateRequired(string? base64, StoredImageKind kind, string fieldName);
        void ValidatePhotoList(IReadOnlyList<string> photos, string fieldName, int maxCount = 5);
    }
}
