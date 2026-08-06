using System.Net;
using System.Text.Json;

using PaymentGateway.Api.Domain;
using PaymentGateway.Api.TestUtils.Builders;

using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace PaymentGateway.Api.IntegrationTests.Services;

public class AcquiringBankClientTests
{
    private const string AuthorizingCard = "2222405343248877";
    private const string DecliningCard = "2222405343248874";
    private const string UnavailableCard = "2222405343248870";
    private const string TestCvv = "123";
    private const string TestExpiryDate = "04/2030";
    private const string TestCurrency = "GBP";
    private const int TestAmount = 100;

    [Fact]
    public async Task AuthorizeAsync_ReturnsAuthorizedWithCode_WhenTheBankApproves()
    {
        using AcquiringBankTestHost host = new();
        host.StubAuthorized("authorizedCode");

        BankPaymentResult result =
            await host.Client.AuthorizeAsync(SamplePayment(AuthorizingCard), CancellationToken.None);

        Assert.Equal(BankPaymentOutcome.Authorized, result.Outcome);
        Assert.Equal("authorizedCode", result.AuthorizationCode);
    }

    [Fact]
    public async Task AuthorizeAsync_ReturnsDeclinedWithoutCode_WhenTheBankRefuses()
    {
        using AcquiringBankTestHost host = new();
        host.StubDeclined();

        BankPaymentResult result =
            await host.Client.AuthorizeAsync(SamplePayment(DecliningCard), CancellationToken.None);

        Assert.Equal(BankPaymentOutcome.Declined, result.Outcome);
        Assert.Null(result.AuthorizationCode);
    }

    [Fact]
    public async Task AuthorizeAsync_ReturnsUnavailable_WhenTheBankReturns503()
    {
        using AcquiringBankTestHost host = new();
        host.StubStatus(HttpStatusCode.ServiceUnavailable);

        BankPaymentResult result =
            await host.Client.AuthorizeAsync(SamplePayment(UnavailableCard), CancellationToken.None);

        Assert.Equal(BankPaymentOutcome.Unavailable, result.Outcome);
    }

    [Fact]
    public async Task AuthorizeAsync_ReturnsInvalidRequest_WhenTheBankRejectsThePayloadAsMalformed()
    {
        using AcquiringBankTestHost host = new();
        host.StubStatus(HttpStatusCode.BadRequest);

        BankPaymentResult result =
            await host.Client.AuthorizeAsync(SamplePayment(AuthorizingCard), CancellationToken.None);

        Assert.Equal(BankPaymentOutcome.InvalidRequest, result.Outcome);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task AuthorizeAsync_ReturnsUnavailable_WhenTheBankReturnsAnUnexpectedStatus(
        HttpStatusCode statusCode)
    {
        using AcquiringBankTestHost host = new();
        host.StubStatus(statusCode);

        BankPaymentResult result =
            await host.Client.AuthorizeAsync(SamplePayment(AuthorizingCard), CancellationToken.None);

        Assert.Equal(BankPaymentOutcome.Unavailable, result.Outcome);
    }

    [Fact]
    public async Task AuthorizeAsync_ReturnsUnavailable_WhenTheBankReturns200WithNoBody()
    {
        using AcquiringBankTestHost host = new();
        host.Bank
            .Given(Request.Create().WithPath("/payments").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("null"));

        BankPaymentResult result =
            await host.Client.AuthorizeAsync(SamplePayment(AuthorizingCard), CancellationToken.None);

        Assert.Equal(BankPaymentOutcome.Unavailable, result.Outcome);
        Assert.Null(result.AuthorizationCode);
    }

    [Fact]
    public async Task AuthorizeAsync_ReturnsUnavailable_WhenTheBankReturns200WithAnUnparseableBody()
    {
        using AcquiringBankTestHost host = new();
        host.Bank
            .Given(Request.Create().WithPath("/payments").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json")
                .WithBody("<html>Bad Gateway</html>"));

        BankPaymentResult result =
            await host.Client.AuthorizeAsync(SamplePayment(AuthorizingCard), CancellationToken.None);

        Assert.Equal(BankPaymentOutcome.Unavailable, result.Outcome);
        Assert.Null(result.AuthorizationCode);
    }

    [Fact]
    public async Task AuthorizeAsync_Sends_TheBanksWireContract()
    {
        using AcquiringBankTestHost host = new();
        host.StubAuthorized("code");

        await host.Client.AuthorizeAsync(SamplePayment(AuthorizingCard), CancellationToken.None);

        string body = host.Bank.LogEntries.Single().RequestMessage!.Body!;
        using JsonDocument document = JsonDocument.Parse(body);
        JsonElement root = document.RootElement;

        Assert.Equal(AuthorizingCard, root.GetProperty("card_number").GetString());
        Assert.Equal(TestExpiryDate, root.GetProperty("expiry_date").GetString());
        Assert.Equal(TestCurrency, root.GetProperty("currency").GetString());
        Assert.Equal(TestAmount, root.GetProperty("amount").GetInt32());
        Assert.Equal(TestCvv, root.GetProperty("cvv").GetString());
        Assert.Equal(5, root.EnumerateObject().Count());
    }

    private static Payment SamplePayment(string cardNumber)
    {
        return Payment.FromValidatedRequest(new PostPaymentRequestBuilder()
            .WithCardNumber(cardNumber)
            .WithExpiry(4, 2030)
            .WithCurrency(TestCurrency)
            .WithAmount(TestAmount)
            .WithCvv(TestCvv)
            .Build());
    }
}