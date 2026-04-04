using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text.Json;

namespace PersonalExpense.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalExceptionMiddleware(RequestDelegate next)
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

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";
        var traceId = context.TraceIdentifier;

        var (statusCode, code, message, details) = ex switch
        {
            ArgumentException => (HttpStatusCode.BadRequest, "VALIDATION_ERROR", ex.Message, null),
            InvalidOperationException => (HttpStatusCode.BadRequest, "INVALID_OPERATION", ex.Message, null),
            KeyNotFoundException => (HttpStatusCode.NotFound, "NOT_FOUND", ex.Message, null),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "UNAUTHORIZED", "Access denied", null),
            _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred", ex.ToString())
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            traceId,
            code,
            message,
            details
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
