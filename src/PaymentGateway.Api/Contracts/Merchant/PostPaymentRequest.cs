namespace PaymentGateway.Api.Contracts.Merchant;

// <summary>
// Represents the request to post a payment.
// </summary>
public class PostPaymentRequest
{
    // <summary>
    // Gets or sets the card number.
    // </summary>
    public string? CardNumber { get; init; }
    
    // <summary>
    // Gets or sets the expiry month of the card. This is an integer representing the month (1-12).
    // </summary>
    public int? ExpiryMonth { get; init; }
    
    // <summary>
    // Gets or sets the expiry year of the card. This is an integer representing the year (2026).
    // </summary>
    public int? ExpiryYear { get; init; }
    
    /// <summary>
    /// Gets or sets the currency of the payment.
    /// Reference for the ISO currency codes: <see href="https://www.xe.com/iso4217.php"/>
    /// </summary>
    public string? Currency { get; init; }
    
    /// <summary>
    /// Gets or sets the amount of the payment in the smallest currency unit (e.g., cents for USD).
    /// </summary>
    public int? Amount { get; init; }
    
    /// <summary>
    /// Gets or sets the CVV of the card.
    /// </summary>
    public string? Cvv { get; init; }
}