namespace PersonalExpense.Application.Exceptions;

public class ErrorResponse
{
    public string TraceId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
}
