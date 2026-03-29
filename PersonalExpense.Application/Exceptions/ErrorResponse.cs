namespace PersonalExpense.Application.Exceptions;

public record ErrorResponse(
    string TraceId,
    string Code,
    string Message,
    object? Details = null
);
