# Product Catalog MCP Demo

Sample ASP.NET Core app that exposes the same in-memory product catalog through **REST** and **MCP** (Model Context Protocol) over **streamable HTTP**. The MCP server is implemented with [`ModelContextProtocol.AspNetCore`](https://www.nuget.org/packages/ModelContextProtocol.AspNetCore) and registered in [`ProductCatalogMcpDemo/Program.cs`](ProductCatalogMcpDemo/Program.cs).

**Repository:** [https://github.com/manan-p-simformsolutions/ProductCatalogMcpDemo](https://github.com/manan-p-simformsolutions/ProductCatalogMcpDemo)

## Prerequisites

- **.NET SDK 10:** This repo pins a minimum SDK in [`global.json`](global.json) (currently **10.0.103**). The project targets `net10.0`. If `dotnet` reports an SDK resolution error, install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or a newer supported SDK (`rollForward` allows newer feature bands).
- **Optional:** [Node.js](https://nodejs.org/) only if you use [MCP Inspector](#optional-mcp-inspector) instead of your editor’s built-in MCP client.

## Clone, restore, and run

```bash
git clone https://github.com/manan-p-simformsolutions/ProductCatalogMcpDemo.git
cd ProductCatalogMcpDemo
dotnet restore
dotnet run --project ProductCatalogMcpDemo/ProductCatalogMcpDemo.csproj
```

By default the **http** profile listens on **http://localhost:5010** (see `ProductCatalogMcpDemo/Properties/launchSettings.json`). The **https** profile also binds **https://localhost:7249**.

| URL | Description |
|-----|-------------|
| [http://localhost:5010/](http://localhost:5010/) | Short help text (Swagger, MCP, and REST paths) |
| [http://localhost:5010/swagger](http://localhost:5010/swagger) | Swagger UI |
| [http://localhost:5010/api/...](http://localhost:5010/api/products) | REST API under `/api` |
| `POST http://localhost:5010/mcp` | MCP streamable HTTP endpoint (JSON-RPC) |

## Architecture

Both entry points call the same domain logic:

- **REST:** Swagger UI → [`CatalogController`](ProductCatalogMcpDemo/Controllers/CatalogController.cs) → [`IProductService`](ProductCatalogMcpDemo/Services/IProductService.cs) → [`ProductService`](ProductCatalogMcpDemo/Services/ProductService.cs).
- **MCP:** `POST /mcp` → [`ProductCatalogTools`](ProductCatalogMcpDemo/Tools/ProductCatalogTools.cs) → the same **`IProductService`** implementation.

So behavior and data match whether you use HTTP APIs or MCP tools.

## REST API and expected results

All examples assume the app is running on `http://localhost:5010` with the default seed data in `ProductService` (products **1–3**, SKUs such as `SKU-MOUSE-001`).

### Try it in Swagger UI

Open [http://localhost:5010/swagger](http://localhost:5010/swagger) in a browser. Expand **GET** `/api/products/{id}`, set **`id`** to **3**, click **Execute**. You should receive **200** and JSON for the Adventure Travel Backpack, for example:

```json
{
  "id": 3,
  "sku": "SKU-ADVENTURE-7",
  "name": "Adventure Travel Backpack",
  "description": "Water-resistant backpack with laptop sleeve.",
  "price": 89.5
}
```

### Search products

`GET /api/products?q={keyword}&max={1-50}` — `max` defaults to **10** if omitted or invalid. If `q` is missing or empty, the service returns an empty list (**200** with `[]`).

**Example (curl):**

```bash
curl -s "http://localhost:5010/api/products?q=mouse&max=5"
```

**Example (PowerShell):**

```powershell
Invoke-RestMethod "http://localhost:5010/api/products?q=mouse&max=5" | ConvertTo-Json
```

**Expected (shape):** JSON array of products. For `q=mouse` you should see the Wireless Mouse entry, for example:

```json
[
  {
    "id": 1,
    "sku": "SKU-MOUSE-001",
    "name": "Wireless Mouse",
    "description": "Ergonomic wireless mouse with silent clicks.",
    "price": 29.99
  }
]
```

**Empty search:**

```bash
curl -s "http://localhost:5010/api/products?q="
```

**Expected:** `[]`

### Get product by ID

`GET /api/products/{id}` — **200** with a product object, or **404** if the ID does not exist.

```bash
curl -s -w "\nHTTP_CODE:%{http_code}\n" "http://localhost:5010/api/products/1"
```

**Expected (HTTP 200):**

```json
{
  "id": 1,
  "sku": "SKU-MOUSE-001",
  "name": "Wireless Mouse",
  "description": "Ergonomic wireless mouse with silent clicks.",
  "price": 29.99
}
```

**Unknown ID (example):** `GET /api/products/999` → **404** with empty body.

### Inventory by SKU

`GET /api/inventory/{sku}` — always **200**. Known SKUs return stock and warehouse; unknown SKUs return quantity **0** and `warehouseCode` **UNKNOWN**.

```bash
curl -s "http://localhost:5010/api/inventory/SKU-MOUSE-001"
```

**Expected:**

```json
{
  "sku": "SKU-MOUSE-001",
  "availableQuantity": 120,
  "warehouseCode": "WH-EAST-1"
}
```

## MCP testing

The server exposes MCP at **`POST /mcp`** using **streamable HTTP** (`WithHttpTransport`, stateless). Tool implementations live in [`ProductCatalogMcpDemo/Tools/ProductCatalogTools.cs`](ProductCatalogMcpDemo/Tools/ProductCatalogTools.cs) (`SearchProductsAsync`, `GetProductByIdAsync`, `CheckInventoryAsync`). The MCP C# SDK publishes tool names to clients (for example snake_case such as **`get_product_by_id`**); your IDE displays them when the server is connected.

### Recommended: `mcp.json` + IDE chat (Copilot)

Configure MCP in your editor, keep the app on port **5010**, then use natural language in chat (for example “Get product details for id **3**”). **GitHub Copilot Chat** (or another MCP-aware agent in your IDE) selects the catalog tool and returns the same fields you see in Swagger for that product.

1. **Start the API** (same command as in [Clone, restore, and run](#clone-restore-and-run)) so **`http://localhost:5010/mcp`** is available.
2. **Register the MCP server** by adding a URL transport entry. Minimal configuration:

```json
{
  "servers": {
    "product-catalog-mcp": {
      "url": "http://localhost:5010/mcp"
    }
  }
}
```

Put this in your editor’s MCP configuration (**VS Code:** user or workspace `mcp.json`—see [Add and manage MCP servers in VS Code](https://code.visualstudio.com/docs/copilot/customization/mcp-servers) and the [MCP configuration reference](https://code.visualstudio.com/docs/copilot/reference/mcp-configuration); **Cursor:** follow Cursor’s MCP docs if the root key differs, often `mcpServers`). For VS Code HTTP servers you may also specify `"type": "http"` next to `"url"` if IntelliSense suggests it.

3. **Confirm the connection** in the MCP UI: the **product-catalog-mcp** server should show as running and report **three tools** (search, product by id, inventory).
4. **Chat:** Ask for catalog data in plain language (for example “Find the product with id **1**” or “Get product details for id **3**”). You should see a tool run (for example **`get_product_by_id`**) and a reply with **id**, **sku**, **name**, **description**, and **price** consistent with [`ProductService`](ProductCatalogMcpDemo/Services/ProductService.cs).

**Expected output (example):** For product id **3**, the assistant should return data aligned with the Swagger example above (**SKU-ADVENTURE-7**, Adventure Travel Backpack, price **89.50**).

### Optional: MCP Inspector

If you prefer a standalone debugger, use the official [MCP Inspector](https://modelcontextprotocol.io/docs/tools/inspector) against **`http://localhost:5010/mcp`** (see Inspector docs for HTTP transport and `npx` prerequisites).

### Protocol note

Under the hood, clients use **JSON-RPC** on the MCP endpoint (**`initialize`** → **`tools/list`** → **`tools/call`**). Framing is handled by the [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk) streamable HTTP implementation—you normally do not call **`curl`** directly for interactive testing.

### MCP server metadata

Configured in code as **ProductCatalogMCP** version **1.0.0** (`Implementation` in `Program.cs`), for parity with clients that display server name/version.

## Version matrix (source of truth)

Keep documentation (including blog posts) aligned with this table.

| Item | Version |
|------|---------|
| **Pinned SDK (minimum)** | 10.0.103 (see [`global.json`](global.json); `rollForward` applies) |
| **Target framework** | `net10.0` |
| **Microsoft.AspNetCore.OpenApi** | 10.0.10 |
| **ModelContextProtocol.AspNetCore** | 1.4.1 |
| **Swashbuckle.AspNetCore** | 10.2.3 |
| **MCP server name / version (runtime)** | ProductCatalogMCP / 1.0.0 |
