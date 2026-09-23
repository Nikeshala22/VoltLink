
using System.Security.Claims;
using VoltLink.Api.Middleware;
using VoltLink.Api.Models;

namespace VoltLink.Api.Security;


public static class ClaimsPrincipalExtensions
{
    
    public static string GetUserId(this ClaimsPrincipal principal)
    {
       
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                 ?? principal.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ForbiddenException("The access token does not identify a user.");
        }

        return id;
    }

    public static string GetRole(this ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
    }

  
    public static bool IsStaff(this ClaimsPrincipal principal)
    {
        var role = principal.GetRole();
        return role == UserRoles.Backoffice || role == UserRoles.GridOperator;
    }

 
    public static void EnsureOwnerOrStaff(this ClaimsPrincipal principal, string ownerNic)
    {
        if (principal.IsStaff())
        {
            return;
        }

        if (!string.Equals(principal.GetUserId(), ownerNic, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenException("You may only access your own records.");
        }
    }
}
