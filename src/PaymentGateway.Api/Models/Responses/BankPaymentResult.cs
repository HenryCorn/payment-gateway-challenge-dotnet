using PaymentGateway.Api.Enums;

namespace PaymentGateway.Api.Models.Responses;

/// <summary>
/// Outcome of one authorization attempt.
/// </summary>
public sealed record BankPaymentResult(BankPaymentOutcome Outcome, string? AuthorizationCode = null);