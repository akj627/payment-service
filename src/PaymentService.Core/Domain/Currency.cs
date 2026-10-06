namespace PaymentService.Core.Domain;

/// <summary>
/// An ISO 4217 currency and the number of decimal places it allows (USD 2, JPY 0, BHD 3).
/// Only a fixed list is supported so a typo like "USX" can never reach a bank.
/// In production this list would come from reference data.
/// </summary>
public sealed record Currency
{
    private static readonly Dictionary<string, Currency> Supported = new()
    {
        ["USD"] = new Currency("USD", 2),
        ["EUR"] = new Currency("EUR", 2),
        ["GBP"] = new Currency("GBP", 2),
        ["CHF"] = new Currency("CHF", 2),
        ["CAD"] = new Currency("CAD", 2),
        ["SGD"] = new Currency("SGD", 2),
        ["INR"] = new Currency("INR", 2),
        ["JPY"] = new Currency("JPY", 0),
        ["BHD"] = new Currency("BHD", 3),
    };

    private Currency(string code, int decimalPlaces)
    {
        Code = code;
        DecimalPlaces = decimalPlaces;
    }

    public string Code { get; }

    public int DecimalPlaces { get; }

    /// <summary>
    /// Looks up a currency by its ISO code. Codes are case-sensitive: "usd" is rejected.
    /// </summary>
    public static bool TryFromCode(string? code, out Currency? currency)
    {
        if (code != null && Supported.TryGetValue(code, out var found))
        {
            currency = found;
            return true;
        }

        currency = null;
        return false;
    }

    public static Currency FromCode(string code)
    {
        if (!TryFromCode(code, out var currency))
        {
            throw new ArgumentException($"Currency '{code}' is not supported.", nameof(code));
        }

        return currency!;
    }

    public override string ToString() => Code;
}
