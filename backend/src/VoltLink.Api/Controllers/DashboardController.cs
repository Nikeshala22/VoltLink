using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VoltLink.Api.Dtos;
using VoltLink.Api.Models;
using VoltLink.Api.Security;
using VoltLink.Api.Services;

namespace VoltLink.Api.Controllers;


[ApiController]
[Route("api/v1/dashboard")]
[Produces("application/json")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }


    [HttpGet("prosumer")]
    [Authorize(Policy = Policies.Prosumer)]
    [ProducesResponseType(typeof(ProsumerDashboardResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProsumerDashboardResponse>> GetProsumerDashboardAsync(
        CancellationToken cancellationToken)
    {
        
        var nic = User.GetUserId();
        var dashboard = await _dashboard.GetProsumerDashboardAsync(nic, cancellationToken);

        return Ok(dashboard);
    }


    [HttpGet("operator")]
    [Authorize(Policy = Policies.Staff)]
    [ProducesResponseType(typeof(OperatorDashboardResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<OperatorDashboardResponse>> GetOperatorDashboardAsync(
        CancellationToken cancellationToken)
    {
        var dashboard = await _dashboard.GetOperatorDashboardAsync(cancellationToken);
        return Ok(dashboard);
    }
}
