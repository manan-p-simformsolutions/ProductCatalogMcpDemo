using ModelContextProtocol.Server;
using ProductCatalogMcpDemo.Models;
using ProductCatalogMcpDemo.Services;
using System.ComponentModel;

namespace ProductCatalogMcpDemo.Tools;

/// <summary>
/// Exposes product catalog operations as MCP tools for AI agents.
/// </summary>
[McpServerToolType]
public sealed class ProductCatalogTools
{
    private readonly IProductService _productsService;

    public ProductCatalogTools(IProductService products)
        => _productsService = products;

    [McpServerTool, Description("Search the product catalog by product name or keyword. Returns up to 50 matching products.")]
    public async Task<IEnumerable<Product>> SearchProductsAsync(
        [Description("Search text to match against product name, description, or SKU. Required, 1 to 80 characters.")] string keyword,
        [Description("How many products to return. Integer from 1 to 50. Default is 10.")] int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        var list = await _productsService.SearchAsync(keyword, maxResults, cancellationToken).ConfigureAwait(false);
        return list;
    }

    [McpServerTool, Description("Retrieve a product by its catalog id. Returns id, SKU, name, description, and price.")]
    public async Task<Product?> GetProductByIdAsync(
        [Description("The product id from the catalog. A positive integer, for example 1.")] int productId,
        CancellationToken cancellationToken = default)
    {
        return await _productsService.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
    }

    [McpServerTool, Description("Check available stock for a product SKU. Returns quantity and warehouse code.")]
    public async Task<InventoryStatus> CheckInventoryAsync(
        [Description("The product SKU to look up, for example SKU-MOUSE-001.")] string sku,
        CancellationToken cancellationToken = default)
    {
        return await _productsService.GetInventoryStatusAsync(sku, cancellationToken).ConfigureAwait(false);
    }
}
