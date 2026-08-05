using PaymentGateway.Api.Domain;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.TestUtils.Builders;

namespace PaymentGateway.Api.Tests.Domain;

public class BankAuthorizationRequestTests
{
    private const string CardNumber = "2222405343248877";
    private const string Cvv = "987";

    [Fact]
    public void FromValidatedPayment_Maps_EveryFieldOntoTheBanksContract()
    {
        BankAuthorizationRequest request = BankAuthorizationRequest.FromValidatedPayment(SamplePayment());

        Assert.Equal(CardNumber, request.CardNumber);
        Assert.Equal("04/2030", request.ExpiryDate);
        Assert.Equal("GBP", request.Currency);
        Assert.Equal(100, request.Amount);
        Assert.Equal(Cvv, request.Cvv);
    }

    [Theory]
    [InlineData(4, 2030, "04/2030")]
    [InlineData(12, 2026, "12/2026")]
    public void FromValidatedPayment_Formats_ExpiryDateAsTwoDigitMonthAndFourDigitYear(
        int month,
        int year,
        string expectedExpiryDate)
    {
        BankAuthorizationRequest request = BankAuthorizationRequest.FromValidatedPayment(SamplePayment(month, year));

        Assert.Equal(expectedExpiryDate, request.ExpiryDate);
    }

    [Fact]
    public void ToString_ShouldNotLeakSensitiveInformation()
    {
        string text = BankAuthorizationRequest.FromValidatedPayment(SamplePayment()).ToString();

        Assert.DoesNotContain(CardNumber, text);
        Assert.DoesNotContain(Cvv, text);
        Assert.Contains("****8877", text);
    }
    
    private static Payment SamplePayment(int month = 4, int year = 2030)
    {
        return Payment.FromValidatedRequest(new PostPaymentRequestBuilder()
            .WithCardNumber(CardNumber)
            .WithExpiry(month, year)
            .WithCurrency("GBP")
            .WithAmount(100)
            .WithCvv(Cvv)
            .Build());
    }
}