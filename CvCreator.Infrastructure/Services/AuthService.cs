using CvCreator.Application.Contracts;
using CvCreator.Application.DTOs;
using CvCreator.Domain.Entities;
using CvCreator.Infrastructure.Identity;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CvCreator.Infrastructure.Services;

public class AuthService(
    UserManager<ApplicationIdentityUser> userManager,
    AppDbContext appDbContext, ITokenService tokenService,
    IConfiguration configuration) : IAuthService
{
    public async Task<RefreshTokenDto> LoginWithGoogleAsync(GoogleLoginDto dto)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = [configuration["Google:ClientId"]!]
            };
            payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken, settings);
        }
        catch
        {
            throw new Exception("Geçersiz Google Token");
        }

        var identityUser = await userManager.FindByEmailAsync(payload.Email);

        if (identityUser == null)
        {
            using var transaction = await appDbContext.Database.BeginTransactionAsync();
            try
            {
                identityUser = new ApplicationIdentityUser
                {
                    UserName = payload.Email,
                    Email = payload.Email,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(identityUser);
                if (!createResult.Succeeded) throw new Exception("Google Kullanıcısı Oluşturulamadı");

                var appUser = new AppUser
                {
                    Id = identityUser.Id,
                    Email = identityUser.Email,
                    UserName = identityUser.UserName,
                    IsGuest = false
                };

                appDbContext.AppUsers.Add(appUser);
                await appDbContext.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        else
        {
            //Kullanıcı var, domain'deki appuser bulup token dönmek için hazırlanmaca
        }

        var user = await appDbContext.AppUsers.FindAsync(identityUser.Id);

        var accessToken = tokenService.CreateToken(user!);
        var refreshToken = tokenService.GenerateRefreshToken();

        identityUser.RefreshToken = refreshToken;
        identityUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await userManager.UpdateAsync(identityUser);

        return new RefreshTokenDto(accessToken, refreshToken);
    }

    public async Task<RefreshTokenDto> LoginAsGuestAsync()
    {
        using var transaction = await appDbContext.Database.BeginTransactionAsync();
        try
        {
            var guestId = Guid.NewGuid();
            var guestEmail = $"{guestId}@guest.cvcreator.com";

            var identityUser = new ApplicationIdentityUser
            {
                Id = guestId,
                UserName = guestEmail,
                Email = guestEmail,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(identityUser);
            if (!createResult.Succeeded) throw new Exception("Misafir Girişi Yapılamadı");

            var appUser = new AppUser
            {
                Id = guestId,
                Email = guestEmail,
                UserName = guestEmail,
                IsGuest = true
            };

            appDbContext.AppUsers.Add(appUser);
            await appDbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            var accessToken = tokenService.CreateToken(appUser);
            var refreshToken = tokenService.GenerateRefreshToken();

            identityUser.RefreshToken = refreshToken;
            identityUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await userManager.UpdateAsync(identityUser);

            return new RefreshTokenDto(accessToken, refreshToken);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<UserDetailDto> GetUserDetailAsync(Guid userId)
    {
        var user = await appDbContext.AppUsers.FindAsync(userId);

        if (user == null) throw new Exception("Kullanıcı Bulunamadı");

        return new UserDetailDto(
            user.Id.ToString(),
            user.Email,
            user.UserName,
            user.IsGuest
        );
    }

    public async Task<RefreshTokenDto> RefreshTokenAsync(string accessToken, string refreshToken)
    {
        var principal = GetPrincipalFromExpiredToken(accessToken);
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null) throw new Exception("Geçersiz Token");

        var identityUser = await userManager.FindByIdAsync(userId);

        if (identityUser == null || identityUser.RefreshToken != refreshToken
            || identityUser.RefreshTokenExpiryTime <= DateTime.UtcNow)
            throw new Exception("Geçersiz veya süresi dolmuş refresh token");

        var appUser = await appDbContext.AppUsers.FindAsync(Guid.Parse(userId));

        var newAccessToken = tokenService.CreateToken(appUser!);
        var newRefreshToken = tokenService.GenerateRefreshToken();

        identityUser.RefreshToken = newRefreshToken;
        identityUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        await userManager.UpdateAsync(identityUser);
        return new RefreshTokenDto(newAccessToken, newRefreshToken);
    }

    private ClaimsPrincipal GetPrincipalFromExpiredToken(string accessToken)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JwtSettings:Secret"]!)),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(accessToken, tokenValidationParameters, out SecurityToken securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken || 
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Geçersiz Token");

        return principal;
    }
}
