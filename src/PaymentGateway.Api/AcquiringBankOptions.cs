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
    public Uri? BaseAddress { get; set; }
    
    /// <summary>
    /// Gets or sets the deadline for a single attempt.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(5);
    
    /// <summary>
    /// Gets or sets the maximum number of retry attempts for transient failures.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
    
    /// <summary>
    /// Gets or sets the minimum attempts before the circuit breaker opens.
    /// </summary>
    public int CircuitBreakerMinimumThroughput { get; set; } = 4;
}