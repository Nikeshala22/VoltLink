using Microsoft.AspNetCore.Mvc;
using VoltLink.Api.Data;

namespace VoltLink.Api.Controllers;


[ApiController]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    private readonly MongoContext _context;
    private readonly ILogger<HealthController> _logger;

    
    public HealthController(MongoContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }


  
    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
     
        try
        {
            await _context.PingAsync(cancellationToken);

            return Ok(new
            {
                status = "Healthy",
                service = "VoltLink.Api",
                database = "Connected",
                serverTimeUtc = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
           
            _logger.LogError(ex, "Health check failed: MongoDB is unreachable.");

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "Unhealthy",
                service = "VoltLink.Api",
                database = "Unreachable",
                serverTimeUtc = DateTime.UtcNow
            });
        }
    }
}
