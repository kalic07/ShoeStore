namespace Shared.Contracts.Exceptions;

/// <summary>
/// Base type for exceptions that are expected, "handled" application errors
/// (as opposed to bugs). Lives in Shared.Contracts (not Shared.Infrastructure)
/// specifically so framework-free Application layers (Order.Application,
/// Payment.Application) can throw these without taking a dependency on
/// ASP.NET Core/MassTransit. Shared.Infrastructure's ExceptionHandlingMiddleware
/// maps these to the right HTTP status codes at the API edge.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message) : base(message) { }
}

public sealed class NotFoundException : AppException
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with id '{key}' was not found.") { }

    public NotFoundException(string message) : base(message) { }
}

public sealed class ConflictException : AppException
{
    public ConflictException(string message) : base(message) { }
}

public sealed class ValidationAppException : AppException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationAppException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
}

public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message) : base(message) { }
}
