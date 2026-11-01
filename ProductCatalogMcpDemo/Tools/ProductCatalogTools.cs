using ModelContextProtocol;
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
    private readonly ILogger<ProductCatalogTools> _logger;

    public ProductCatalogTools(IProductService products, ILogger<ProductCatalogTools> logger)
    {
        _productsService = products;
        _logger = logger;
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Search the product catalog by product name or keyword. Returns up to 50 matching products.")]
    public Task<IEnumerable<Product>> SearchProductsAsync(
        [Description("Search text to match against product name, description, or SKU. Required, 1 to 80 characters.")] string keyword,
        [Description("How many products to return. Integer from 1 to 50. Default is 10.")] int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        return InvokeAsync(async () =>
        {
            var list = await _productsService.SearchAsync(keyword, maxResults, cancellationToken).ConfigureAwait(false);
            return (IEnumerable<Product>)list;
        });
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Retrieve a product by its catalog id. Returns id, SKU, name, description, and price.")]
    public Task<Product?> GetProductByIdAsync(
        [Description("The product id from the catalog. A positive integer, for example 1.")] int productId,
        CancellationToken cancellationToken = default)
    {
        return InvokeAsync(() => _productsService.GetByIdAsync(productId, cancellationToken));
    }

    [McpServerTool(ReadOnly = true, Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("Check available stock for a product SKU. Returns quantity and warehouse code.")]
    public Task<InventoryStatus> CheckInventoryAsync(
        [Description("The product SKU to look up, for example SKU-MOUSE-001.")] string sku,
        CancellationToken cancellationToken = default)
    {
        return InvokeAsync(() => _productsService.GetInventoryStatusAsync(sku, cancellationToken));
    }

    private async Task<T> InvokeAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (ArgumentException ex)
        {
            throw new McpException(ex.Message);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Catalog tool failed.");
            throw;
        }
    }
}
