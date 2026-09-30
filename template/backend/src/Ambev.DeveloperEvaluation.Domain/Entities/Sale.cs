using Ambev.DeveloperEvaluation.Domain.Common;

namespace Ambev.DeveloperEvaluation.Domain.Entities
{
    public class Sale : BaseEntity
    {
        private readonly List<SaleItem> _items = new();

        // Para EF
        private Sale() { }

        private Sale(Guid id, DateTime saleDate, Guid customerId, string customerName, Guid branchId, string branchName, IEnumerable<SaleItem>? items = null)
        {
            Id = id;
            SaleDate = saleDate;
            CustomerId = customerId;
            CustomerName = customerName;
            BranchId = branchId;
            BranchName = branchName;
            SaleNumber = $"SALE-{id.ToString("N")}";
            IsCancelled = false;

            if (items != null)
            {
                foreach (var it in items)
                {
                    // garantir vínculo
                    if (it.SaleId != id)
                        throw new DomainException("Item saleId mismatch.");
                    _items.Add(it);
                }
            }

            RecalculateTotal();
        }

        public static Sale Create(DateTime saleDate, Guid customerId, string customerName, Guid branchId, string branchName, IEnumerable<SaleItem>? items = null)
        {
            if (saleDate == default)
                throw new DomainException("SaleDate is required.");

            if (customerId == Guid.Empty)
                throw new DomainException("CustomerId is required.");

            if (string.IsNullOrWhiteSpace(customerName))
                throw new DomainException("CustomerName is required.");

            if (branchId == Guid.Empty)
                throw new DomainException("BranchId is required.");

            if (string.IsNullOrWhiteSpace(branchName))
                throw new DomainException("BranchName is required.");

            var id = Guid.NewGuid();

            // Validate items collection first (unique active productIds)
            if (items != null)
            {
                var activeProductIds = items.Select(i => i.ProductId).ToList();
                if (activeProductIds.Count != activeProductIds.Distinct().Count())
                    throw new DomainException("Duplicate productId in initial items is not allowed.");
            }

            return new Sale(id, saleDate, customerId, customerName.Trim(), branchId, branchName.Trim(), items);
        }

        public string SaleNumber { get; private set; } = null!;
        public DateTime SaleDate { get; private set; }
        public Guid CustomerId { get; private set; }
        public string CustomerName { get; private set; } = null!;
        public Guid BranchId { get; private set; }
        public string BranchName { get; private set; } = null!;
        public decimal TotalAmount { get; private set; }
        public bool IsCancelled { get; private set; }
        public IReadOnlyCollection<SaleItem> Items => _items.AsReadOnly();

        public void AddItem(Guid productId, string productName, int quantity, decimal unitPrice)
        {
            if (IsCancelled)
                throw new DomainException("Cannot add item to a cancelled sale.");

            if (_items.Any(i => !i.IsCancelled && i.ProductId == productId))
                throw new DomainException("Duplicate active productId in sale not allowed.");

            var item = SaleItem.Create(null, Id, productId, productName, quantity, unitPrice);
            _items.Add(item);
            RecalculateTotal();
        }

        public void UpdateHeader(DateTime saleDate, Guid customerId, string customerName, Guid branchId, string branchName)
        {
            if (IsCancelled)
                throw new DomainException("Cannot update a cancelled sale.");

            if (saleDate == default)
                throw new DomainException("SaleDate is required.");

            if (customerId == Guid.Empty)
                throw new DomainException("CustomerId is required.");

            if (string.IsNullOrWhiteSpace(customerName))
                throw new DomainException("CustomerName is required.");

            if (branchId == Guid.Empty)
                throw new DomainException("BranchId is required.");

            SaleDate = saleDate;
            CustomerId = customerId;
            CustomerName = customerName.Trim();
            BranchId = branchId;
            BranchName = branchName.Trim();
        }

