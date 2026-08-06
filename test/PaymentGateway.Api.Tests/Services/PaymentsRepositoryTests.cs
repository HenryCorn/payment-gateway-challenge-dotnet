using PaymentGateway.Api.Contracts.Merchant;
using PaymentGateway.Api.Domain;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests.Services;

public class PaymentsRepositoryTests
{
    private readonly PaymentsRepository _repository = new();
    
    [Fact]
    public void GetPayment_Returns_PreviouslyAddedPayment()
    {
        PaymentResponse payment = SampleResponse();
        
        _repository.AddPayment(payment);
        
        Assert.Equal(payment, _repository.GetPayment(payment.Id));
    }
    
    [Fact]
    public void GetPayment_ReturnsNull_WhenPaymentNotFound()
    {
        Assert.Null(_repository.GetPayment(Guid.NewGuid()));
    }
    
    [Fact]
    public void AddPayment_Overwrites_WhenTheSameIdIsAddedTwice()
    {
        Guid id = Guid.NewGuid();

        _repository.AddPayment(SampleResponse(id));
        _repository.AddPayment(SampleResponse(id, PaymentStatus.Declined));

        Assert.Equal(PaymentStatus.Declined, _repository.GetPayment(id)!.Status);
    }

    [Fact]
    public void AddPayment_KeepsEveryPayment_WhenWritersRunConcurrently()
    {
        PaymentResponse[] payments = Enumerable.Range(0, 1_000)
            .Select(_ => SampleResponse())
            .ToArray();

        Parallel.ForEach(payments, _repository.AddPayment);

        Assert.All(payments, payment => Assert.NotNull(_repository.GetPayment(payment.Id)));
    }
    
    private static PaymentResponse SampleResponse(
        Guid? id = null,
        PaymentStatus status = PaymentStatus.Authorized)
    {
        return new PaymentResponse(
            Id: id ?? Guid.NewGuid(),
            Status: status,
            CardNumberLastFour: "8877",
            ExpiryMonth: 4,
            ExpiryYear: 2030,
            Currency: "GBP",
            Amount: 100);
    }
}