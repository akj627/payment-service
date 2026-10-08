using PaymentService.Core.Domain;

namespace PaymentService.Core.Batches;

/// <summary>
/// Checks the whole batch and reports every problem at once. If anything is wrong, nothing is accepted:
/// treasury teams fix the file and resubmit, rather than having half a payment run go out.
/// </summary>
public static class BatchRequestValidator
{
    public const int MaxPaymentsPerBatch = 1000;

    public static Dictionary<string, string[]> Validate(SubmitBatchRequest request, out List<NewPayment> payments)
    {
        var errors = new Dictionary<string, string[]>();
        payments = new List<NewPayment>();

        if (request.Payments == null || request.Payments.Count == 0)
        {
            errors["payments"] = ["A batch must contain at least one payment."];
            return errors;
        }

        if (request.Payments.Count > MaxPaymentsPerBatch)
        {
            errors["payments"] = [$"A batch can contain at most {MaxPaymentsPerBatch} payments."];
            return errors;
        }

        var seenReferences = new HashSet<string>();

        for (int i = 0; i < request.Payments.Count; i++)
        {
            string path = $"payments[{i}]";
            var item = request.Payments[i];

            if (item == null)
            {
                errors[path] = ["Payment is required."];
                continue;
            }

            var itemErrors = new List<(string Field, string Message)>();

            CheckText(item.Reference, "reference", 35, itemErrors);
            CheckText(item.BeneficiaryName, "beneficiaryName", 140, itemErrors);
            CheckText(item.BeneficiaryAccount, "beneficiaryAccount", 34, itemErrors);
            CheckText(item.BankPartner, "bankPartner", 50, itemErrors);

            if (item.Reference != null && !seenReferences.Add(item.Reference))
            {
                itemErrors.Add(("reference", $"Reference '{item.Reference}' appears more than once in the batch."));
            }

            Money? amount = null;
            if (!Currency.TryFromCode(item.Currency, out var currency))
            {
                itemErrors.Add(("currency", $"Currency '{item.Currency}' is not supported."));
            }
            else if (!Money.TryParse(item.Amount, currency!, out amount, out var amountError))
            {
                itemErrors.Add(("amount", amountError!));
            }
            else if (!amount!.IsPositive)
            {
                itemErrors.Add(("amount", "Amount must be greater than zero."));
            }

            foreach (var group in itemErrors.GroupBy(e => e.Field))
            {
                errors[$"{path}.{group.Key}"] = group.Select(e => e.Message).ToArray();
            }

            if (itemErrors.Count == 0)
            {
                payments.Add(new NewPayment(
                    item.Reference!,
                    amount!,
                    new Beneficiary(item.BeneficiaryName!, item.BeneficiaryAccount!),
                    item.BankPartner!));
            }
        }

        return errors;
    }

    private static void CheckText(string? value, string field, int maxLength, List<(string, string)> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add((field, $"{field} is required."));
        }
        else if (value.Length > maxLength)
        {
            errors.Add((field, $"{field} must be at most {maxLength} characters."));
        }
    }
}
