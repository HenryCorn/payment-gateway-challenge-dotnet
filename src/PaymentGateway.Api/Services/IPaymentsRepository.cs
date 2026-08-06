using PaymentGateway.Api.Contracts.Merchant;

namespace PaymentGateway.Api.Services;

public interface IPaymentsRepository
{
    /// <summary>
    /// Adds a payment response to the repository.
    /// </summary>
    /// <param name="payment">The payment response to add.</param>
    void AddPayment(PaymentResponse payment);
    
    /// <summary>
    /// Retrieves a payment response from the repository by its unique identifier if it exists; otherwise, returns null.
    /// </summary>
    /// <param name="id">The unique identifier of the payment response to retrieve.</param>
    /// <returns>A <see cref="PaymentResponse"/> if found; otherwise, null.</returns>
    PaymentResponse? GetPayment(Guid id);
}