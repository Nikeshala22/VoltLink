using VoltLink.Api.Models;

namespace VoltLink.Api.Dtos;


public static class UserMappings
{
  
    public static UserResponse ToResponse(this User user)
    {
        return new UserResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.Phone,
            user.Address,
            user.Role,
            user.IsActive,
            user.DeactivationRequested,
            user.CreatedAtUtc);
    }

   
    public static IReadOnlyList<UserResponse> ToResponseList(this IEnumerable<User> users)
    {
        return users.Select(u => u.ToResponse()).ToList();
    }
}
