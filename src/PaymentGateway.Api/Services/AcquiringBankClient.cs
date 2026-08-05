using System.Net;

using PaymentGateway.Api.Contracts.AcquiringBank;
using PaymentGateway.Api.Domain;

using Polly.CircuitBreaker;
using Polly.Timeout;

namespace PaymentGateway.Api.Services;

internal sealed class AcquiringBankClient : IAcquiringBankClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AcquiringBankClient> _logger;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AcquiringBankClient"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to communicate with the acquiring bank.</param>
    /// <param name="logger">The logger used for logging information and errors.</param>
    public AcquiringBankClient(HttpClient httpClient, ILogger<AcquiringBankClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    /// <inheritdoc/>
        public async Task<BankPaymentResult> AuthorizeAsync(Payment payment, CancellationToken cancellationToken)
    {
        BankAuthorizationRequest request = BankAuthorizationRequest.FromValidatedPayment(payment);

        try
        {
            using HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync("/payments", request, cancellationToken);

            return await InterpretAsync(response, payment, cancellationToken);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning(
                "Acquiring bank circuit is open; payment for card ****{LastFourCardDigits} was not attempted.",
                payment.LastFourCardDigits);

            return new BankPaymentResult(BankPaymentOutcome.Unavailable);
        }
        catch (TimeoutRejectedException)
        {
            _logger.LogWarning(
                "Acquiring bank did not answer within {Timeout} for card ****{LastFourCardDigits}. "
                + "The payment may or may not have been taken, so it is deliberately not retried.",
                _httpClient.Timeout,
                payment.LastFourCardDigits);

            return new BankPaymentResult(BankPaymentOutcome.Unavailable);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(
                exception,
                "Acquiring bank was unreachable ({HttpRequestError}) for card ****{LastFourCardDigits}.",
                exception.HttpRequestError,
                payment.LastFourCardDigits);

            return new BankPaymentResult(BankPaymentOutcome.Unavailable);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Acquiring bank call was abandoned for card ****{LastFourCardDigits}.",
                payment.LastFourCardDigits);

            return new BankPaymentResult(BankPaymentOutcome.Unavailable);
        }
    }
    
    private async Task<BankPaymentResult> InterpretAsync(
        HttpResponseMessage response,
        Payment payment,
        CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            _logger.LogWarning(
                "Acquiring bank returned 503 for card ****{LastFourCardDigits} after exhausting retries.",
                payment.LastFourCardDigits);

            return new BankPaymentResult(BankPaymentOutcome.Unavailable);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            _logger.LogError(
                "Acquiring bank rejected the request as malformed for card ****{LastFourCardDigits}.",
                payment.LastFourCardDigits);

            return new BankPaymentResult(BankPaymentOutcome.InvalidRequest);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Acquiring bank returned an unexpected {StatusCode} for card ****{LastFourCardDigits}.",
                (int)response.StatusCode,
                payment.LastFourCardDigits);

            return new BankPaymentResult(BankPaymentOutcome.Unavailable);
        }

        BankAuthorizationResponse? body =
            await response.Content.ReadFromJsonAsync<BankAuthorizationResponse>(cancellationToken);

        if (body is null)
        {
            _logger.LogWarning(
                "Acquiring bank returned an empty body for card ****{LastFourCardDigits}.",
                payment.LastFourCardDigits);

            return new BankPaymentResult(BankPaymentOutcome.Unavailable);
        }

        return body.Authorized
            ? new BankPaymentResult(BankPaymentOutcome.Authorized, body.AuthorizationCode)
            : new BankPaymentResult(BankPaymentOutcome.Declined);
    }
}