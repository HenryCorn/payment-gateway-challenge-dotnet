using PaymentGateway.Api.Contracts.Merchant;

namespace PaymentGateway.Api.Services;

public class PaymentsRepository
{
    private readonly List<PaymentResponse> Payments = new();
    
    public void Add(PaymentResponse payment)
    {
        Payments.Add(payment);
    }

    public PaymentResponse Get(Guid id)
    {
        return Payments.FirstOrDefault(p => p.Id == id);
    }
}