---
description: "Scaffolds a complete Clean Architecture feature across all layers: Domain entity, repository interface, EF configuration, repository implementation, command/query/handler, validator, and controller endpoint."
agent: "agent"
argument-hint: "Feature name (e.g., 'Pet', 'DietEntry', 'WeightEntry')"
tools: ["read", "search", "edit", "todo"]
---

Scaffold a complete Clean Architecture feature for the Petatu API.

Feature to scaffold: **$ARGUMENTS**

Follow the instructions in [clean-architecture.instructions.md](../instructions/clean-architecture.instructions.md), [cqrs-mediatr.instructions.md](../instructions/cqrs-mediatr.instructions.md), [efcore.instructions.md](../instructions/efcore.instructions.md), and [error-handling.instructions.md](../instructions/error-handling.instructions.md).

## Steps

Work through these steps in order. Track progress with the todo list.

### 1. Read existing code
- Read `API/src/Petatu.Domain/Entities/User.cs` and `API/src/Petatu.Domain/Common/BaseAuditableEntity.cs` to understand the entity base class pattern
- Read `API/src/Petatu.Domain/Events/User/` to understand domain event naming
- Check if the feature folder already exists in `API/src/Petatu.Application/`

### 2. Domain layer — `Petatu.Domain`

Create:
- **Entity**: `API/src/Petatu.Domain/Entities/<Feature>.cs`
  - Extend `BaseAuditableEntity`
  - Private setters, public factory method `Create(...)`
  - Call `AddDomainEvent(new <Feature>CreatedEvent(this))` in `Create`
- **Domain event**: `API/src/Petatu.Domain/Events/<Feature>/<Feature>CreatedEvent.cs` extending `BaseEvent`
- **Domain exception (NotFoundException)**: `API/src/Petatu.Domain/Exceptions/<Feature>/<Feature>NotFoundException.cs`
- **Repository interface**: `API/src/Petatu.Domain/Repositories/I<Feature>Repository.cs`
  - Methods: `GetByIdAsync`, `AddAsync`, `Update`, `Delete`
  - Return `<Feature>?` for `GetByIdAsync` (nullable)

### 3. Infrastructure layer — `Petatu.Infrastructure`

Create:
- **EF configuration**: `API/src/Petatu.Infrastructure/Data/Configurations/<Feature>Configuration.cs`
  - Implement `IEntityTypeConfiguration<<Feature>>`
  - Configure PK, required properties with max lengths, relationships
  - Ignore `DomainEvents`
- **Repository implementation**: `API/src/Petatu.Infrastructure/Repositories/<Feature>Repository.cs`
  - Implement `I<Feature>Repository`
  - Use `ApplicationDbContext` via primary constructor

Register the repository in `API/src/Petatu.Infrastructure/DependencyInjection.cs`:
```csharp
services.AddScoped<I<Feature>Repository, <Feature>Repository>();
```

Add `DbSet<<Feature>>` to `ApplicationDbContext`.

### 4. Application layer — `Petatu.Application`

Create a **Create** command (minimal first use case):
- `API/src/Petatu.Application/<Feature>s/Commands/Create<Feature>/Create<Feature>Command.cs` — `record` implementing `IRequest<Guid>`
- `API/src/Petatu.Application/<Feature>s/Commands/Create<Feature>/Create<Feature>Handler.cs` — `sealed class` implementing `IRequestHandler<Create<Feature>Command, Guid>`
- `API/src/Petatu.Application/<Feature>s/Commands/Create<Feature>/Create<Feature>Validator.cs` — `AbstractValidator<Create<Feature>Command>`

Create a **GetById** query:
- `API/src/Petatu.Application/<Feature>s/Queries/Get<Feature>ById/Get<Feature>ByIdQuery.cs`
- `API/src/Petatu.Application/<Feature>s/Queries/Get<Feature>ById/Get<Feature>ByIdHandler.cs`
- `API/src/Petatu.Application/<Feature>s/Dtos/<Feature>Dto.cs` — flat `record` with key fields

### 5. Web layer — `Petatu.Web`

Create or update:
- **Controller**: `API/src/Petatu.Web/Controllers/<Feature>sController.cs`
  - `[ApiController]`, `[Route("api/<feature>s")]`
  - `POST /` → `Create<Feature>Command` → `201 Created`
  - `GET /{id:guid}` → `Get<Feature>ByIdQuery` → `200 OK` or `404 Not Found`
  - Use `ISender` via primary constructor
  - Add `[ProducesResponseType]` attributes for OpenAPI

### 6. Verify
- Check that no layer violates the dependency direction
- Confirm all new types use file-scoped namespaces and nullable annotations
- Ensure the new `DbSet` is added to `ApplicationDbContext`
