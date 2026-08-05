using PaymentGateway.Api.Domain;
using PaymentGateway.Api.TestUtils.Builders;

namespace PaymentGateway.Api.Tests.Domain;

public class PaymentTests
{
    private const string CardNumber = "1234567890123456";
    private const string Cvv = "123";
    
    [Fact]
    public void FromValidatedRequest_WhenLeadingZerosInLastFourDigits_ShouldPreserveLeadingZeros()
    {
        var request = new PostPaymentRequestBuilder()
            .WithCardNumber("1234567890120034")
            .Build();

        var payment = Payment.FromValidatedRequest(request);

        Assert.Equal("0034", payment.LastFourCardDigits);
    }
    
    [Fact]
    public void FromValidatedRequest_ShouldNormaliseCurrenciesToUpperCase()
    {
        var request = new PostPaymentRequestBuilder()
            .WithCurrency("eur")
            .Build();

        var payment = Payment.FromValidatedRequest(request);

        Assert.Equal("EUR", payment.Currency);
    }
    
    [Fact]
    public void ToString_ShouldNotLeakSensitiveInformation()
    {
        string text = SamplePayment().ToString();

        Assert.DoesNotContain(CardNumber, text);
        Assert.DoesNotContain(Cvv, text);
        Assert.Contains("****8877", text);
    }
    
    private static Payment SamplePayment(string cardNumber = CardNumber, string? currency = "EUR")
    {
        return Payment.FromValidatedRequest(new PostPaymentRequestBuilder()
            .WithCardNumber(cardNumber)
            .WithCurrency(currency)
            .WithCvv(Cvv)
            .Build()
        );
    }
}