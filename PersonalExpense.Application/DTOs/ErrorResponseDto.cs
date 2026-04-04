namespace PersonalExpense.Application.DTOs;

public record ErrorResponseDto(
    string TraceId,
    string Code,
    string Message,
    IEnumerable<string>? Details = null
);
