---
description: "Use when building or reviewing .NET 8, ASP.NET Core Web API, C#, JSON config, solution, or project files. Enforces enterprise coding standards, API design, security, architecture, testing, database, cloud-readiness, and performance practices for mid-senior .NET teams."
name: ".NET Engineering Standards"
applyTo:
  - "**/*.cs"
  - "**/*.csproj"
  - "**/*.sln"
  - "**/*.json"
---
# .NET Engineering Standards

Use these standards for all implementation and review tasks in this repository.

## Enforcement Contract

- MUST follow these rules unless a documented exception is approved in the PR description.
- MUST return concrete fixes (code or config) for every detected violation.
- MUST classify violations:
  - Critical: Security, data loss risk, auth flaws, severe correctness bugs.
  - Major: Architecture violations, performance hotspots, reliability issues.
  - Minor: Naming, style, documentation, maintainability improvements.
- MUST map each review finding to a standard section in this file.
- SHOULD prefer modern .NET 8 built-in features before custom frameworks.

## 1) Coding Standards (Naming, SOLID, Clean Code)

### DO
- Use clear names: `PascalCase` for types/methods, `camelCase` for locals/parameters, `_camelCase` for private readonly fields.
- Keep methods small and cohesive (single responsibility).
- Depend on abstractions at boundaries (interfaces for external dependencies).
- Use immutable DTOs where possible (`record`, init-only).
- Keep controllers thin; move business logic to application/services layer.

### DON'T
- Do not use ambiguous names (`data`, `obj`, `temp`) outside trivial scopes.
- Do not inject large God services with unrelated responsibilities.
- Do not duplicate domain rules across controller/service/repository layers.

## 1.1) Documentation and Code Comments

### DO
- Add XML documentation comments on all public and protected methods, including controllers, services, repositories, and shared library APIs.
- Include `summary`, parameter docs, return docs, and `exception` docs when exceptions are part of the method contract.
- Add short implementation comments only where intent is not obvious from the code itself (complex logic, non-trivial business rules, security constraints, performance trade-offs).
- Keep comments synchronized with behavior changes in the same PR.

### DON'T
- Do not add XML comments that repeat the method name without meaningful context.
- Do not add obvious line-by-line comments that restate code.
- Do not keep stale comments that describe behavior no longer implemented.

### Good vs Bad

#### Bad
```csharp
/// <summary>
/// Gets user.
/// </summary>
public async Task<UserDto?> GetUserAsync(Guid userId, CancellationToken ct)
{
    // set id
    var id = userId;
    // fetch user
    return await _repository.GetByIdAsync(id, ct);
}
```

#### Good
```csharp
/// <summary>
/// Retrieves a user profile for the specified identifier.
/// </summary>
/// <param name="userId">The unique identifier of the user profile.</param>
/// <param name="ct">Cancellation token for cooperative cancellation.</param>
/// <returns>The user profile when found; otherwise null.</returns>
public async Task<UserDto?> GetUserAsync(Guid userId, CancellationToken ct)
{
    // Enforce tenant isolation before data access.
    await _tenantGuard.EnsureAccessAsync(userId, ct);

    return await _repository.GetByIdAsync(userId, ct);
}
```

### Good vs Bad

#### Bad
```csharp
public class UserService {
    public void DoStuff(User u) {
        // validation + mapping + persistence + email + logging mixed together
    }
}
```

#### Good
```csharp
public sealed class UserRegistrationService : IUserRegistrationService
{
    private readonly IUserValidator _validator;
    private readonly IUserRepository _users;
    private readonly INotificationService _notifications;

    public UserRegistrationService(
        IUserValidator validator,
        IUserRepository users,
        INotificationService notifications)
    {
        _validator = validator;
        _users = users;
        _notifications = notifications;
    }

    public async Task RegisterAsync(RegisterUserCommand command, CancellationToken ct)
    {
        _validator.ValidateAndThrow(command);
        var user = User.Create(command.Email, command.DisplayName);
        await _users.AddAsync(user, ct);
        await _notifications.SendWelcomeEmailAsync(user.Email, ct);
    }
}
```

## 2) API Design (REST, Versioning, Validation)

### DO
- Use resource-oriented routes and nouns (`/api/v1/orders/{id}`).
- Version APIs explicitly using URL segments only.
- Validate all request models using FluentValidation or equivalent.
- Return RFC 7807 Problem Details for errors.
- Support pagination/filtering for list endpoints.

### DON'T
- Do not expose internal domain entities directly in API contracts.
- Do not return `200 OK` for failed operations.
- Do not create action-style routes when resource semantics fit.
- Do not use header-based or query-string-based version negotiation in this codebase.

### Good vs Bad

#### Bad
```csharp
[HttpPost("CreateOrderNow")]
public IActionResult Create(Order order) => Ok(_service.Create(order));
```

#### Good
```csharp
[ApiController]
[Route("api/v{version:apiVersion}/orders")]
[ApiVersion("1.0")]
public sealed class OrdersController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderCreatedResponse>> Create(
        [FromBody] CreateOrderRequest request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateOrderCommand(request.CustomerId, request.Items), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.OrderId, version = "1.0" }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponse>> GetById(Guid id, CancellationToken ct)
    {
        var order = await _mediator.Send(new GetOrderQuery(id), ct);
        return Ok(order);
    }
}
```

## 3) Security Standards (OWASP, Auth, Validation, Secrets)

