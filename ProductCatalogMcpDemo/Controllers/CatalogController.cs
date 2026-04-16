using Microsoft.AspNetCore.Mvc;
using ProductCatalogMcpDemo.Models;
using ProductCatalogMcpDemo.Services;

namespace ProductCatalogMcpDemo.Controllers;

[ApiController]
[Route("api")]
public sealed class CatalogController : ControllerBase
{
    private readonly IProductService _productsService;

    public CatalogController(IProductService products)
        => _productsService = products;

    /// <summary>Search the catalog by keyword (query: q, max).</summary>
    [HttpGet("products")]
    [ProducesResponseType(typeof(IReadOnlyList<Product>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchProducts(
        [FromQuery] string? q,
        [FromQuery] int? max,
        CancellationToken cancellationToken)
    {
        var keyword = q ?? string.Empty;
        var maxResults = max is > 0 and <= 50 ? max.Value : 10;
        var results = await _productsService.SearchAsync(keyword, maxResults, cancellationToken).ConfigureAwait(false);
        return Ok(results);
    }

    /// <summary>Get a product by integer id.</summary>
    [HttpGet("products/{id:int}")]
    [ProducesResponseType(typeof(Product), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById(int id, CancellationToken cancellationToken)
    {
        var product = await _productsService.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return product is null ? NotFound() : Ok(product);
    }

    /// <summary>Get inventory status for a SKU.</summary>
    [HttpGet("inventory/{sku}")]
    [ProducesResponseType(typeof(InventoryStatus), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInventoryBySku(string sku, CancellationToken cancellationToken)
    {
        var status = await _productsService.GetInventoryStatusAsync(sku, cancellationToken).ConfigureAwait(false);
        return Ok(status);
    }
}
