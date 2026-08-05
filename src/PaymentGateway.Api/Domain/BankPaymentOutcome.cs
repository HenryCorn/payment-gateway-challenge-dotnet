namespace PaymentGateway.Api.Domain;

/// <summary>
/// Represents the possible outcomes of a bank payment authorization attempt.
/// </summary>
public enum BankPaymentOutcome
{
    Authorized,
    Declined,
    Unavailable,
    InvalidRequest
}