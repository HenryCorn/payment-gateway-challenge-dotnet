using FluentValidation.Results;

using Microsoft.Extensions.Time.Testing;

using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.TestUtils.Builders;
using PaymentGateway.Api.Validation;

namespace PaymentGateway.Api.Tests.Validation;

public class PostPaymentRequestValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 4, 12, 0, 0, TimeSpan.Zero);

    private readonly PostPaymentRequestValidator _validator = new(new FakeTimeProvider(Now));

    [Theory]
    [InlineData(13, false)]
    [InlineData(14, true)]
    [InlineData(19, true)]
    [InlineData(20, false)]
    public void Validator_Handles_CardNumbersLength(int length, bool expectedValid)
    {
        PostPaymentRequest request = new PostPaymentRequestBuilder()
            .WithCardNumber(AuthorisedCardNumberOfLength(length))
            .Build();
        
        ValidationResult result = _validator.Validate(request);
        
        Assert.Equal(expectedValid, result.IsValid);
    }
    
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123456789012e3")]
    [InlineData("2222 4053 4324 8877")]
    public void Validator_Rejects_WhenCardNumberIsNotSpecifiedLengthOfDigits(string? cardNumber)
    {
        PostPaymentRequest request = new PostPaymentRequestBuilder()
            .WithCardNumber(cardNumber)
            .Build();

        AssertFailedOn(_validator.Validate(request), nameof(PostPaymentRequest.CardNumber));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(12, true)]
    [InlineData(13, false)]
    public void Validator_Handles_ExpirationMonthBoundaries(int month, bool expectedValid)
    {
        PostPaymentRequest request = new PostPaymentRequestBuilder().WithExpiry(month, Now.Year + 1).Build();
        
        ValidationResult result = _validator.Validate(request);
        
        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData(7, 2026, false)]
    [InlineData(8, 2026, false)]
    [InlineData(9, 2026, true)]
    [InlineData(12, 2025, false)]
    [InlineData(1, 2027, true)]
    public void Validator_Handles_FullExpirationDate(int month, int year, bool expectedValid)
    {
        PostPaymentRequest request = new PostPaymentRequestBuilder().WithExpiry(month, year).Build();
        
        ValidationResult result = _validator.Validate(request);
        
        Assert.Equal(expectedValid, result.IsValid);
    }
    
    [Fact]
    public void Validator_WhenMissingExpiryMonth_ReportsOnceAsRequiredNotAlsoAsExpired()
    {
        PostPaymentRequest request = new PostPaymentRequestBuilder().WithExpiry(null, 2030).Build();

        ValidationResult result = _validator.Validate(request);

        AssertFailedOn(result, nameof(PostPaymentRequest.ExpiryMonth));
        Assert.Single(result.Errors);
    }
    
    [Theory]
    [InlineData("GBP", true)]
    [InlineData("USD", true)]
    [InlineData("EUR", true)]
    [InlineData("gbp", true)]
    [InlineData("GB", false)]
    [InlineData("GBPP", false)]
    [InlineData("JPY", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Validator_Handles_CurrencyRules(string? currency, bool expectedValid)
    {
        PostPaymentRequest request = new PostPaymentRequestBuilder().WithCurrency(currency).Build();

        ValidationResult result = _validator.Validate(request);

        Assert.Equal(expectedValid, result.IsValid);
    }
    
    [Theory]
    [InlineData(null, false)]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(1050, true)]
    public void Validator_Handles_AmountRules(int? amount, bool expectedValid)
    {
        PostPaymentRequest request = new PostPaymentRequestBuilder().WithAmount(amount).Build();

        ValidationResult result = _validator.Validate(request);

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Theory]
    [InlineData("12", false)]
    [InlineData("123", true)]
    [InlineData("1234", true)]
    [InlineData("12345", false)]
    [InlineData("0123", true)]
    [InlineData("12a", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Validator_Handles_CvvRules(string? cvv, bool expectedValid)
    {
        PostPaymentRequest request = new PostPaymentRequestBuilder().WithCvv(cvv).Build();

        ValidationResult result = _validator.Validate(request);

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact]
    public void Validator_ReportsNoErrorsForAValidRequest()
    {
        ValidationResult result = _validator.Validate(new PostPaymentRequestBuilder().Build());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validator_ReportsAllErrorsForAnInvalidRequest()
    {
        PostPaymentRequest request = new PostPaymentRequestBuilder()
            .WithCardNumber("123")
            .WithExpiry(13, 2020)
            .WithCurrency("XXX")
            .WithAmount(0)
            .WithCvv("ab")
            .Build();

        ValidationResult result = _validator.Validate(request);

        Assert.False(result.IsValid);

        string[] failedProperties = result.Errors.Select(error => error.PropertyName).ToArray();

        Assert.Equal(5, failedProperties.Length);
        Assert.Equal(failedProperties.Length, failedProperties.Distinct().Count());
        Assert.Contains(nameof(PostPaymentRequest.CardNumber), failedProperties);
        Assert.Contains(nameof(PostPaymentRequest.ExpiryMonth), failedProperties);
        Assert.Contains(nameof(PostPaymentRequest.Currency), failedProperties);
        Assert.Contains(nameof(PostPaymentRequest.Amount), failedProperties);
        Assert.Contains(nameof(PostPaymentRequest.Cvv), failedProperties);
    }
    
    private static string AuthorisedCardNumberOfLength(int length)
    {
        return new string('4', length - 1) + '7';
    }
    
    private static void AssertFailedOn(ValidationResult result, string propertyName)
    {
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
    }
}