namespace PaymentService.Core.Domain;

/// <summary>A business rule was broken.</summary>
public class DomainException(string message) : Exception(message);
