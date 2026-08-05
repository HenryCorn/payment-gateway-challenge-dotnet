using PaymentGateway.Api.Contracts.Merchant;

namespace PaymentGateway.Api.Domain;

/// <summary>
/// A validated payment instruction.
/// </summary>
public sealed record Payment
{
    /// <summary>
    /// Gets the card number used for the payment.
    /// </summary>
    public required string CardNumber { get; init; }
    
    /// <summary>
    /// Gets the expiry month of the card used for the payment.
    /// </summary>
    public required int ExpiryMonth { get; init; }
    
    /// <summary>
    /// Gets the expiry year of the card used for the payment.
    /// </summary>
    public required int ExpiryYear { get; init; }
    
    /// <summary>
    /// Gets the currency of the payment.
    /// </summary>
    public required string Currency { get; init; }
    
    /// <summary>
    /// Gets the amount of the payment in the smallest currency unit.
    /// </summary>
    public required int Amount { get; init; }
    
    /// <summary>
    /// Gets the CVV of the card used for the payment.
    /// </summary>
    public required string Cvv { get; init; }
    
    /// <summary>
    /// Gets the last four digits of the card number used for the payment.
    /// </summary>
    public string LastFourCardDigits => CardNumber[^4..];
    
    public static Payment FromValidatedRequest(PostPaymentRequest request)
    {
        return new Payment
        {
            CardNumber = request.CardNumber!,
            ExpiryMonth = request.ExpiryMonth!.Value,
            ExpiryYear = request.ExpiryYear!.Value,
            Currency = request.Currency!.ToUpperInvariant(),
            Amount = request.Amount!.Value,
            Cvv = request.Cvv!
        };
    }
    
    public PaymentResponse ToResponse(Guid id, PaymentStatus status)
    {
        return new PaymentResponse(
            Id: id,
            Status: status,
            CardNumberLastFour: LastFourCardDigits,
            ExpiryMonth: ExpiryMonth,
            ExpiryYear: ExpiryYear,
            Currency: Currency,
            Amount: Amount
        );
    }

    public override string ToString()
    {
        return $"Payment {{ CardNumber = ****{LastFourCardDigits}, ExpiryMonth = {ExpiryMonth}, "
               + $"ExpiryYear = {ExpiryYear}, Currency = {Currency}, Amount = {Amount}, Cvv = *** }}";
    }
}