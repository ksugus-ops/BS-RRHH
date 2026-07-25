namespace HRIA.Application.Common.Models;

/// <summary>Resultado paginado genérico.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}
