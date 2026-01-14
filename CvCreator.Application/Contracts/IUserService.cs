namespace CvCreator.Application.Contracts;

public interface IUserService
{
    Task<bool> DeleteUserAccountAsync(Guid userId);
}
