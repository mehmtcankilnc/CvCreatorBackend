namespace CvCreator.Application.Contracts;

public interface IUserService
{
    Task<bool> DeleteUserFromSupabase(string userId);
    Task DeleteRelatedData(Guid userId);
}
