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
    [RegularExpression(@"^[a-zA-Z0-9]{5,20}$", ErrorMessage = "NIC must be between 5 and 20 alphanumeric characters.")]
    public string Nic { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 120 characters.")]
    [RegularExpression(@"^(?!\s*$).+", ErrorMessage = "Full name cannot be blank or contain only whitespace.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
    [StringLength(120, ErrorMessage = "Email address cannot exceed 120 characters.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Phone must be a valid telephone number.")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
    public string? Phone { get; set; }

    [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
    public string? Address { get; set; }

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; } = string.Empty;
}

public class CreateStaffUserRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 120 characters.")]
    [RegularExpression(@"^(?!\s*$).+", ErrorMessage = "Full name cannot be blank or contain only whitespace.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email must be a valid email address.")]
    [StringLength(120, ErrorMessage = "Email address cannot exceed 120 characters.")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Phone must be a valid telephone number.")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
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

/// <summary>
/// Editable profile fields. Role and profile info can be updated by authorized staff.
/// </summary>
public class UpdateUserRequest
{
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 120 characters.")]
    [RegularExpression(@"^(?!\s*$).+", ErrorMessage = "Full name cannot be blank or contain only whitespace.")]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Phone must be a valid telephone number.")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
    public string? Phone { get; set; }

    [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters.")]
    public string? Address { get; set; }

    public string? Role { get; set; }
}
