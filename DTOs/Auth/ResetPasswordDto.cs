using System.ComponentModel.DataAnnotations;

namespace Pm.DTOs.Auth
{
    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "Token wajib diisi")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password baru wajib diisi")]
        [MinLength(8, ErrorMessage = "Password minimal 8 karakter")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
