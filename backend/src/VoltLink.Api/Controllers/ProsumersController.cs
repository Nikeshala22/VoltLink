using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoltLink.Api.Dtos;
using VoltLink.Api.Models;
using VoltLink.Api.Security;
using VoltLink.Api.Services;

namespace VoltLink.Api.Controllers;


[ApiController]
[Route("api/v1/prosumers")]
[Produces("application/json")]
[Authorize]
public class ProsumersController : ControllerBase
{
    private readonly IAccountService _accounts;


    public ProsumersController(IAccountService accounts)
    {
        _accounts = accounts;
    }

   
    [HttpGet]
    [Authorize(Policy = Policies.Staff)]
    [ProducesResponseType(typeof(IReadOnlyList<UserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> ListAsync(
        [FromQuery] bool? isActive,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var prosumers = await _accounts.ListAsync(
            role: UserRoles.Prosumer,
            isActive: isActive,
            deactivationRequested: null,
            search: search,
            cancellationToken: cancellationToken);

        return Ok(prosumers);
    }


    [HttpGet("pending")]
    [Authorize(Policy = Policies.Backoffice)]
    [ProducesResponseType(typeof(IReadOnlyList<UserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> ListPendingAsync(
        CancellationToken cancellationToken)
    {
        
        var pending = await _accounts.ListAsync(
            role: UserRoles.Prosumer,
            isActive: false,
            cancellationToken: cancellationToken);

        return Ok(pending);
    }

   
    [HttpGet("deactivation-requests")]
    [Authorize(Policy = Policies.Backoffice)]
    [ProducesResponseType(typeof(IReadOnlyList<UserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> ListDeactivationRequestsAsync(
        CancellationToken cancellationToken)
    {
        var requests = await _accounts.ListAsync(
            role: UserRoles.Prosumer,
            deactivationRequested: true,
            cancellationToken: cancellationToken);

        return Ok(requests);
    }

    
    [HttpGet("{nic}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetByNicAsync(
        string nic, CancellationToken cancellationToken)
    {
       
        User.EnsureOwnerOrStaff(nic);

        var prosumer = await _accounts.GetByIdAsync(nic, cancellationToken);
        return Ok(prosumer);
    }

   
    [HttpPost]
    [Authorize(Policy = Policies.Backoffice)]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> CreateAsync(
        [FromBody] CreateProsumerRequest request, CancellationToken cancellationToken)
    {
        var created = await _accounts.CreateProsumerAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByNicAsync), new { nic = created.Id }, created);
    }

  
    [HttpPut("{nic}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> UpdateAsync(
        string nic, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        // Ownership check before any data is changed.
        User.EnsureOwnerOrStaff(nic);

        var updated = await _accounts.UpdateAsync(nic, request, cancellationToken);
        return Ok(updated);
    }

    
    [HttpPatch("{nic}/request-deactivation")]
    [Authorize(Policy = Policies.Prosumer)]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserResponse>> RequestDeactivationAsync(
        string nic, CancellationToken cancellationToken)
    {
        // A prosumer may only request deactivation of their own account.
        User.EnsureOwnerOrStaff(nic);

        var updated = await _accounts.RequestDeactivationAsync(nic, cancellationToken);
        return Ok(updated);
    }

   
    [HttpPatch("{nic}/activate")]
    [Authorize(Policy = Policies.Backoffice)]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> ActivateAsync(
        string nic, CancellationToken cancellationToken)
    {
        var updated = await _accounts.ActivateAsync(nic, cancellationToken);
        return Ok(updated);
    }

  
    [HttpPatch("{nic}/deactivate")]
    [Authorize(Policy = Policies.Backoffice)]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> DeactivateAsync(
        string nic, CancellationToken cancellationToken)
    {
        var updated = await _accounts.DeactivateAsync(nic, cancellationToken);
        return Ok(updated);
    }
}
