namespace Shared.Abstractions.Exceptions;

/// <summary>
/// Base exception for domain-related errors
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    
    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Exception thrown when an entity is not found
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object id) 
        : base($"{entityName} with ID '{id}' was not found.") { }
    
    public NotFoundException(string message) : base(message) { }
}

/// <summary>
/// Exception thrown for validation errors
/// </summary>
public class ValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; }
    
    public ValidationException(Dictionary<string, string[]> errors) 
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }
    
    public ValidationException(string fieldName, string error) 
        : base($"Validation failed for {fieldName}")
    {
        Errors = new Dictionary<string, string[]>
        {
            { fieldName, new[] { error } }
        };
    }
}

/// <summary>
/// Exception thrown when a business rule is violated
/// </summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
    
    public BusinessRuleException(string message, Exception innerException) : base(message, innerException) { }
}
