using PaymentGateway.Api.Domain;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services;

public interface IAcquiringBankClient
{
    /// <summary>
    /// Asks the acquiring bank to authorize a payment.
    /// </summary>
    /// <param name="payment">The payment details to be authorized.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns> The task result contains the outcome of the authorization attempt.</returns>
    Task<BankPaymentResult> AuthorizeAsync(Payment payment, CancellationToken cancellationToken);
}