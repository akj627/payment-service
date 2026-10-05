using System.Globalization;

namespace PaymentService.Core.Domain;

/// <summary>
/// An exact amount of money: a whole number of minor units plus a currency.
/// $12.50 is stored as 1250 USD cents.
///
/// We never use double for money, and we never round. An amount the currency
/// cannot represent (for example 10.005 USD) is rejected, not adjusted.
/// </summary>
public sealed record Money
{
    private Money(long amountInMinorUnits, Currency currency)
    {
        AmountInMinorUnits = amountInMinorUnits;
        Currency = currency;
    }

    public long AmountInMinorUnits { get; }

    public Currency Currency { get; }

    public bool IsPositive => AmountInMinorUnits > 0;

    public static Money FromMinorUnits(long amountInMinorUnits, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        return new Money(amountInMinorUnits, currency);
    }

    /// <summary>
    /// Parses an amount such as "1250.50". Only digits and an optional "." are allowed:
    /// no sign, no thousands separator, no exponent.
    /// </summary>
    public static bool TryParse(string? amount, Currency currency, out Money? money, out string? error)
    {
        ArgumentNullException.ThrowIfNull(currency);
        money = null;

        if (string.IsNullOrWhiteSpace(amount))
        {
            error = "Amount is required.";
            return false;
        }

        if (!decimal.TryParse(amount, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value))
        {
            error = $"Amount '{amount}' is not a valid number.";
            return false;
        }

        // Checked before multiplying so a huge input cannot overflow decimal.
        if (value > long.MaxValue)
        {
            error = $"Amount '{amount}' is too large.";
            return false;
        }

        decimal minorUnits = value * MinorUnitsPerUnit(currency);

        if (minorUnits != decimal.Truncate(minorUnits))
        {
            error = $"Amount '{amount}' has more than {currency.DecimalPlaces} decimal places, which {currency.Code} does not allow.";
            return false;
        }

        if (minorUnits > long.MaxValue)
        {
            error = $"Amount '{amount}' is too large.";
            return false;
        }

        money = new Money((long)minorUnits, currency);
        error = null;
        return true;
    }

    public Money Add(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (other.Currency != Currency)
        {
            throw new InvalidOperationException($"Cannot add {other.Currency.Code} to {Currency.Code}.");
        }

        return new Money(checked(AmountInMinorUnits + other.AmountInMinorUnits), Currency);
    }

    /// <summary>The amount with exactly the currency's decimal places, e.g. "1250.50".</summary>
    public string ToAmountString()
    {
        decimal value = AmountInMinorUnits / MinorUnitsPerUnit(Currency);
        return value.ToString("F" + Currency.DecimalPlaces, CultureInfo.InvariantCulture);
    }

    public override string ToString() => $"{ToAmountString()} {Currency.Code}";

    private static decimal MinorUnitsPerUnit(Currency currency)
    {
        decimal result = 1;
        for (int i = 0; i < currency.DecimalPlaces; i++)
        {
            result *= 10;
        }

        return result;
    }
}