        public record ItemUpdate(Guid? Id, Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

        public void UpdateItems(IEnumerable<ItemUpdate> desiredItems)
        {
            if (IsCancelled)
                throw new DomainException("Cannot update items of a cancelled sale.");

            if (desiredItems == null)
                throw new DomainException("desiredItems is required.");

            var desiredList = desiredItems.ToList();

            // Validation phase (do not mutate domain)
            // 1. Duplicate IDs in payload
            var nonNullIds = desiredList.Where(d => d.Id != null).Select(d => d.Id!.Value).ToList();
            if (nonNullIds.Count != nonNullIds.Distinct().Count())
                throw new DomainException("Duplicate item id in request.");

            // 2. All supplied non-null ids must belong to this sale
            foreach (var id in nonNullIds)
            {
                var existing = _items.FirstOrDefault(i => i.Id == id);
                if (existing == null)
                    throw new DomainException($"Item id '{id}' does not belong to this sale.");
            }

            // 3. Do not allow reactivation of previously cancelled items
            foreach (var d in desiredList.Where(d => d.Id != null))
            {
                var existing = _items.First(i => i.Id == d.Id!.Value);
                if (existing.IsCancelled)
                    throw new DomainException("Cannot reactivate a cancelled item.");
            }

            // 4. Unique active product IDs in desired set
            var dupProducts = desiredList.Select(d => d.ProductId).ToList();
            if (dupProducts.Count != dupProducts.Distinct().Count())
                throw new DomainException("Duplicate productId in desired items is not allowed.");

            // 5. Quantity/price/name validation via SalePricing (calls will throw if invalid)
            foreach (var d in desiredList)
            {
                if (d.ProductId == Guid.Empty)
                    throw new DomainException("ProductId is required.");

                if (string.IsNullOrWhiteSpace(d.ProductName))
                    throw new DomainException("ProductName is required.");

                // Validate with pricing calc (throws on invalid qty/price)
                _ = Ambev.DeveloperEvaluation.Domain.Services.SalePricing.Calculate(d.Quantity, d.UnitPrice);
            }

            // All validations passed — apply changes atomically
            // Determine existing active items that are omitted => to cancel
            var desiredIdsSet = new HashSet<Guid>(desiredList.Where(d => d.Id != null).Select(d => d.Id!.Value));
            var itemsToCancel = _items.Where(i => !i.IsCancelled && !desiredIdsSet.Contains(i.Id)).ToList();

            // Update existing + add new
            foreach (var d in desiredList)
            {
                if (d.Id != null)
                {
                    var existing = _items.First(i => i.Id == d.Id!.Value);
                    existing.Update(d.ProductName, d.Quantity, d.UnitPrice);
                }
                else
                {
                    // new item
                    var newItem = SaleItem.Create(null, Id, d.ProductId, d.ProductName, d.Quantity, d.UnitPrice);
                    _items.Add(newItem);
                }
            }

            // Cancel omitted active items
            foreach (var it in itemsToCancel)
            {
                it.MarkCancelled();
            }

            RecalculateTotal();
        }

        public bool CancelItem(Guid itemId)
        {
            var item = _items.FirstOrDefault(i => i.Id == itemId);
            if (item == null)
                throw new DomainException("Item not found in this sale.");

            var changed = item.MarkCancelled();
            if (changed)
                RecalculateTotal();

            return changed;
        }

        public bool Cancel()
        {
            if (IsCancelled)
                return false;

            IsCancelled = true;

            var anyChanged = false;
            foreach (var it in _items.Where(i => !i.IsCancelled))
            {
                if (it.MarkCancelled())
                    anyChanged = true;
            }

            // After cancelling all active items, total is sum of active items (none) => 0
            RecalculateTotal();
            return true;
        }

        public void RecalculateTotal()
        {
            var sum = _items.Where(i => !i.IsCancelled).Sum(i => i.TotalAmount);
            TotalAmount = decimal.Round(sum, 2, MidpointRounding.AwayFromZero);
        }
    }
}