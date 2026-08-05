using PaymentGateway.Api.Contracts.Merchant;

namespace PaymentGateway.Api.TestUtils.Builders;

/// <summary>
/// Builds a request that is valid against PostPaymentRequestValidator.
/// </summary>
public class PostPaymentRequestBuilder
{
    private string? _cardNumber = "4111222233334443";
    private int? _expiryMonth = 12;
    private int? _expiryYear = 2030;
    private string? _currency = "EUR";
    private int? _amount = 100;
    private string? _cvv = "123";

    public PostPaymentRequestBuilder WithCardNumber(string? cardNumber)
    {
        _cardNumber = cardNumber;
        return this;
    }

    public PostPaymentRequestBuilder WithExpiry(int? month, int? year)
    {
        _expiryMonth = month;
        _expiryYear = year;
        return this;
    }

    public PostPaymentRequestBuilder WithCurrency(string? currency)
    {
        _currency = currency;
        return this;
    }

    public PostPaymentRequestBuilder WithAmount(int? amount)
    {
        _amount = amount;
        return this;
    }

    public PostPaymentRequestBuilder WithCvv(string? cvv)
    {
        _cvv = cvv;
        return this;
    }

    public PostPaymentRequest Build()
    {
        return new PostPaymentRequest
        {
            CardNumber = _cardNumber,
            ExpiryMonth = _expiryMonth,
            ExpiryYear = _expiryYear,
            Currency = _currency,
            Amount = _amount,
            Cvv = _cvv
        };
    }
}