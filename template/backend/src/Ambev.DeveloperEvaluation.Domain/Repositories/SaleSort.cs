namespace Ambev.DeveloperEvaluation.Domain.Repositories;

public record SaleSort(string Field, bool Descending)
{
    public static IReadOnlyList<SaleSort> Parse(string? order)
    {
        var fields = new HashSet<string>
        {
            "id", "salenumber", "saledate", "customername", "branchname", "totalamount", "iscancelled"
        };
        var result = new List<SaleSort>();
        foreach (var term in (string.IsNullOrWhiteSpace(order) ? "saleDate desc" : order).Split(','))
        {
            var parts = term.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length is < 1 or > 2 || !fields.Contains(parts[0].ToLowerInvariant())
                || (parts.Length == 2 && !parts[1].Equals("asc", StringComparison.OrdinalIgnoreCase)
                    && !parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase)))
            {
                throw new DomainException("Invalid _order. Use saleNumber, saleDate, customerName, branchName, totalAmount, isCancelled or id, followed by asc or desc.");
            }
            result.Add(new SaleSort(parts[0].ToLowerInvariant(),
                parts.Length == 2 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase)));
        }
        return result;
    }
}
