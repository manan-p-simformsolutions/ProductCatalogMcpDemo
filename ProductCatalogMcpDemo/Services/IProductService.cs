using ProductCatalogMcpDemo.Models;

namespace ProductCatalogMcpDemo.Services;

public interface IProductService
{
    Task<IReadOnlyList<Product>> SearchAsync(string keyword, int maxResults, CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken = default);

    Task<InventoryStatus> GetInventoryStatusAsync(string sku, CancellationToken cancellationToken = default);
}
