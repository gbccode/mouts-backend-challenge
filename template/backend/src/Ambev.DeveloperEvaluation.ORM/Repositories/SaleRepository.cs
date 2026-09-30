using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

/// <summary>
/// EF Core implementation of ISaleRepository
/// </summary>
public class SaleRepository : ISaleRepository
{
    private readonly DefaultContext _context;

    public SaleRepository(DefaultContext context)
    {
        _context = context;
    }

    public async Task<Sale> AddAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        await _context.Sales.AddAsync(sale, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    public async Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Load tracked aggregate with items for update/cancel operations
        return await _context.Sales
            .Include(s => s.Items)
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<SalesPageResult> GetPageAsync(int page, int pageSize, string? searchTerm = null,
        string? order = null, bool? isCancelled = null, CancellationToken cancellationToken = default)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            throw new DomainException("_page must be positive and _size must be between 1 and 100.");
        var sorting = SaleSort.Parse(order);

        var query = _context.Sales.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLowerInvariant();
            query = query.Where(s =>
                s.SaleNumber.ToLower().Contains(term) ||
                s.CustomerName.ToLower().Contains(term) ||
                s.BranchName.ToLower().Contains(term));
        }

        if (isCancelled.HasValue)
            query = query.Where(s => s.IsCancelled == isCancelled.Value);

        // Count before pagination
        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        // Recover from a page disappearing after a deletion, including concurrent deletions.
        var currentPage = Math.Min(page, Math.Max(1, totalPages));

        IOrderedQueryable<Sale>? ordered = null;
        foreach (var sort in sorting)
        {
            ordered = sort.Field switch
            {
                "salenumber" => ApplyOrder(query, ordered, s => s.SaleNumber, sort.Descending),
                "saledate" => ApplyOrder(query, ordered, s => s.SaleDate, sort.Descending),
                "customername" => ApplyOrder(query, ordered, s => s.CustomerName, sort.Descending),
                "branchname" => ApplyOrder(query, ordered, s => s.BranchName, sort.Descending),
                "totalamount" => ApplyOrder(query, ordered, s => s.TotalAmount, sort.Descending),
                "iscancelled" => ApplyOrder(query, ordered, s => s.IsCancelled, sort.Descending),
                "id" => ApplyOrder(query, ordered, s => s.Id, sort.Descending),
                _ => throw new DomainException("Unsupported sale ordering field.")
            };
        }

        // Order and paginate in the database
        var items = await ordered!
            .ThenBy(s => s.Id) // A unique tie-breaker keeps pagination deterministic.
            .Skip((currentPage - 1) * pageSize)
            .Take(pageSize)
            .Include(s => s.Items)
            .ToListAsync(cancellationToken);

        return new SalesPageResult(items, totalItems, currentPage, totalPages);
    }

    private static IOrderedQueryable<Sale> ApplyOrder<TKey>(IQueryable<Sale> query,
        IOrderedQueryable<Sale>? ordered, Expression<Func<Sale, TKey>> key, bool descending)
    {
        if (ordered is null)
            return descending ? query.OrderByDescending(key) : query.OrderBy(key);
        return descending ? ordered.ThenByDescending(key) : ordered.ThenBy(key);
    }

    public async Task SaveAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        // Assumes sale is already tracked by context; just persist once.
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await _context.Sales
            .Include(s => s.Items)
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (sale == null)
            return false;

        _context.Sales.Remove(sale);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
