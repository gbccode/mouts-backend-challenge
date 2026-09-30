using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

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

    public async Task<SalesPageResult> GetPageAsync(int page, int pageSize, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var query = _context.Sales.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(s =>
                s.SaleNumber.Contains(term) ||
                s.CustomerName.Contains(term) ||
                s.BranchName.Contains(term));
        }

        // Count before pagination
        var totalItems = await query.CountAsync(cancellationToken);

        // Order and paginate in the database
        var items = await query
            .OrderByDescending(s => s.SaleDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        return new SalesPageResult(items, totalItems, page, totalPages);
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