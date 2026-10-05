using PaymentService.Core.Domain;

namespace PaymentService.Tests.Domain;

public class MoneyTests
{
    private static readonly Currency Usd = Currency.FromCode("USD");
    private static readonly Currency Jpy = Currency.FromCode("JPY");
    private static readonly Currency Bhd = Currency.FromCode("BHD");

    [Theory]
    [InlineData("1250.50", 125050)]
    [InlineData("1250.5", 125050)]
    [InlineData("1250", 125000)]
    [InlineData("0.01", 1)]
    [InlineData("0", 0)]
    public void TryParse_converts_usd_to_cents_exactly(string amount, long expectedCents)
    {
        bool ok = Money.TryParse(amount, Usd, out var money, out var error);

        Assert.True(ok, error);
        Assert.Equal(expectedCents, money!.AmountInMinorUnits);
        Assert.Equal(Usd, money.Currency);
    }

    [Fact]
    public void TryParse_respects_currency_decimal_places()
    {
        Assert.True(Money.TryParse("1500", Jpy, out var yen, out _));
        Assert.Equal(1500, yen!.AmountInMinorUnits);

        Assert.True(Money.TryParse("12.345", Bhd, out var dinar, out _));
        Assert.Equal(12345, dinar!.AmountInMinorUnits);
    }

    [Theory]
    [InlineData("10.005")] // too many decimal places for USD: rejected, never rounded
    [InlineData("-10.00")] // sign not allowed
    [InlineData("1,000.00")] // thousands separator not allowed
    [InlineData("1e3")] // exponent not allowed
    [InlineData("abc")]
    [InlineData("")]
    [InlineData(null)]
    public void TryParse_rejects_invalid_usd_amounts(string? amount)
    {
        bool ok = Money.TryParse(amount, Usd, out var money, out var error);

        Assert.False(ok);
        Assert.Null(money);
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void TryParse_rejects_decimals_for_jpy()
    {
        Assert.False(Money.TryParse("100.5", Jpy, out _, out _));
    }

    [Fact]
    public void TryParse_rejects_amount_too_large_for_long()
    {
        Assert.False(Money.TryParse("99999999999999999999", Usd, out _, out var error));
        Assert.Contains("too large", error);
    }

    [Fact]
    public void Add_sums_amounts_in_same_currency()
    {
        var total = Money.FromMinorUnits(1050, Usd).Add(Money.FromMinorUnits(250, Usd));

        Assert.Equal(1300, total.AmountInMinorUnits);
        Assert.Equal(Usd, total.Currency);
    }

    [Fact]
    public void Add_refuses_to_mix_currencies()
    {
        var dollars = Money.FromMinorUnits(100, Usd);
        var yen = Money.FromMinorUnits(100, Jpy);

        Assert.Throws<InvalidOperationException>(() => dollars.Add(yen));
    }

    [Theory]
    [InlineData(125050, "USD", "1250.50 USD")]
    [InlineData(5, "USD", "0.05 USD")]
    [InlineData(1500, "JPY", "1500 JPY")]
    [InlineData(12345, "BHD", "12.345 BHD")]
    public void ToString_shows_exact_decimal_places(long minorUnits, string currencyCode, string expected)
    {
        var money = Money.FromMinorUnits(minorUnits, Currency.FromCode(currencyCode));

        Assert.Equal(expected, money.ToString());
    }

    [Fact]
    public void Equal_amount_and_currency_are_equal()
    {
        Assert.Equal(Money.FromMinorUnits(100, Usd), Money.FromMinorUnits(100, Usd));
        Assert.NotEqual(Money.FromMinorUnits(100, Usd), Money.FromMinorUnits(101, Usd));
    }
}
