using System.ComponentModel.DataAnnotations;

namespace VoltLink.Api.Dtos;

public record UserResponse(
    string Id,
    string FullName,
    string Email,
    string? Phone,
    string? Address,
    string Role,
    bool IsActive,
    bool DeactivationRequested,
    DateTime CreatedAtUtc);

public class LoginRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}

public record LoginResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    UserResponse User);

public class RegisterProsumerRequest
{
    [Required(ErrorMessage = "NIC is required.")]
    [StringLength(20, MinimumLength = 5, ErrorMessage = "NIC must be between 5 and 20 characters.")]
    public string Nic { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(120, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Phone must be a valid telephone number.")]
    public string? Phone { get; set; }

    public string? Address { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;
}

public class CreateStaffUserRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(120, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Phone must be a valid telephone number.")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;
}


public class CreateProsumerRequest : RegisterProsumerRequest
{
    
    public bool ActivateImmediately { get; set; } = true;
}


public class UpdateUserRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(120, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Phone must be a valid telephone number.")]
    public string? Phone { get; set; }

    public string? Address { get; set; }
}
