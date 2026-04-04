using Microsoft.AspNetCore.Http;
using PersonalExpense.Application.DTOs;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;

namespace PersonalExpense.API.Middleware;

public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlerMiddleware(RequestDelegate next)
    {
        _next = next;
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
        var traceId = context.TraceIdentifier;
        var statusCode = HttpStatusCode.InternalServerError;
        var code = "InternalServerError";
        var message = "An unexpected error occurred";
        var details = new List<string>();

        switch (exception)
        {
            case KeyNotFoundException:
                statusCode = HttpStatusCode.NotFound;
                code = "NotFound";
                message = exception.Message;
                break;
            case ArgumentException:
                statusCode = HttpStatusCode.BadRequest;
                code = "BadRequest";
                message = exception.Message;
                break;
            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                code = "Unauthorized";
                message = "You are not authorized to access this resource";
                break;
            default:
                // 对于其他异常，记录详细信息
                details.Add(exception.Message);
                details.Add(exception.StackTrace ?? string.Empty);
                break;
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var errorResponse = new ErrorResponseDto(
            TraceId: traceId,
            Code: code,
            Message: message,
            Details: details.Count > 0 ? details : null
        );

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
}
