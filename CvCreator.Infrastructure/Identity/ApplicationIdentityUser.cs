using Microsoft.AspNetCore.Identity;

namespace CvCreator.Infrastructure.Identity;

public class ApplicationIdentityUser : IdentityUser<Guid>
{
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
}
