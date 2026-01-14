using CvCreator.Application.DTOs;

namespace CvCreator.Application.Contracts;

public interface IAuthService
{
    Task<RefreshTokenDto> LoginWithGoogleAsync(GoogleLoginDto dto);
    Task<RefreshTokenDto> LoginAsGuestAsync();
    Task<UserDetailDto> GetUserDetailAsync(Guid userId);
    Task<RefreshTokenDto> RefreshTokenAsync(string accessToken, string refreshToken);
}
