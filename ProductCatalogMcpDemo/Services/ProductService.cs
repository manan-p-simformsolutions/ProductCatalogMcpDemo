using ProductCatalogMcpDemo.Models;

namespace ProductCatalogMcpDemo.Services;

/// <summary>
/// In-memory catalog for the blog demo (replace with your API or database).
/// CancellationToken is here so the same call chain is ready for
/// production I/O such as FindAsync or HttpClient.GetAsync.
/// </summary>
public sealed class ProductService : IProductService
{
    private static readonly List<Product> Products =
    [
        new()
        {
            Id = 1,
            Sku = "SKU-MOUSE-001",
            Name = "Wireless Mouse",
            Description = "Ergonomic wireless mouse with silent clicks.",
            Price = 29.99m,
        },
        new()
        {
            Id = 2,
            Sku = "SKU-CHARGER-42",
            Name = "USB-C Hub",
            Description = "7-in-1 hub with HDMI and SD card reader.",
            Price = 59.00m,
        },
        new()
        {
            Id = 3,
            Sku = "SKU-ADVENTURE-7",
            Name = "Adventure Travel Backpack",
            Description = "Water-resistant backpack with laptop sleeve.",
            Price = 89.50m,
        },
    ];

    private static readonly Dictionary<string, InventoryStatus> Inventory = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SKU-MOUSE-001"] = new()
        {
            Sku = "SKU-MOUSE-001",
            AvailableQuantity = 120,
            WarehouseCode = "WH-EAST-1",
        },
        ["SKU-CHARGER-42"] = new()
        {
            Sku = "SKU-CHARGER-42",
            AvailableQuantity = 34,
            WarehouseCode = "WH-WEST-2",
        },
        ["SKU-ADVENTURE-7"] = new()
        {
            Sku = "SKU-ADVENTURE-7",
            AvailableQuantity = 12,
            WarehouseCode = "WH-EAST-1",
        },
    };

    private const int MaxKeywordLength = 80;
    private const int MaxSkuLength = 32;

    public Task<IReadOnlyList<Product>> SearchAsync(string keyword, int maxResults, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(keyword))
        {
            throw new ArgumentException("Keyword is required.", nameof(keyword));
        }

        var k = keyword.Trim();
        if (k.Length > MaxKeywordLength)
        {
            throw new ArgumentException("Keyword must be 80 characters or fewer.", nameof(keyword));
        }

        if (maxResults is < 1 or > 50)
        {
            throw new ArgumentOutOfRangeException(nameof(maxResults), "maxResults must be between 1 and 50.");
        }

        var matches = Products
            .Where(p =>
                p.Name.Contains(k, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(k, StringComparison.OrdinalIgnoreCase) ||
                p.Sku.Contains(k, StringComparison.OrdinalIgnoreCase))
            .Take(maxResults)
            .ToList();

        return Task.FromResult<IReadOnlyList<Product>>(matches);
    }

    public Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (productId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(productId), "productId must be a positive integer.");
        }

        var product = Products.FirstOrDefault(p => p.Id == productId);
        return Task.FromResult(product);
    }

    public Task<InventoryStatus> GetInventoryStatusAsync(string sku, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new ArgumentException("SKU is required.", nameof(sku));
        }

        var s = sku.Trim();
        if (s.Length > MaxSkuLength)
        {
            throw new ArgumentException("SKU must be 32 characters or fewer.", nameof(sku));
        }

        if (Inventory.TryGetValue(s, out var status))
        {
            return Task.FromResult(status);
        }

        return Task.FromResult(new InventoryStatus
        {
            Sku = s,
            AvailableQuantity = 0,
            WarehouseCode = "UNKNOWN",
        });
    }
}
