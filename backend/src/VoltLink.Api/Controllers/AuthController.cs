using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoltLink.Api.Dtos;
using VoltLink.Api.Security;
using VoltLink.Api.Services;

namespace VoltLink.Api.Controllers;


[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAccountService _accounts;

 
    public AuthController(IAccountService accounts)
    {
        _accounts = accounts;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LoginResponse>> LoginAsync(
        [FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        
        var result = await _accounts.LoginAsync(request, cancellationToken);
        return Ok(result);
    }

  
    [HttpPost("register-prosumer")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> RegisterProsumerAsync(
        [FromBody] RegisterProsumerRequest request, CancellationToken cancellationToken)
    {
        var created = await _accounts.RegisterProsumerAsync(request, cancellationToken);

        
        return CreatedAtAction(
            actionName: nameof(ProsumersController.GetByNicAsync),
            controllerName: "Prosumers",
            routeValues: new { nic = created.Id },
            value: created);
    }

   
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        
        var id = User.GetUserId();
        var profile = await _accounts.GetByIdAsync(id, cancellationToken);

        return Ok(profile);
    }
}
