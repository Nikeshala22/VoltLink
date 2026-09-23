using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoltLink.Api.Dtos;
using VoltLink.Api.Models;
using VoltLink.Api.Services;

namespace VoltLink.Api.Controllers;


[ApiController]
[Route("api/v1/users")]
[Produces("application/json")]
[Authorize(Policy = Policies.Backoffice)]
public class UsersController : ControllerBase
{
    private readonly IAccountService _accounts;

    public UsersController(IAccountService accounts)
    {
        _accounts = accounts;
    }

   
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> ListAsync(
        [FromQuery] string? role,
        [FromQuery] bool? isActive,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var users = await _accounts.ListAsync(
            role, isActive, deactivationRequested: null, search: search,
            cancellationToken: cancellationToken);

        return Ok(users);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetByIdAsync(
        string id, CancellationToken cancellationToken)
    {
        var user = await _accounts.GetByIdAsync(id, cancellationToken);
        return Ok(user);
    }

    
    [HttpPost]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> CreateAsync(
        [FromBody] CreateStaffUserRequest request, CancellationToken cancellationToken)
    {
        var created = await _accounts.CreateStaffUserAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id }, created);
    }

  
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> UpdateAsync(
        string id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var updated = await _accounts.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }


    [HttpPatch("{id}/activate")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> ActivateAsync(
        string id, CancellationToken cancellationToken)
    {
        var updated = await _accounts.ActivateAsync(id, cancellationToken);
        return Ok(updated);
    }

    
    [HttpPatch("{id}/deactivate")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> DeactivateAsync(
        string id, CancellationToken cancellationToken)
    {
        var updated = await _accounts.DeactivateAsync(id, cancellationToken);
        return Ok(updated);
    }
}
