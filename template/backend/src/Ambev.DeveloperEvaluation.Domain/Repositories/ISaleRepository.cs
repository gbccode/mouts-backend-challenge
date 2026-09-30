using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Domain.Repositories;

/// <summary>
/// Result of a paginated query for sales, including the list of sales, total items, current page, and total pages.
/// </summary>
/// <param name="Sales">List of sales in the current page</param>
/// <param name="TotalItems">Total number of items that match the filter</param>
/// <param name="CurrentPage">Current page (1-based)</param>
/// <param name="TotalPages">Total number of pages</param>
public record SalesPageResult(IEnumerable<Sale> Sales, int TotalItems, int CurrentPage, int TotalPages);

/// <summary>
/// Repository interface for managing Sale entities. Provides methods for adding, retrieving, updating, and deleting sales, as well as paginated queries with optional filtering by search term.
/// </summary>
public interface ISaleRepository
{
    Task<Sale> AddAsync(Sale sale, CancellationToken cancellationToken = default);

    /// <summary>
    /// Load a sale by id, including its items. Returns null if not found.
    /// </summary>
    Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paginated list of sales, optionally filtered by search term (sale number, customer name, or branch name).
    /// </summary>
    Task<SalesPageResult> GetPageAsync(int page, int pageSize, string? searchTerm = null,
        string? order = null, bool? isCancelled = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persist a sale and its items (cascade). If the sale already exists, it will be updated. If not, it will be added.
    /// </summary>
    Task SaveAsync(Sale sale, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a sale and its items (cascade). Returns true if found and removed.
    /// </summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
