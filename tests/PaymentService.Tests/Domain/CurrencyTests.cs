using PaymentService.Core.Domain;

namespace PaymentService.Tests.Domain;

public class CurrencyTests
{
    [Theory]
    [InlineData("USD", 2)]
    [InlineData("JPY", 0)]
    [InlineData("BHD", 3)]
    public void FromCode_returns_currency_with_correct_decimal_places(string code, int expectedDecimalPlaces)
    {
        var currency = Currency.FromCode(code);

        Assert.Equal(code, currency.Code);
        Assert.Equal(expectedDecimalPlaces, currency.DecimalPlaces);
    }

    [Theory]
    [InlineData("usd")]
    [InlineData("USX")]
    [InlineData("")]
    [InlineData(null)]
    public void TryFromCode_rejects_unknown_codes(string? code)
    {
        bool found = Currency.TryFromCode(code, out var currency);

        Assert.False(found);
        Assert.Null(currency);
    }

    [Fact]
    public void Same_code_returns_same_instance()
    {
        Assert.Same(Currency.FromCode("EUR"), Currency.FromCode("EUR"));
    }
}
