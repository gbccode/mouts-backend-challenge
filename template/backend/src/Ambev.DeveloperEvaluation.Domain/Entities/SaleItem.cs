using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Services;

namespace Ambev.DeveloperEvaluation.Domain.Entities
{
    public class SaleItem : BaseEntity
    {
        // Para EF
        private SaleItem() { }

        private SaleItem(Guid id, Guid saleId, Guid productId, string productName, int quantity, decimal unitPrice)
        {
            Id = id;
            SaleId = saleId;
            ProductId = productId;
            ProductName = productName;
            Quantity = quantity;
            UnitPrice = unitPrice;
            IsCancelled = false;

            // Calcula e popula campos monetários/percentuais
            var (rate, discount, total) = SalePricing.Calculate(quantity, unitPrice);
            DiscountRate = rate;
            DiscountAmount = discount;
            TotalAmount = total;
        }

        public static SaleItem Create(Guid? id, Guid saleId, Guid productId, string productName, int quantity, decimal unitPrice)
        {
            if (productId == Guid.Empty)
                throw new DomainException("ProductId is required.");

            if (string.IsNullOrWhiteSpace(productName))
                throw new DomainException("ProductName is required.");

            // SalePricing will validate quantity and unitPrice
            var assignedId = id == null || id == Guid.Empty ? Guid.NewGuid() : id.Value;

            return new SaleItem(assignedId, saleId, productId, productName.Trim(), quantity, unitPrice);
        }

        public void Update(string productName, int quantity, decimal unitPrice)
        {
            if (IsCancelled)
                throw new DomainException("Cannot update a cancelled item.");

            if (string.IsNullOrWhiteSpace(productName))
                throw new DomainException("ProductName is required.");

            // Validate via pricing calc before mutating
            var (rate, discount, total) = SalePricing.Calculate(quantity, unitPrice);

            ProductName = productName.Trim();
            Quantity = quantity;
            UnitPrice = unitPrice;
            DiscountRate = rate;
            DiscountAmount = discount;
            TotalAmount = total;
        }

        public Guid SaleId { get; private set; }
        public Guid ProductId { get; private set; }
        public string ProductName { get; private set; }
        public int Quantity { get; private set; }
        public decimal UnitPrice { get; private set; }
        public decimal DiscountRate { get; private set; }
        public decimal DiscountAmount { get; private set; }
        public decimal TotalAmount { get; private set; }
        public bool IsCancelled { get; private set; }

        internal bool MarkCancelled()
        {
            if (IsCancelled)
                return false;

            IsCancelled = true;
            // Preserve TotalAmount and DiscountAmount as per rules
            return true;
        }
    }
}