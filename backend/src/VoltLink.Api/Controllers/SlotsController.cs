using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoltLink.Api.Dtos;
using VoltLink.Api.Models;
using VoltLink.Api.Services;

namespace VoltLink.Api.Controllers;


[ApiController]
[Route("api/v1/slots")]
[Produces("application/json")]
[Authorize]
public class SlotsController : ControllerBase
{
    private readonly ISlotService _slots;

    public SlotsController(ISlotService slots)
    {
        _slots = slots;
    }

 
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SlotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SlotResponse>> GetByIdAsync(
        string id, CancellationToken cancellationToken)
    {
        var slot = await _slots.GetByIdAsync(id, cancellationToken);
        return Ok(slot);
    }


    [HttpPut("{id}")]
    [Authorize(Policy = Policies.Staff)]
    [ProducesResponseType(typeof(SlotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<SlotResponse>> UpdateAsync(
        string id, [FromBody] UpdateSlotRequest request, CancellationToken cancellationToken)
    {
        var updated = await _slots.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }


    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.Staff)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        await _slots.DeleteAsync(id, cancellationToken);

       
        return NoContent();
    }
}
