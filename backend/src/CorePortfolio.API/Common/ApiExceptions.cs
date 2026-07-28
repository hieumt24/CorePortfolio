namespace CorePortfolio.API.Common;

public sealed class ResourceNotFoundException : Exception
{
    public ResourceNotFoundException(string message) : base(message) { }
}

public sealed class ResourceConflictException : Exception
{
    public ResourceConflictException(string message) : base(message) { }
}

public sealed class RequestValidationException : Exception
{
    public RequestValidationException(string message) : base(message) { }
}

public sealed class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message = "You do not have permission to perform this action.")
        : base(message) { }
}

public sealed class ServiceUnavailableException : Exception
{
    public ServiceUnavailableException(string message) : base(message) { }
}
