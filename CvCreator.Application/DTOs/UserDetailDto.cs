namespace CvCreator.Application.DTOs;

public record UserDetailDto(
    string Id,
    string Email,
    string UserName,
    bool IsGuest
);
