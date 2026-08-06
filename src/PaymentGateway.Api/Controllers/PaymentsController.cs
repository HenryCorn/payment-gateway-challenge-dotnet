using System.Diagnostics;

using FluentValidation;
using FluentValidation.Results;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Contracts.Merchant;
using PaymentGateway.Api.Domain;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly IPaymentsRepository _paymentsRepository;
    private readonly IValidator<PostPaymentRequest> _paymentValidator;
    private readonly IAcquiringBankClient _acquiringBankClient;
    private readonly PaymentsMetrics _paymentsMetrics;
    private readonly ILogger<PaymentsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentsController"/> class.
    /// </summary>
    /// <param name="paymentsRepository"> The repository for storing and retrieving payment responses.</param>
    /// <param name="paymentValidator"> The validator for validating incoming payment requests.</param>
    /// <param name="acquiringBankClient"> The client for communicating with the acquiring bank.</param>
    /// <param name="paymentsMetrics"> The counter for payments that reached a final status.</param>
    public PaymentsController(
        IPaymentsRepository paymentsRepository,
        IValidator<PostPaymentRequest> paymentValidator,
        IAcquiringBankClient acquiringBankClient,
        PaymentsMetrics paymentsMetrics,
        ILogger<PaymentsController> logger)
    {
        _paymentsRepository = paymentsRepository;
        _paymentValidator = paymentValidator;
        _acquiringBankClient = acquiringBankClient;
        _paymentsMetrics = paymentsMetrics;
        _logger = logger;
    }
    
    /// <summary>
    /// Retrieves a payment response by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the payment response to retrieve.</param>
    /// <returns> An <see cref="ActionResult"/> containing the payment response if found; otherwise, a NotFound result.</returns>

    [HttpGet("{id:guid}", Name = "GetPayment")]
    public ActionResult<PaymentResponse?> GetPayment(Guid id)
    {
        PaymentResponse? payment = _paymentsRepository.GetPayment(id);
        
        if (payment is null)
        {
            return NotFound();
        }

        return new OkObjectResult(payment);
    }

    /// <summary>
    /// Processes a payment request by validating it, attempting to authorize it with the acquiring bank, and storing the result in the repository.
    /// </summary>
    /// <param name="paymentRequest">The payment request to be processed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns> An <see cref="ActionResult"/> containing the payment response if successful; otherwise, a BadRequest result.</returns>
    [HttpPost]
    public async Task<ActionResult<PaymentResponse>> PostPaymentAsync(
        PostPaymentRequest paymentRequest,
        CancellationToken cancellationToken)
    {
        if (paymentRequest is null || !ModelState.IsValid)
        {
            return ValidationProblem(statusCode: StatusCodes.Status400BadRequest);
        }

        ValidationResult validationResult = await _paymentValidator.ValidateAsync(paymentRequest, cancellationToken);
        if (!validationResult.IsValid)
        {
            foreach (ValidationFailure failure in validationResult.Errors)
            {
                ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }

            _logger.LogInformation(
                "Payment request rejected by validation. Fields: {InvalidFields}.",
                validationResult.Errors.Select(error => error.PropertyName).Distinct());

            return ValidationProblem(statusCode: StatusCodes.Status422UnprocessableEntity);
        }
        
        Payment payment = Payment.FromValidatedRequest(paymentRequest);
        
        BankPaymentResult bankPaymentResult = await _acquiringBankClient.AuthorizeAsync(payment, cancellationToken);

        _paymentsMetrics.RecordAuthorization(bankPaymentResult.Outcome, payment.Currency);

        switch (bankPaymentResult.Outcome)
        {
            case BankPaymentOutcome.Authorized:
            case BankPaymentOutcome.Declined:
                PaymentStatus status = bankPaymentResult.Outcome == BankPaymentOutcome.Authorized
                    ? PaymentStatus.Authorized
                    : PaymentStatus.Declined;
                PaymentResponse response = payment.ToResponse(Guid.NewGuid(), status);
                _paymentsRepository.AddPayment(response);
                
                Activity.Current?.SetTag("payment.status", status.ToString());

                _logger.LogInformation(
                    "Payment {PaymentId} {PaymentStatus} for {Currency} {Amount} on card ****{LastFourCardDigits}.",
                    response.Id,
                    status,
                    payment.Currency,
                    payment.Amount,
                    payment.LastFourCardDigits);

                return CreatedAtRoute("GetPayment", new { id = response.Id }, response);
            case BankPaymentOutcome.Unavailable:
                _logger.LogWarning(
                    "Payment not processed: the acquiring bank was unavailable. Card ****{LastFourCardDigits}.",
                    payment.LastFourCardDigits);

                return Problem(
                    statusCode: StatusCodes.Status502BadGateway,
                    title: "The acquiring bank is unavailable",
                    detail: "The payment was not processed. Please retry later.");

            case BankPaymentOutcome.InvalidRequest:
            default:
                _logger.LogError(
                    "The acquiring bank rejected our request as invalid. This is a defect in this gateway, "
                    + "not a problem with the payment. Card ****{LastFourCardDigits}.",
                    payment.LastFourCardDigits);

                return Problem(statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}