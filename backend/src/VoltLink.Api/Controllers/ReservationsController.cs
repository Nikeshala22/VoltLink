using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoltLink.Api.Dtos;
using VoltLink.Api.Models;
using VoltLink.Api.Repositories;
using VoltLink.Api.Security;
using VoltLink.Api.Services;

namespace VoltLink.Api.Controllers;


[ApiController]
[Route("api/v1/reservations")]
[Produces("application/json")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservations;

        public ReservationsController(IReservationService reservations)
    {
        _reservations = reservations;
    }

    
    private CallerContext Caller => new(User.GetUserId(), User.GetRole());

   
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ReservationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReservationResponse>>> SearchAsync(
        [FromQuery] string? nic,
        [FromQuery] string? stationId,
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? q,
        [FromQuery] int limit = 200,
        CancellationToken cancellationToken = default)
    {
        var query = new ReservationQuery
        {
            ProsumerNic = nic,
            StationId = stationId,
            Status = status,
            FromUtc = from,
            ToUtc = to,
            Search = q,
            Limit = limit
        };

        var results = await _reservations.SearchAsync(query, Caller, cancellationToken);
        return Ok(results);
    }

   
    [HttpGet("pending")]
    [Authorize(Policy = Policies.Staff)]
    [ProducesResponseType(typeof(IReadOnlyList<ReservationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReservationResponse>>> ListPendingAsync(
        CancellationToken cancellationToken)
    {
        var query = new ReservationQuery { Status = ReservationStatus.Pending };
        var results = await _reservations.SearchAsync(query, Caller, cancellationToken);

        return Ok(results);
    }

   
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationResponse>> GetByIdAsync(
        string id, CancellationToken cancellationToken)
    {
        var reservation = await _reservations.GetByIdAsync(id, Caller, cancellationToken);
        return Ok(reservation);
    }

    
    [HttpPost]
    [ProducesResponseType(typeof(ReservationSummaryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ReservationSummaryResponse>> CreateAsync(
        [FromBody] CreateReservationRequest request, CancellationToken cancellationToken)
    {
        var summary = await _reservations.CreateAsync(request, Caller, cancellationToken);

        return CreatedAtAction(
            nameof(GetByIdAsync), new { id = summary.Reservation.Id }, summary);
    }

    
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ReservationSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ReservationSummaryResponse>> UpdateAsync(
        string id, [FromBody] UpdateReservationRequest request, CancellationToken cancellationToken)
    {
        var summary = await _reservations.UpdateAsync(id, request, Caller, cancellationToken);
        return Ok(summary);
    }

   
    [HttpPatch("{id}/cancel")]
    [ProducesResponseType(typeof(ReservationSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ReservationSummaryResponse>> CancelAsync(
        string id, CancellationToken cancellationToken)
    {
        var summary = await _reservations.CancelAsync(id, Caller, cancellationToken);
        return Ok(summary);
    }

    
    [HttpPatch("{id}/approve")]
    [Authorize(Policy = Policies.Staff)]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ReservationResponse>> ApproveAsync(
        string id, CancellationToken cancellationToken)
    {
        var reservation = await _reservations.ApproveAsync(id, cancellationToken);
        return Ok(reservation);
    }

   
    [HttpPatch("{id}/reject")]
    [Authorize(Policy = Policies.Staff)]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ReservationResponse>> RejectAsync(
        string id, CancellationToken cancellationToken)
    {
        var reservation = await _reservations.RejectAsync(id, cancellationToken);
        return Ok(reservation);
    }

    
    [HttpGet("{id}/qr")]
    [ProducesResponseType(typeof(QrCodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<QrCodeResponse>> GetQrCodeAsync(
        string id, CancellationToken cancellationToken)
    {
        var qr = await _reservations.GetQrCodeAsync(id, Caller, cancellationToken);
        return Ok(qr);
    }

    
    [HttpPost("verify-qr")]
    [Authorize(Policy = Policies.Staff)]
    [ProducesResponseType(typeof(ReservationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ReservationResponse>> VerifyQrAsync(
        [FromBody] VerifyQrRequest request, CancellationToken cancellationToken)
    {
        var reservation = await _reservations.VerifyQrAsync(request.Token, cancellationToken);
        return Ok(reservation);
    }

   
    [HttpPost("{id}/complete")]
    [Authorize(Policy = Policies.Staff)]
    [ProducesResponseType(typeof(ReservationSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ReservationSummaryResponse>> CompleteAsync(
        string id, CancellationToken cancellationToken)
    {
        var summary = await _reservations.CompleteAsync(id, Caller, cancellationToken);
        return Ok(summary);
    }
}
