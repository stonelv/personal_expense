using System.Diagnostics;
using System.Net;
using System.Text.Json;
using PersonalExpense.Application.DTOs;
using PersonalExpense.Application.Exceptions;

namespace PersonalExpense.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
        var code = "INTERNAL_ERROR";
        var message = "An unexpected error occurred";
        var statusCode = (int)HttpStatusCode.InternalServerError;
        var details = new List<string>();

        switch (exception)
        {
            case CustomException customException:
                code = customException.Code;
                message = customException.Message;
                statusCode = customException.StatusCode;
                break;
            
            case KeyNotFoundException notFoundException:
                code = "NOT_FOUND";
                message = notFoundException.Message;
                statusCode = (int)HttpStatusCode.NotFound;
                break;
            
            case UnauthorizedAccessException unauthorizedException:
                code = "UNAUTHORIZED";
                message = unauthorizedException.Message;
                statusCode = (int)HttpStatusCode.Unauthorized;
                break;
            
            case ArgumentException argumentException:
                code = "BAD_REQUEST";
                message = argumentException.Message;
                statusCode = (int)HttpStatusCode.BadRequest;
                break;
            
            case InvalidOperationException invalidOperationException:
                code = "BAD_REQUEST";
                message = invalidOperationException.Message;
                statusCode = (int)HttpStatusCode.BadRequest;
                break;
            
            default:
                _logger.LogError(exception, "Unhandled exception occurred. TraceId: {TraceId}", traceId);
                details.Add(exception.Message);
                if (exception.InnerException != null)
                {
                    details.Add(exception.InnerException.Message);
                }
                break;
        }

        var response = new ErrorResponse
        {
            TraceId = traceId,
            Code = code,
            Message = message,
            Details = details
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var json = JsonSerializer.Serialize(response, options);

        return context.Response.WriteAsync(json);
    }
}
