using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Contracts.AcquiringBank;

/// <summary>
/// Represents the response from the acquiring bank after attempting to authorize a payment.
/// </summary>
internal sealed record BankAuthorizationResponse
{
    /// <summary>
    /// Gets a value indicating whether the payment was authorized by the acquiring bank.
    /// </summary>
    [JsonPropertyName("authorized")]
    public bool Authorized { get; init; }
    
    /// <summary>
    /// Gets the authorization code provided by the acquiring bank if the payment was authorized; otherwise, null.
    /// </summary>
    [JsonPropertyName("authorization_code")]
    public string? AuthorizationCode { get; init; }
}