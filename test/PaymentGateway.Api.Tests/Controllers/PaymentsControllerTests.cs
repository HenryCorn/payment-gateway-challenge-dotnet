using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PaymentGateway.Api.Contracts.Merchant;
using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Domain;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.TestUtils.Builders;

namespace PaymentGateway.Api.Tests.Controllers;

public class PaymentsControllerTests : IDisposable
{
    private const string TestCard = "2222405343248877";
    private readonly Mock<IPaymentsRepository> _repository = new();
    private readonly Mock<IAcquiringBankClient> _bank = new();
    private readonly WebApplicationFactory<PaymentsController> _factory;
    private readonly HttpClient _client;
    private PaymentResponse? _stored;
    
    public PaymentsControllerTests()
    {
        _repository
            .Setup(repository => repository.AddPayment(It.IsAny<PaymentResponse>()))
            .Callback<PaymentResponse>(payment => _stored = payment);

        _factory = new WebApplicationFactory<PaymentsController>()
            .WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(_repository.Object);
                services.AddSingleton(_bank.Object);
            }));

        _client = _factory.CreateClient();
    }
    
    [Fact]
    public async Task GetPayment_Returns200AndThePayment_WhenItExists()
    {
        PaymentResponse stored = new(
            Id: Guid.NewGuid(),
            Status: PaymentStatus.Authorized,
            CardNumberLastFour: "8877",
            ExpiryMonth: 4,
            ExpiryYear: 2030,
            Currency: "GBP",
            Amount: 100);

        _repository.Setup(repository => repository.GetPayment(stored.Id)).Returns(stored);

        HttpResponseMessage response = await _client.GetAsync($"/api/Payments/{stored.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(stored, await response.Content.ReadFromJsonAsync<PaymentResponse>());
    }

    [Fact]
    public async Task GetPayment_Returns404_WhenThePaymentIsUnknown()
    {
        HttpResponseMessage response = await _client.GetAsync($"/api/Payments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
    
    [Fact]
    public async Task PostPayment_Returns201WithThePaymentDetails_WhenTheBankAuthorizes()
    {
        BankAnswers(BankPaymentOutcome.Authorized, "auth-code");

        HttpResponseMessage response = await PostPaymentAsync();
        PaymentResponse? returned = await response.Content.ReadFromJsonAsync<PaymentResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(
            new PaymentResponse(returned!.Id, PaymentStatus.Authorized, "8877", 4, 2030, "GBP", 100),
            returned);
    }

    [Fact]
    public async Task PostPayment_Returns201WithDeclinedStatus_WhenTheBankRefuses()
    {
        BankAnswers(BankPaymentOutcome.Declined);

        HttpResponseMessage response = await PostPaymentAsync();
        PaymentResponse? returned = await response.Content.ReadFromJsonAsync<PaymentResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(PaymentStatus.Declined, returned!.Status);
    }

    [Fact]
    public async Task PostPayment_StoresExactlyWhatItReturns_WhenThePaymentIsCreated()
    {
        BankAnswers(BankPaymentOutcome.Authorized, "auth-code");

        HttpResponseMessage response = await PostPaymentAsync();
        PaymentResponse? returned = await response.Content.ReadFromJsonAsync<PaymentResponse>();

        Assert.Equal(returned, _stored);
    }

    [Fact]
    public async Task PostPayment_PointsLocationAtTheCreatedPayment_WhenThePaymentIsCreated()
    {
        BankAnswers(BankPaymentOutcome.Authorized, "auth-code");

        HttpResponseMessage response = await PostPaymentAsync();
        PaymentResponse? returned = await response.Content.ReadFromJsonAsync<PaymentResponse>();

        Assert.Equal(
            $"/api/Payments/{returned!.Id}",
            response.Headers.Location?.AbsolutePath,
            ignoreCase: true);
    }
    
    [Theory]
    [InlineData(BankPaymentOutcome.Unavailable, HttpStatusCode.BadGateway)]
    [InlineData(BankPaymentOutcome.InvalidRequest, HttpStatusCode.InternalServerError)]
    public async Task PostPayment_ReportsTheFailureAndStoresNothing_WhenTheBankDoesNotDecide(
        BankPaymentOutcome outcome,
        HttpStatusCode expectedStatusCode)
    {
        BankAnswers(outcome);

        HttpResponseMessage response = await PostPaymentAsync();

        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Null(_stored);
    }

    [Fact]
    public async Task PostPayment_Returns422AndNeverCallsTheBank_WhenValidationFails()
    {
        BankAnswers(BankPaymentOutcome.Authorized);

        HttpResponseMessage response =
            await PostPaymentAsync(new PostPaymentRequestBuilder().WithCvv("12").Build());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        _bank.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PostPayment_ReportsEveryBrokenRule_WhenSeveralAreInvalid()
    {
        PostPaymentRequest invalid = new PostPaymentRequestBuilder()
            .WithCardNumber("nope")
            .WithCvv("12")
            .WithCurrency("XYZ")
            .Build();

        HttpResponseMessage response = await PostPaymentAsync(invalid);
        ValidationProblemDetails? problem =
            await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(
            new[] { "CardNumber", "Currency", "Cvv" },
            problem!.Errors.Keys.OrderBy(key => key, StringComparer.Ordinal));
    }
    
    [Fact]
    public async Task PostPayment_NeverReturnsTheFullCardNumberOrCvv()
    {
        BankAnswers(BankPaymentOutcome.Authorized, "auth-code");

        string body = await (await PostPaymentAsync()).Content.ReadAsStringAsync();

        Assert.DoesNotContain(TestCard, body, StringComparison.Ordinal);
        Assert.DoesNotContain("\"123\"", body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("{ \"cardNumber\": \"2222405343248877\",")]                    // truncated JSON
    [InlineData("{\"cardNumber\":\"2222405343248877\",\"amount\":\"lots\"}")]  // wrong type for amount
    [InlineData("")]                                                           // no body at all
    public async Task PostPayment_Returns400_WhenTheBodyCannotBeParsed(string body)
    {
        BankAnswers(BankPaymentOutcome.Authorized);

        using StringContent content = new(body, Encoding.UTF8, "application/json");

        HttpResponseMessage response = await _client.PostAsync("/api/Payments", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _bank.VerifyNoOtherCalls();
        Assert.Null(_stored);
    }

    private void BankAnswers(BankPaymentOutcome outcome, string? authorizationCode = null)
    {
        _bank
            .Setup(client => client.AuthorizeAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankPaymentResult(outcome, authorizationCode));
    }

    private Task<HttpResponseMessage> PostPaymentAsync(PostPaymentRequest? request = null)
    {
        return _client.PostAsJsonAsync("/api/Payments", request ?? AValidRequest());
    }

    private static PostPaymentRequest AValidRequest()
    {
        return new PostPaymentRequestBuilder()
            .WithCardNumber(TestCard)
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
    }
}