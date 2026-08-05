using FluentValidation;

using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Validation;

public class PostPaymentRequestValidator: AbstractValidator<PostPaymentRequest>
{
    private static readonly string[] SupportedCurrencies = ["EUR", "USD", "GBP"];
    
    private readonly TimeProvider _timeProvider;

    public PostPaymentRequestValidator(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;

        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(request => request.CardNumber)
            .NotEmpty().WithMessage("Card number is required")
            .Matches("^[0-9]{14,19}$").WithMessage("Card number must be 14-19 numeric characters.");
        
        RuleFor(request => request.ExpiryMonth)
            .NotNull().WithMessage("Expiry month is required")
            .InclusiveBetween(1,12).WithMessage("Expiry month must be between 1 and 12.");
        
        RuleFor(request => request.ExpiryYear)
            .NotNull().WithMessage("Expiry year is required")
            .InclusiveBetween(1, 9999).WithMessage("Expiry year must be a valid year.");
        
        RuleFor(request => request)
            .Must(NotBeExpired)
            .OverridePropertyName("Expiry")
            .WithMessage("Card expiry must not be in the past.");

        RuleFor(request => request.Currency)
            .NotEmpty().WithMessage("Currency is required")
            .Length(3).WithMessage("Currency must be a 3-letter ISO code.")
            .Must(IsSupported).WithMessage("Currency is not supported");
        
        RuleFor(request => request.Amount)
            .NotNull().WithMessage("Amount is required.")
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(request => request.Cvv)
            .NotEmpty().WithMessage("CVV is required")
            .Matches("^[0-9]{3,4}$").WithMessage("CVV must be 3 or 4 numeric characters.");
    }
    
    private bool NotBeExpired(PostPaymentRequest request)
    {
        if (request.ExpiryMonth is null || request.ExpiryYear is null)
        {
            return true;
        }
        
        int month = request.ExpiryMonth.Value;
        int year = request.ExpiryYear.Value;

        if (month is < 1 or > 12 || year is < 1 or > 9999)
        {
            return true;
        }

        DateTimeOffset now = _timeProvider.GetUtcNow();

        int expiry = (year * 100) + month;
        int currentMonth = (now.Year * 100) + now.Month;

        return expiry > currentMonth;
    }
    
    private static bool IsSupported(string? currency)
    {
        return SupportedCurrencies.Any(code => string.Equals(code, currency, StringComparison.OrdinalIgnoreCase));
    }
}