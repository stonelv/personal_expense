namespace PersonalExpense.Application.Exceptions;

public abstract class CustomException : Exception
{
    public string Code { get; }
    public int StatusCode { get; }

    protected CustomException(string code, string message, int statusCode) : base(message)
    {
        Code = code;
        StatusCode = statusCode;
    }
}

public class NotFoundException : CustomException
{
    public NotFoundException(string message) : base("NOT_FOUND", message, 404)
    {
    }

    public NotFoundException(string entityName, Guid id) 
        : base("NOT_FOUND", $"{entityName} with id {id} not found", 404)
    {
    }
}

public class BadRequestException : CustomException
{
    public BadRequestException(string message) : base("BAD_REQUEST", message, 400)
    {
    }
}

public class UnauthorizedException : CustomException
{
    public UnauthorizedException(string message) : base("UNAUTHORIZED", message, 401)
    {
    }
}

public class ForbiddenException : CustomException
{
    public ForbiddenException(string message) : base("FORBIDDEN", message, 403)
    {
    }
}

public class ConflictException : CustomException
{
    public ConflictException(string message) : base("CONFLICT", message, 409)
    {
    }
}