### DO
- Enforce authentication and authorization with policy-based auth.
- Validate all input and constrain model binding.
- Use parameterized queries/ORM APIs; prevent SQL injection.
- Store secrets in secure stores (Azure Key Vault, AWS Secrets Manager).
- Use least privilege for app/database/cloud identities.

### DON'T
- Do not log secrets, tokens, or PII.
- Do not hardcode secrets in source, appsettings, or tests.
- Do not disable TLS or certificate validation in production code.

### Good vs Bad

#### Bad
```csharp
var conn = new SqlConnection("Server=...;User Id=sa;Password=SuperSecret123");
var sql = $"SELECT * FROM Users WHERE Email = '{email}'";
```

#### Good
```csharp
var connectionString = _configuration.GetConnectionString("MainDb")
    ?? throw new InvalidOperationException("Missing MainDb connection string.");

await using var conn = new SqlConnection(connectionString);
const string sql = "SELECT Id, Email FROM Users WHERE Email = @Email";
var user = await conn.QuerySingleOrDefaultAsync<UserRow>(sql, new { Email = email });
```

## 4) Logging & Monitoring

### DO
- Use structured logging with templates and named properties.
- Include correlation/trace IDs in logs and responses.
- Emit business and technical telemetry (request latency, dependency failures).
- Use OpenTelemetry where possible.

### DON'T
- Do not use string-concatenated logs.
- Do not swallow exceptions without logging context.

### Good vs Bad

#### Bad
```csharp
_logger.LogInformation("Order " + orderId + " for " + customerId + " created");
```

#### Good
```csharp
using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
{
    _logger.LogInformation("Order created. OrderId: {OrderId}, CustomerId: {CustomerId}", orderId, customerId);
}
```

## 5) Performance (Async, Caching, DB Optimization)

### DO
- Use async/await end-to-end for I/O operations.
- Pass `CancellationToken` through all async boundaries.
- Cache hot read paths with explicit TTL and invalidation strategy.
- Select only required columns and paginate large result sets.

### DON'T
- Do not block async with `.Result` or `.Wait()`.
- Do not perform N+1 queries.
- Do not load entire tables into memory for filtering.

## 6) Error Handling (Global Handling, Problem Details)

### DO
- Use global exception middleware/filters for centralized handling.
- Return consistent Problem Details payloads.
- Distinguish domain errors, validation errors, and unexpected exceptions.

### DON'T
- Do not expose stack traces or internal exception messages to clients.
- Do not use generic `catch (Exception)` unless rethrowing or translating with context.

### Good vs Bad

#### Bad
```csharp
try
{
    // ...
}
catch (Exception ex)
{
    return BadRequest(ex.Message);
}
```

#### Good
```csharp
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var problem = Results.Problem(
            title: "Unexpected error",
            statusCode: StatusCodes.Status500InternalServerError,
            type: "https://httpstatuses.com/500");

        await problem.ExecuteAsync(context);
    });
});
```

## 7) Architecture (Layered, Clean Architecture, Microservices)

### DO
- Separate API, application, domain, and infrastructure concerns.
- Keep domain model independent of transport/persistence frameworks.
- Define stable contracts at service boundaries.
- Use outbox/event-driven patterns when cross-service consistency is needed.

### DON'T
- Do not reference infrastructure directly from domain layer.
- Do not leak ORM entities across bounded contexts.

## 8) Testing (Unit, Integration, Coverage)

### DO
- Unit test business rules and edge cases.
- Integration test API, persistence, and auth boundaries.
- Mock only true external dependencies.
- Enforce minimum coverage goals for critical components (for example, >= 70% line coverage in core domain/application projects).

### DON'T
- Do not write brittle tests coupled to implementation details.
- Do not rely only on happy-path tests.

### Good vs Bad

#### Bad
```csharp
[Fact]
public void RegisterUser_Works() { Assert.True(true); }
```

#### Good
```csharp
[Fact]
public async Task RegisterAsync_InvalidEmail_ThrowsValidationException()
{
    var command = new RegisterUserCommand("bad-email", "Vivek");

    await Assert.ThrowsAsync<ValidationException>(() => _service.RegisterAsync(command, CancellationToken.None));
}
```

## 9) Database Practices (Indexes, Migrations, Transactions)

### DO
- Create indexes for high-selectivity predicates and frequent joins.
- Keep migrations atomic, reversible when feasible, and reviewed.
- Use explicit transactions for multi-step consistency requirements.
- Tune queries with execution plans and realistic dataset sizes.

### DON'T
- Do not run schema-breaking migrations without rollout/rollback plan.
- Do not use serializable transactions by default.

## 10) Cloud Readiness (Config, Environment Separation)

### DO
- Use environment-specific config overrides and strongly typed options.
- Keep production-safe defaults; fail fast for missing required config.
- Externalize stateful dependencies and use health checks/readiness probes.
- Configure resilient outbound calls (timeouts, retries, circuit breakers).

### DON'T
- Do not couple behavior to local-only assumptions.
- Do not store environment secrets in source control.

## Review Checklist (PR Gate)

- API contract follows versioning, validation, and Problem Details conventions.
- AuthZ/AuthN and secret handling meet security requirements.
- Logs are structured and include correlation context.
- Async, query shape, and caching are performance-conscious.
- Architecture boundaries are respected.
- Tests are meaningful and cover critical paths/edge cases.
- Migrations/indexes/transactions are safe and justified.
- Cloud config and environment separation are production-ready.

If any checklist item fails, provide a concrete fix before approving.
