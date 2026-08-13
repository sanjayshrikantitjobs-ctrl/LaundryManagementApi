using System.Linq.Expressions;

namespace LaundryMgmt.Application.Common.Models;

/// <summary>Applies a client-requested sort against an explicit per-query allow-list of
/// columns (never raw reflection over arbitrary property names) — keeps sorting both
/// EF-translatable and safe from exposing internal/unindexed columns.</summary>
public static class SortableQueryExtensions
{
    /// <summary>
    /// <paramref name="defaultDescending"/> preserves each query's original hardcoded
    /// sort direction (e.g. newest-first for orders/promotions) when the caller doesn't
    /// request a specific column — only an explicit <paramref name="sortBy"/> lets the
    /// caller's <paramref name="sortDirection"/> override it.
    /// </summary>
    public static IOrderedQueryable<T> ApplySort<T>(
        this IQueryable<T> query,
        string? sortBy,
        string? sortDirection,
        IReadOnlyDictionary<string, Expression<Func<T, object>>> sortableColumns,
        Expression<Func<T, object>> defaultSort,
        bool defaultDescending = false)
    {
        var selector = sortBy is not null && sortableColumns.TryGetValue(sortBy, out var match)
            ? match
            : defaultSort;

        var descending = sortBy is not null
            ? string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase)
            : defaultDescending;

        return descending ? query.OrderByDescending(selector) : query.OrderBy(selector);
    }
}
