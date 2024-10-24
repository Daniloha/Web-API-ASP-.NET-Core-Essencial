using Microsoft.AspNetCore.Identity;

namespace APICatalogo.Models
{
    public class AplicationUser : IdentityUser
    {
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
    }
}
