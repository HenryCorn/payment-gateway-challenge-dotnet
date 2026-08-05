namespace PaymentGateway.Api.Domain;

/// <summary>
/// Outcome of one authorization attempt.
/// </summary>
public sealed record BankPaymentResult(BankPaymentOutcome Outcome, string? AuthorizationCode = null);