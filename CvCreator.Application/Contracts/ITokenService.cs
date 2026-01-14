using CvCreator.Domain.Entities;

namespace CvCreator.Application.Contracts;

public interface ITokenService
{
    string CreateToken(AppUser user);
    string GenerateRefreshToken();
}
