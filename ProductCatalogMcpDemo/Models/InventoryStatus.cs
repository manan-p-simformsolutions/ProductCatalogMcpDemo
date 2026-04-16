namespace ProductCatalogMcpDemo.Models;

public class InventoryStatus
{
    public string Sku { get; set; } = string.Empty;

    public int AvailableQuantity { get; set; }

    public string WarehouseCode { get; set; } = string.Empty;
}
