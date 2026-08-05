using System.Text.Json.Serialization;

using PaymentGateway.Api.Domain;

namespace PaymentGateway.Api.Contracts.AcquiringBank;

/// <summary>
/// The acquiring bank's request shape,
/// </summary>
public sealed class BankAuthorizationRequest
{
    /// <summary>
    /// Gets or sets the card number to be authorized.
    /// </summary>
    [JsonPropertyName("card_number")]
    public required string CardNumber {get; set;}
    
    /// <summary>
    /// Gets or sets the expiry date of the card in the format "MM/YY".
    /// </summary>
    [JsonPropertyName("expiry_date")]
    public required string ExpiryDate { get; set; }
    
    /// <summary>
    /// Gets or sets the CVV of the card.
    /// </summary>
    [JsonPropertyName("currency")]
    public required string Currency { get; set; }
    
    /// <summary>
    /// Gets or sets the amount to be authorized in the smallest currency unit (e.g., cents for USD).
    /// </summary>
    [JsonPropertyName("amount")]
    public required int Amount { get; set; }
    
    /// <summary>
    /// Gets or sets the CVV of the card.
    /// </summary>
    [JsonPropertyName("cvv")]
    public required string Cvv { get; init; }
    
    public static BankAuthorizationRequest FromValidatedPayment(Payment payment)
    {
        return new BankAuthorizationRequest
        {
            CardNumber = payment.CardNumber,
            ExpiryDate = $"{payment.ExpiryMonth:D2}/{payment.ExpiryYear:D4}",
            Currency = payment.Currency,
            Amount = payment.Amount,
            Cvv = payment.Cvv
        };
    }
    
    public override string ToString()
    {
        return $"BankAuthorizationRequest {{ CardNumber = ****{CardNumber[^4..]}, "
               + $"ExpiryDate = {ExpiryDate}, Currency = {Currency}, Amount = {Amount}, Cvv = *** }}";
    }
}