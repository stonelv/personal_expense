using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PersonalExpense.Application.Exceptions;
using System.Net;
using System.Text.Json;

namespace PersonalExpense.Application.Middleware;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var errorResponse = new ErrorResponse(
            TraceId: traceId,
            Code: "InternalServerError",
            Message: exception.Message
        );

        var statusCode = HttpStatusCode.InternalServerError;

        switch (exception)
        {
            case InvalidOperationException invalidOp:
                statusCode = HttpStatusCode.BadRequest;
                errorResponse = errorResponse with { Code = "BadRequest", Message = invalidOp.Message };
                break;
            case KeyNotFoundException:
                statusCode = HttpStatusCode.NotFound;
                errorResponse = errorResponse with { Code = "NotFound", Message = "Resource not found" };
                break;
            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                errorResponse = errorResponse with { Code = "Unauthorized", Message = "Unauthorized access" };
                break;
        }

        _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        await context.Response.WriteAsync(json);
    }
}
