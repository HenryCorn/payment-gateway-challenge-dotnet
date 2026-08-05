using System.Net;

using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

using PaymentGateway.Api.Services;

using Polly;
using Polly.Timeout;

namespace PaymentGateway.Api.Extensions;

public static class AcquiringBankServiceCollectionExtensions
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan BreakerSamplingDuration = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan BreakerBreakDuration = TimeSpan.FromSeconds(5);
    private const double BreakerFailureRatio = 0.5;

    public static IServiceCollection AddAcquiringBank(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<AcquiringBankOptions>()
            .Bind(configuration.GetSection(AcquiringBankOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddHttpClient<IAcquiringBankClient, AcquiringBankClient>((serviceProvider, httpClient) =>
            {
                AcquiringBankOptions options =
                    serviceProvider.GetRequiredService<IOptions<AcquiringBankOptions>>().Value;

                httpClient.BaseAddress = options.BaseAddress;

                // The resilience pipeline owns the deadline. Leaving HttpClient's
                // own 100-second timeout in place would give two competing
                // deadlines, and the wrong one would surface as
                // TaskCanceledException instead of TimeoutRejectedException.
                httpClient.Timeout = Timeout.InfiniteTimeSpan;
            })
            .AddResilienceHandler("acquiring-bank", ConfigurePipeline);

        return services;
    }

    private static void ConfigurePipeline(
        ResiliencePipelineBuilder<HttpResponseMessage> pipeline,
        ResilienceHandlerContext context)
    {
        AcquiringBankOptions options =
            context.ServiceProvider.GetRequiredService<IOptions<AcquiringBankOptions>>().Value;

        // Order is outermost first. Retry wraps the breaker wraps the timeout -
        // see the note below for why the timeout is innermost.
        if (options.MaxRetryAttempts > 0)
        {
            pipeline.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = options.MaxRetryAttempts,
                Delay = RetryDelay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,

                // Deliberately narrower than the default predicate. A retry is
                // only safe where the bank demonstrably did not process the
                // request: it offers no idempotency key, so a second attempt at
                // a request it already received is a second charge.
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(static response => response.StatusCode == HttpStatusCode.ServiceUnavailable)
                    .Handle<HttpRequestException>(NeverReachedTheBank)
            });
        }

        pipeline
            .AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = BreakerFailureRatio,
                MinimumThroughput = options.CircuitBreakerMinimumThroughput,
                SamplingDuration = BreakerSamplingDuration,
                BreakDuration = BreakerBreakDuration,

                // Wider than the retry predicate on purpose. A bank that keeps
                // timing out is unhealthy and should trip the breaker, even
                // though an individual timeout must not be retried.
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .HandleResult(static response => response.StatusCode == HttpStatusCode.ServiceUnavailable)
                    .Handle<HttpRequestException>()
                    .Handle<TimeoutRejectedException>()
            })
            .AddTimeout(options.RequestTimeout);
    }

    /// <summary>
    /// True only when the failure happened before a request could be delivered.
    /// Anything else may have been received and processed by the bank, and a
    /// retry would risk a second charge.
    /// </summary>
    private static bool NeverReachedTheBank(HttpRequestException exception)
    {
        return exception.HttpRequestError is HttpRequestError.ConnectionError
            or HttpRequestError.NameResolutionError
            or HttpRequestError.ProxyTunnelError
            or HttpRequestError.SecureConnectionError;
    }
}