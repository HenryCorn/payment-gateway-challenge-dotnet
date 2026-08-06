using System.Diagnostics.Metrics;

using PaymentGateway.Api.Domain;

namespace PaymentGateway.Api.Services;

/// <summary>
/// Metrics for the Payments API.
/// </summary>
public sealed class PaymentsMetrics
{
    public const string MeterName = "PaymentGateway.Api";

    private readonly Counter<long> _authorizations;

    public PaymentsMetrics(IMeterFactory meterFactory)
    {
        Meter meter = meterFactory.Create(MeterName);

        _authorizations = meter.CreateCounter<long>(
            "payments.authorizations",
            description: "Authorization attempts that reached the bank, by outcome.");
    }

    /// <summary>
    /// Counts one authorization attempt. Every outcome is recorded, not just the
    /// ones that created a payment - a rising Unavailable count is the signal
    /// that the bank is down, and it is the reason to alert.
    /// </summary>
    public void RecordAuthorization(BankPaymentOutcome outcome, string currency)
    {
        _authorizations.Add(
            1,
            new KeyValuePair<string, object?>("payment.outcome", outcome.ToString()),
            new KeyValuePair<string, object?>("payment.currency", currency));
    }
}