using ProductCatalogMcpDemo.Models;

namespace ProductCatalogMcpDemo.Services;

/// <summary>
/// In-memory catalog for the blog demo (replace with your API or database).
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

    public Task<IReadOnlyList<Product>> SearchAsync(string keyword, int maxResults, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return Task.FromResult<IReadOnlyList<Product>>([]);
        }

        var k = keyword.Trim();
        var matches = Products
            .Where(p =>
                p.Name.Contains(k, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(k, StringComparison.OrdinalIgnoreCase) ||
                p.Sku.Contains(k, StringComparison.OrdinalIgnoreCase))
            .Take(Math.Max(1, Math.Min(maxResults, 50)))
            .ToList();

        return Task.FromResult<IReadOnlyList<Product>>(matches);
    }

    public Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        var product = Products.FirstOrDefault(p => p.Id == productId);
        return Task.FromResult(product);
    }

    public Task<InventoryStatus> GetInventoryStatusAsync(string sku, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return Task.FromResult(new InventoryStatus
            {
                Sku = "(empty)",
                AvailableQuantity = 0,
                WarehouseCode = "N/A",
            });
        }

        if (Inventory.TryGetValue(sku.Trim(), out var status))
        {
            return Task.FromResult(status);
        }

        return Task.FromResult(new InventoryStatus
        {
            Sku = sku.Trim(),
            AvailableQuantity = 0,
            WarehouseCode = "UNKNOWN",
        });
    }
}
