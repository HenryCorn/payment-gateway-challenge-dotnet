using FluentValidation;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Contracts.Merchant;
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

    [HttpGet("{id:guid}", Name = "GetPayment")]
    public ActionResult<PaymentResponse?> GetPaymentAsync(Guid id)
    {
        PaymentResponse? payment = _paymentsRepository.Get(id);
        
        if (payment is null)
        {
            return NotFound();
        }

        return new OkObjectResult(payment);
    }
}