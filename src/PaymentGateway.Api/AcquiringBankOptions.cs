using System.ComponentModel.DataAnnotations;

namespace PaymentGateway.Api;

/// <summary>
/// Options for configuring the acquiring bank service, validated at startup.
/// </summary>
public sealed class AcquiringBankOptions
{
    public const string SectionName = "AcquiringBank";

    /// <summary>
    /// Gets or sets the base address of the acquiring bank service.
    /// </summary>
    [Required]
    public Uri? BaseAddress { get; set; }

    /// <summary>
    /// Gets or sets the deadline for a single attempt.
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:00.001", "00:01:00")]
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for transient failures.
    /// </summary>
    [Range(0, 5)]
    public int MaxRetryAttempts { get; set; } = 2;

    /// <summary>
    /// Gets or sets the minimum attempts before the circuit breaker opens.
    /// Polly requires at least 2 and validates it at pipeline-build time with
    /// an unhelpful message - this range turns that into a named config error.
    /// </summary>
    [Range(2, int.MaxValue)]
    public int CircuitBreakerMinimumThroughput { get; set; } = 4;
}