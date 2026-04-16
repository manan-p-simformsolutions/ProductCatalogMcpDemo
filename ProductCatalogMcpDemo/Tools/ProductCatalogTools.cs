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

    [McpServerTool, Description("Search the product catalog by keyword.")]
    public async Task<IEnumerable<Product>> SearchProductsAsync(
        [Description("Keyword to search for in name, description, or SKU.")] string keyword,
        [Description("Maximum number of results to return (default 10).")] int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        var list = await _productsService.SearchAsync(keyword, maxResults, cancellationToken).ConfigureAwait(false);
        return list;
    }

    [McpServerTool, Description("Retrieve full details for a product by its ID.")]
    public async Task<Product?> GetProductByIdAsync(
        [Description("The unique product identifier (integer).")] int productId,
        CancellationToken cancellationToken = default)
    {
        return await _productsService.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
    }

    [McpServerTool, Description("Check available stock for a product SKU.")]
    public async Task<InventoryStatus> CheckInventoryAsync(
        [Description("The SKU code of the product to check.")] string sku,
        CancellationToken cancellationToken = default)
    {
        return await _productsService.GetInventoryStatusAsync(sku, cancellationToken).ConfigureAwait(false);
    }
}
