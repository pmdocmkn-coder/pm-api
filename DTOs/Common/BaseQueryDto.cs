using System.ComponentModel.DataAnnotations;

namespace Pm.DTOs.Common
{
    public abstract class BaseQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public string? SortBy { get; set; }
        public string? SortDir { get; set; } = "desc";

        // UBAH DARI abstract MENJADI virtual DENGAN DEFAULT KOSONG
        protected virtual string[] AllowedSortFields => Array.Empty<string>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrWhiteSpace(SortBy))
            {
                // Kalau AllowedSortFields kosong (default), kita anggap semua sort diperbolehkan
                // atau bisa langsung reject semua kalau mau ketat
                if (AllowedSortFields.Length > 0 &&
                    !AllowedSortFields.Contains(SortBy, StringComparer.OrdinalIgnoreCase))
                {
                    yield return new ValidationResult(
                        $"Invalid sort field '{SortBy}'. Allowed: {string.Join(", ", AllowedSortFields)}",
                        new[] { nameof(SortBy) });
                }
            }

            if (!string.IsNullOrWhiteSpace(SortDir) &&
                !string.Equals(SortDir, "asc", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(SortDir, "desc", StringComparison.OrdinalIgnoreCase))
            {
                yield return new ValidationResult(
                    "SortDir must be either 'asc' or 'desc'.",
                    new[] { nameof(SortDir) });
            }
        }
    }
}