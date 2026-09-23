
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace VoltLink.Api.Middleware;


public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

 
    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
         
            _logger.LogWarning("{Code}: {Message}", ex.Code, ex.Message);
            await WriteProblemAsync(context, MapStatusCode(ex), ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            
            _logger.LogError(ex, "Unhandled exception while processing {Path}.", context.Request.Path);
            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                ErrorCodes.Unexpected,
                "An unexpected error occurred while processing the request.");
        }
    }

  
    private static int MapStatusCode(AppException exception) => exception switch
    {
        NotFoundException => StatusCodes.Status404NotFound,
        ValidationException => StatusCodes.Status400BadRequest,
        ForbiddenException => StatusCodes.Status403Forbidden,
        ConflictException => StatusCodes.Status409Conflict,

    
        BusinessRuleViolationException => StatusCodes.Status422UnprocessableEntity,

        _ => StatusCodes.Status500InternalServerError
    };

    
    private static async Task WriteProblemAsync(
        HttpContext context, int statusCode, string code, string message)
    {
       
        if (context.Response.HasStarted)
        {
            return;
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = code,
            Detail = message,
            Instance = context.Request.Path
        };

       
        problem.Extensions["errorCode"] = code;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }


    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
