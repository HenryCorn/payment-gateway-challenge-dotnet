using System.Collections.Concurrent;

using PaymentGateway.Api.Contracts.Merchant;

namespace PaymentGateway.Api.Services;

public class PaymentsRepository : IPaymentsRepository
{
    private readonly ConcurrentDictionary<Guid, PaymentResponse> _payments = new();

    /// <inheritdoc/>
    public void Add(PaymentResponse payment)
    {
        _payments[payment.Id] = payment;
    }

    /// <inheritdoc/>
    public PaymentResponse? Get(Guid id)
    {
        return _payments.GetValueOrDefault(id);
    }
}