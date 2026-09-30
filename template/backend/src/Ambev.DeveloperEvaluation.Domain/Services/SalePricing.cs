namespace Ambev.DeveloperEvaluation.Domain.Services
{
    public class SalePricing
    {
        public static (decimal Rate, decimal Discount, decimal Total) Calculate(int quantity, decimal unitPrice)
        {
            if (quantity is < 1 or > 20)
                throw new DomainException("Quantity must be between 1 and 20.");

            if (unitPrice <= 0)
                throw new DomainException("Unit price must be positive.");

            if (decimal.Round(unitPrice, 2) != unitPrice)
                throw new DomainException("Unit price supports at most two decimals.");

            var rate = quantity >= 10 ? 0.20m : quantity >= 4 ? 0.10m : 0m;
            var gross = quantity * unitPrice;
            var discount = decimal.Round(
                gross * rate, 2, MidpointRounding.AwayFromZero);

            return (rate, discount, gross - discount);
        }
    }
}
