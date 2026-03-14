using System.ComponentModel.DataAnnotations;

namespace JobHandling.Application.DTOs
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Username must be 3-100 characters")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must be 6-255 characters")]
        public string Password { get; set; }
    }
}