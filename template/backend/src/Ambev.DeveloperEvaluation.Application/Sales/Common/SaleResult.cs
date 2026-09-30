namespace Ambev.DeveloperEvaluation.Application.Sales.Common;

public class SaleItemResult
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }    // fraction, e.g. 0.20
    public decimal DiscountAmount { get; set; }  // currency
    public decimal TotalAmount { get; set; }     // currency
    public bool IsCancelled { get; set; }
}

public class SaleResult
{
    public Guid Id { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public bool IsCancelled { get; set; }
    public IEnumerable<SaleItemResult> Items { get; set; } = Array.Empty<SaleItemResult>();
}