using CvCreator.Application.Contracts;
using CvCreator.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CvCreator.Infrastructure.Services;

public class UserService(
    AppDbContext appDbContext, IFileService fileService,
    UserManager<ApplicationIdentityUser> userManager) : IUserService
{
    private readonly AppDbContext _appDbContext = appDbContext;
    private readonly IFileService _fileService = fileService;

    public async Task<bool> DeleteUserAccountAsync(Guid userId)
    {
        var identityUser = await userManager.FindByIdAsync(userId.ToString());
        if (identityUser == null) return false;

        var appUser = await _appDbContext.AppUsers
            .Include(u => u.Resumes)
            .Include(u => u.CoverLetters)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (appUser != null)
        {
            if (appUser.Resumes != null)
            {
                foreach (var resume in appUser.Resumes)
                {
                    if (!string.IsNullOrEmpty(resume.StoragePath))
                    {
                        _fileService.DeleteFile(resume.StoragePath);
                    }
                }
            }

            if (appUser.CoverLetters != null)
            {
                foreach (var coverLetter in appUser.CoverLetters)
                {
                    if (!string.IsNullOrEmpty(coverLetter.StoragePath))
                    {
                        _fileService.DeleteFile(coverLetter.StoragePath);
                    }
                }
            }

            _appDbContext.AppUsers.Remove(appUser);
            await _appDbContext.SaveChangesAsync();
        }

        var result = await userManager.DeleteAsync(identityUser);

        return result.Succeeded;
    }
}
