using System.Diagnostics.Metrics;

using PaymentGateway.Api.Domain;

namespace PaymentGateway.Api.Services;

/// <summary>
/// Metrics for the Payments API.
/// </summary>
public sealed class PaymentsMetrics
{
    public const string MeterName = "PaymentGateway.Api";

    private readonly Counter<long> _paymentsProcessed;

    public PaymentsMetrics(IMeterFactory meterFactory)
    {
        Meter meter = meterFactory.Create(MeterName);

        _paymentsProcessed = meter.CreateCounter<long>(
            "payments.processed",
            description: "Payments that reached a final status.");
    }

    public void RecordProcessed(PaymentStatus status, string currency)
    {
        _paymentsProcessed.Add(
            1,
            new KeyValuePair<string, object?>("payment.status", status.ToString()),
            new KeyValuePair<string, object?>("payment.currency", currency));
    }
}