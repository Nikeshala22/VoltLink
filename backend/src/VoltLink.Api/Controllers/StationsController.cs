using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoltLink.Api.Dtos;
using VoltLink.Api.Models;
using VoltLink.Api.Services;

namespace VoltLink.Api.Controllers;


[ApiController]
[Route("api/v1/stations")]
[Produces("application/json")]
[Authorize]
public class StationsController : ControllerBase
{
    private readonly IStationService _stations;
    private readonly ISlotService _slots;

  
    public StationsController(IStationService stations, ISlotService slots)
    {
        _stations = stations;
        _slots = slots;
    }


    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<StationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StationResponse>>> ListAsync(
        [FromQuery] bool? isActive,
        [FromQuery] string? city,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var stations = await _stations.ListAsync(isActive, city, search, cancellationToken);
        return Ok(stations);
    }

 
    [HttpGet("nearby")]
    [ProducesResponseType(typeof(IReadOnlyList<NearbyStationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<NearbyStationResponse>>> FindNearbyAsync(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] double radiusKm = 10d,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var stations = await _stations.FindNearbyAsync(lat, lng, radiusKm, limit, cancellationToken);
        return Ok(stations);
    }

  
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(StationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StationResponse>> GetByIdAsync(
        string id, CancellationToken cancellationToken)
    {
        var station = await _stations.GetByIdAsync(id, cancellationToken);
        return Ok(station);
    }


    [HttpPost]
    [Authorize(Policy = Policies.Backoffice)]
    [ProducesResponseType(typeof(StationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StationResponse>> CreateAsync(
        [FromBody] CreateStationRequest request, CancellationToken cancellationToken)
    {
        var created = await _stations.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = created.Id }, created);
    }


    [HttpPut("{id}")]
    [Authorize(Policy = Policies.Backoffice)]
    [ProducesResponseType(typeof(StationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StationResponse>> UpdateAsync(
        string id, [FromBody] UpdateStationRequest request, CancellationToken cancellationToken)
    {
        var updated = await _stations.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

  
    [HttpPatch("{id}/activate")]
    [Authorize(Policy = Policies.Backoffice)]
    [ProducesResponseType(typeof(StationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<StationResponse>> ActivateAsync(
        string id, CancellationToken cancellationToken)
    {
        var updated = await _stations.ActivateAsync(id, cancellationToken);
        return Ok(updated);
    }


    [HttpPatch("{id}/deactivate")]
    [Authorize(Policy = Policies.Backoffice)]
    [ProducesResponseType(typeof(StationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<StationResponse>> DeactivateAsync(
        string id, CancellationToken cancellationToken)
    {
        var updated = await _stations.DeactivateAsync(id, cancellationToken);
        return Ok(updated);
    }

    
    [HttpPatch("{id}/battery-slots")]
    [Authorize(Policy = Policies.Staff)]
    [ProducesResponseType(typeof(StationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StationResponse>> UpdateBatterySlotsAsync(
        string id, [FromBody] UpdateBatterySlotsRequest request, CancellationToken cancellationToken)
    {
        var updated = await _stations.UpdateBatterySlotsAsync(id, request, cancellationToken);
        return Ok(updated);
    }

 
    [HttpGet("{id}/slots")]
    [ProducesResponseType(typeof(IReadOnlyList<SlotResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<SlotResponse>>> ListSlotsAsync(
        string id,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var slots = await _slots.ListByStationAsync(id, from, to, isActive, cancellationToken);
        return Ok(slots);
    }

    [HttpPost("{id}/slots")]
    [Authorize(Policy = Policies.Staff)]
    [ProducesResponseType(typeof(SlotResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SlotResponse>> CreateSlotAsync(
        string id, [FromBody] CreateSlotRequest request, CancellationToken cancellationToken)
    {
        var created = await _slots.CreateAsync(id, request, cancellationToken);

        return CreatedAtAction(
            actionName: nameof(SlotsController.GetByIdAsync),
            controllerName: "Slots",
            routeValues: new { id = created.Id },
            value: created);
    }
}
