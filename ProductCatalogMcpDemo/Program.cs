using ModelContextProtocol.Protocol;
using Microsoft.OpenApi.Models;
using ProductCatalogMcpDemo.Services;
using ProductCatalogMcpDemo.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IProductService, ProductService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Product Catalog API",
        Version = "v1",
        Description = "REST endpoints for the product catalog (same logic as MCP tools).",
    });
});

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new Implementation
        {
            Name = "ProductCatalogMCP",
            Version = "1.0.0",
        };
    })
    .WithHttpTransport(o =>
    {
        o.Stateless = true;
    })
    .WithToolsFromAssembly();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Product Catalog v1");
});

app.MapControllers();

app.MapGet("/", () => Results.Text(
    "Product catalog demo. Swagger UI: /swagger  |  MCP: POST /mcp  |  REST: /api/products, /api/products/{id}, /api/inventory/{sku}"));

app.MapMcp("/mcp");

app.Run();
