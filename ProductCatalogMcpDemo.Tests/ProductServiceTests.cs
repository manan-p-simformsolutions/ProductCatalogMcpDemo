using ProductCatalogMcpDemo.Services;
using Xunit;

namespace ProductCatalogMcpDemo.Tests;

public sealed class ProductServiceTests
{
    private readonly ProductService _sut = new();

    [Fact]
    public async Task Search_ByKeyword_ReturnsMatchingProduct()
    {
        var results = await _sut.SearchAsync("mouse", 10);

        var product = Assert.Single(results);
        Assert.Equal(1, product.Id);
        Assert.Equal("Wireless Mouse", product.Name);
    }

    [Fact]
    public async Task GetById_KnownId_ReturnsProduct()
    {
        var product = await _sut.GetByIdAsync(1);

        Assert.NotNull(product);
        Assert.Equal("SKU-MOUSE-001", product.Sku);
    }

    [Fact]
    public async Task Search_EmptyKeyword_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.SearchAsync("  ", 10));
    }

    [Fact]
    public async Task Search_MaxResultsOutOfRange_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.SearchAsync("mouse", 0));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.SearchAsync("mouse", 51));
    }

    [Fact]
    public async Task GetById_NonPositiveId_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _sut.GetByIdAsync(0));
    }

    [Fact]
    public async Task GetInventory_EmptySku_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetInventoryStatusAsync(""));
    }
}
