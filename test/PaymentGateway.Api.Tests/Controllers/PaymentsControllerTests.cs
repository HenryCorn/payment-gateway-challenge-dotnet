using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.Contracts.Merchant;
using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Domain;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests.Controllers;

public class PaymentsControllerTests
{
    [Fact]
    public async Task RetrievesAPaymentSuccessfully()
    {
        // Arrange
        PaymentResponse payment = new(
            Id: Guid.NewGuid(),
            Status: PaymentStatus.Authorized,
            CardNumberLastFour: "8877",
            ExpiryMonth: 12,
            ExpiryYear: 2025,
            Currency: "USD",
            Amount: 1000
        );

        PaymentsRepository paymentsRepository = new();
        paymentsRepository.Add(payment);

        WebApplicationFactory<PaymentsController> webApplicationFactory = new();
        HttpClient client = webApplicationFactory
            .WithWebHostBuilder(builder =>
                builder.ConfigureServices(services => services.AddSingleton(paymentsRepository)))
            .CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/Payments/{payment.Id}");
        PaymentResponse? paymentResponse = await response.Content.ReadFromJsonAsync<PaymentResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal(payment.Id, paymentResponse.Id);
        Assert.Equal("8877", paymentResponse.CardNumberLastFour);
        Assert.Equal(PaymentStatus.Authorized, paymentResponse.Status);
    }

    [Fact]
    public async Task Returns404IfPaymentNotFound()
    {
        // Arrange
        WebApplicationFactory<PaymentsController> webApplicationFactory = new();
        HttpClient client = webApplicationFactory.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}