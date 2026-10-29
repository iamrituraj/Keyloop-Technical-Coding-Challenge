namespace Keyloop.Scheduler.Domain.Exceptions;

public class ResourceConflictException : DomainException
{
    public string ResourceType { get; }
    public string ConflictingId { get; }
    public List<string> SuggestedAlternatives { get; }

    public ResourceConflictException(
        string message, 
        string resourceType = "", 
        string conflictingId = "",
        List<string>? suggestedAlternatives = null) 
        : base(message)
    {
        ResourceType = resourceType;
        ConflictingId = conflictingId;
        SuggestedAlternatives = suggestedAlternatives ?? new List<string>();
    }
}
