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

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentsController"/> class.
    /// </summary>
    /// <param name="paymentsRepository"> The repository for storing and retrieving payment responses.</param>
    /// <param name="paymentValidator"> The validator for validating incoming payment requests.</param>
    /// <param name="acquiringBankClient"> The client for communicating with the acquiring bank.</param>
    public PaymentsController(
        IPaymentsRepository paymentsRepository,
        IValidator<PostPaymentRequest> paymentValidator,
        IAcquiringBankClient acquiringBankClient)
    {
        _paymentsRepository = paymentsRepository;
        _paymentValidator = paymentValidator;
        _acquiringBankClient = acquiringBankClient;
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

            return ValidationProblem(statusCode: StatusCodes.Status422UnprocessableEntity);
        }
        
        Payment payment = Payment.FromValidatedRequest(paymentRequest);
        
        BankPaymentResult bankPaymentResult = await _acquiringBankClient.AuthorizeAsync(payment, cancellationToken);

        switch (bankPaymentResult.Outcome)
        {
            case BankPaymentOutcome.Authorized:
            case BankPaymentOutcome.Declined:
                PaymentStatus status = bankPaymentResult.Outcome == BankPaymentOutcome.Authorized
                    ? PaymentStatus.Authorized
                    : PaymentStatus.Declined;
                PaymentResponse response = payment.ToResponse(Guid.NewGuid(), status);
                _paymentsRepository.AddPayment(response);
                
                return CreatedAtRoute("GetPayment", new { id = response.Id }, response);
            case BankPaymentOutcome.Unavailable:
                return Problem(
                    statusCode: StatusCodes.Status502BadGateway,
                    title: "The acquiring bank is unavailable",
                    detail: "The payment was not processed. Please retry later.");

            case BankPaymentOutcome.InvalidRequest:
            default:
                return Problem(statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}