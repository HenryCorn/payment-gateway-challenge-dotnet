using System.Net;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using PaymentGateway.Api.Extensions;
using PaymentGateway.Api.Services;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace PaymentGateway.Api.IntegrationTests.Services;

/// <summary>
/// Uses WireMock to simulate the acquiring bank. AI Usage to generate this.
/// </summary>
internal sealed class AcquiringBankTestHost : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public AcquiringBankTestHost(params (string Key, string Value)[] overrides)
    {
        Bank = WireMockServer.Start();

        Dictionary<string, string?> settings = new()
        {
            ["AcquiringBank:BaseAddress"] = Bank.Url,
            ["AcquiringBank:RequestTimeout"] = "00:00:05",
            ["AcquiringBank:MaxRetryAttempts"] = "2",
            ["AcquiringBank:CircuitBreakerMinimumThroughput"] = "1000"
        };

        foreach ((string key, string value) in overrides)
        {
            settings[key] = value;
        }

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        ServiceCollection services = new();
        services.AddFakeLogging();
        services.AddAcquiringBank(configuration);

        _serviceProvider = services.BuildServiceProvider();

        Client = _serviceProvider.GetRequiredService<IAcquiringBankClient>();
        Logs = _serviceProvider.GetFakeLogCollector();
    }

    public WireMockServer Bank { get; }

    public IAcquiringBankClient Client { get; }

    public FakeLogCollector Logs { get; }

    public int RequestCount => Bank.LogEntries.Count();

    public string LoggedText => string.Join('\n', Logs.GetSnapshot().Select(entry => entry.Message));

    public void StubAuthorized(string authorizationCode)
    {
        StubJson(HttpStatusCode.OK, new { authorized = true, authorization_code = authorizationCode });
    }

    public void StubDeclined()
    {
        StubJson(HttpStatusCode.OK, new { authorized = false, authorization_code = "" });
    }

    public void StubStatus(HttpStatusCode statusCode)
    {
        StubJson(statusCode, new { });
    }

    public void StubJson(HttpStatusCode statusCode, object body)
    {
        Bank
            .Given(Request.Create().WithPath("/payments").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(statusCode).WithBodyAsJson(body));
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        Bank.Stop();
        Bank.Dispose();
    }
}