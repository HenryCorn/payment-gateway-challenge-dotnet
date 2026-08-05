using PaymentGateway.Api.Enums;

namespace PaymentGateway.Api.Models.Responses;

// <summary>
// Represents the response returned after processing a payment.
// </summary>
public sealed record PaymentResponse(
    // <summary>
    // Gets the unique identifier of the payment.
    // </summary>
    Guid Id,
    
    // <summary>
    // Gets the status of the payment.
    // </summary>
    PaymentStatus Status,
    
    // <summary>
    // Gets the last four digits of the card number used for the payment.
    // </summary>
    string CardNumberLastFour,
    
    // <summary>
    // Gets the expiry month of the card used for the payment.
    // </summary>
    int ExpiryMonth,
    
    // <summary>
    // Gets the expiry year of the card used for the payment.
    // </summary>
    int ExpiryYear,
    
    // <summary>
    // Gets the currency of the payment.
    // </summary>
    string Currency,
    
    // <summary>
    // Gets the amount of the payment in the smallest currency unit.
    // </summary>
    int Amount
);