using System.Diagnostics.Metrics;

using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.DependencyInjection;

using PaymentGateway.Api.Domain;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests.Services;

// AI Generated Tests
public class PaymentsMetricsTests : IDisposable
{
    private readonly ServiceProvider _services =
        new ServiceCollection().AddMetrics().BuildServiceProvider();

    private readonly PaymentsMetrics _metrics;
    private readonly MetricCollector<long> _collector;

    public PaymentsMetricsTests()
    {
        IMeterFactory meterFactory = _services.GetRequiredService<IMeterFactory>();

        _metrics = new PaymentsMetrics(meterFactory);
        _collector = new MetricCollector<long>(
            meterFactory, PaymentsMetrics.MeterName, "payments.processed");
    }

    [Fact]
    public void RecordProcessed_CountsOnePayment_WithItsStatusAndCurrency()
    {
        _metrics.RecordProcessed(PaymentStatus.Authorized, "GBP");

        CollectedMeasurement<long> measurement = Assert.Single(_collector.GetMeasurementSnapshot());

        Assert.Equal(1, measurement.Value);
        Assert.Equal("Authorized", measurement.Tags["payment.status"]);
        Assert.Equal("GBP", measurement.Tags["payment.currency"]);
    }

    [Fact]
    public void RecordProcessed_TagsNothingBeyondStatusAndCurrency()
    {
        _metrics.RecordProcessed(PaymentStatus.Declined, "USD");

        CollectedMeasurement<long> measurement = Assert.Single(_collector.GetMeasurementSnapshot());

        Assert.Equal(
            new[] { "payment.currency", "payment.status" },
            measurement.Tags.Keys.OrderBy(key => key, StringComparer.Ordinal));
    }

    public void Dispose()
    {
        _collector.Dispose();
        _services.Dispose();
    }
}