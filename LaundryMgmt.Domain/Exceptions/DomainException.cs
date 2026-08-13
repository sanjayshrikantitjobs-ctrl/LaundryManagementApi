namespace LaundryMgmt.Domain.Exceptions;

/// <summary>Thrown when a business rule inside the Domain layer is violated.
/// Caught by API middleware and translated into a 400/409 response.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
