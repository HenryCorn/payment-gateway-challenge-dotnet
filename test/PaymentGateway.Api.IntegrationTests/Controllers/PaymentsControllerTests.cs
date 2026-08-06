using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PaymentGateway.Api.Contracts.Merchant;
using PaymentGateway.Api.Domain;
using PaymentGateway.Api.TestUtils.Builders;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace PaymentGateway.Api.IntegrationTests.Controllers;

public class PaymentsControllerTests : IDisposable
{
    private readonly WireMockServer _bank = WireMockServer.Start();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    
    
    public PaymentsControllerTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("AcquiringBank:BaseAddress", _bank.Url!);
                builder.UseSetting("AcquiringBank:MaxRetryAttempts", "2");
                builder.UseSetting("AcquiringBank:CircuitBreakerMinimumThroughput", "1000");
            });

        _client = _factory.CreateClient();
    }
    
    [Fact]
    public async Task PostPayment_Returns201Authorized_ForAnOddEndingCard()
    {
        BankResponds(HttpStatusCode.OK, new { authorized = true, authorization_code = "0bb07405" });

        HttpResponseMessage response =
            await _client.PostAsJsonAsync("/api/Payments", ARequestFor("2222405343248877"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        PaymentResponse? created = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.Equal(PaymentStatus.Authorized, created!.Status);
    }

    [Fact]
    public async Task PostPayment_Returns201Declined_ForAnEvenEndingCard()
    {
        BankResponds(HttpStatusCode.OK, new { authorized = false, authorization_code = "" });

        HttpResponseMessage response =
            await _client.PostAsJsonAsync("/api/Payments", ARequestFor("2222405343248874"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        PaymentResponse? created = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        Assert.Equal(PaymentStatus.Declined, created!.Status);
        HttpResponseMessage retrieved = await _client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, retrieved.StatusCode);
    }

    [Fact]
    public async Task PostPayment_Returns502AfterRetrying_ForAZeroEndingCard()
    {
        BankResponds(HttpStatusCode.ServiceUnavailable, new { });

        HttpResponseMessage response =
            await _client.PostAsJsonAsync("/api/Payments", ARequestFor("2222405343248870"));

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.Equal(3, _bank.LogEntries.Count());
    }

    [Fact]
    public async Task PostPayment_Returns422AndNeverReachesTheBank_ForAnInvalidRequest()
    {
        BankResponds(HttpStatusCode.OK, new { authorized = true, authorization_code = "code" });
        PostPaymentRequest invalid = new PostPaymentRequestBuilder().WithCvv("12").Build();

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/Payments", invalid);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        Assert.Empty(_bank.LogEntries);
    }

    private void BankResponds(HttpStatusCode statusCode, object body)
    {
        _bank
            .Given(Request.Create().WithPath("/payments").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(statusCode).WithBodyAsJson(body));
    }

    private static PostPaymentRequest ARequestFor(string cardNumber)
    {
        return new PostPaymentRequestBuilder()
            .WithCardNumber(cardNumber)
            .WithExpiry(4, 2030)
            .WithCurrency("GBP")
            .WithAmount(100)
            .WithCvv("123")
            .Build();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
        _bank.Dispose();
    }
}