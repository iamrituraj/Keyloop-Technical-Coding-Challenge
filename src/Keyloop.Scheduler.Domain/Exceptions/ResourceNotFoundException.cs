namespace Keyloop.Scheduler.Domain.Exceptions;

public class ResourceNotFoundException : DomainException
{
    public ResourceNotFoundException(string entityName, string key) 
        : base($"{entityName} with key '{key}' was not found.") { }
}
