// -----------------------------------------------------------------------------
// File        : AuthController.cs
// Project     : VoltLink.Api - Smart Solar Microgrid Trading System
// Module      : Controllers
// Description : Authentication endpoints shared by the React web application
//               and the Android application: login, prosumer self service
//               registration and retrieval of the signed in profile.
// Author      : <IT Number - Member Name>
// Created     : 2026-09-03
// -----------------------------------------------------------------------------

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoltLink.Api.Dtos;
using VoltLink.Api.Security;
using VoltLink.Api.Services;

namespace VoltLink.Api.Controllers;

/// <summary>
/// Issues access tokens and creates prosumer accounts.
/// Note that this controller contains no rules of its own: it binds the
/// request, calls the service and returns the result.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAccountService _accounts;

    /// <summary>
    /// Receives the account service from dependency injection.
    /// </summary>
    public AuthController(IAccountService accounts)
    {
        _accounts = accounts;
    }

    /// <summary>
    /// Signs a user in and returns an access token together with their profile,
    /// which the clients use to route to the correct role based home screen.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<LoginResponse>> LoginAsync(
        [FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        // Delegate straight to the service; all credential rules live there.
        var result = await _accounts.LoginAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("verify-2fa")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LoginResponse>> VerifyTwoFactorAsync(
        [FromBody] VerifyTwoFactorRequest request, CancellationToken cancellationToken)
    {
        var result = await _accounts.VerifyTwoFactorAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("resend-2fa")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendTwoFactorAsync(
        [FromBody] ResendTwoFactorRequest request, CancellationToken cancellationToken)
    {
        await _accounts.ResendTwoFactorCodeAsync(request.Email, cancellationToken);
        return Ok(new { message = "If the account exists and 2FA is active, a fresh security code was sent." });
    }

    [HttpPost("register-prosumer")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> RegisterProsumerAsync(
        [FromBody] RegisterProsumerRequest request, CancellationToken cancellationToken)
    {
        var created = await _accounts.RegisterProsumerAsync(request, cancellationToken);

        // 201 with a Location header pointing at the new prosumer resource.
        return CreatedAtAction(
            actionName: nameof(ProsumersController.GetByNicAsync),
            controllerName: "Prosumers",
            routeValues: new { nic = created.Id },
            value: created);
    }

    /// <summary>
    /// Returns the profile of the caller, identified from their access token.
    /// Used by both clients on start-up to restore the session.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        // The identifier comes from the validated token, never from the client,
        // so a caller cannot ask for somebody else's profile here.
        var id = User.GetUserId();
        var profile = await _accounts.GetByIdAsync(id, cancellationToken);

        return Ok(profile);
    }

    [HttpPatch("me/toggle-2fa")]
    [Authorize]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserResponse>> ToggleMyTwoFactorAsync(
        [FromBody] ToggleTwoFactorRequest request, CancellationToken cancellationToken)
    {
        var id = User.GetUserId();
        var updated = await _accounts.ToggleTwoFactorAsync(id, request.Enabled, cancellationToken);
        return Ok(updated);
    }
}
