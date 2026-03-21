---
description: "Always-on Clean Architecture rules for the Petatu API. Covers layer boundaries, dependency direction, file placement, and naming conventions for .NET projects."
applyTo: "API/src/**/*.cs"
---

# Clean Architecture Rules — Petatu API

## Layer Dependency Direction

```
Petatu.Domain  ←  Petatu.Application  ←  Petatu.Infrastructure
                                      ←  Petatu.Web
```

- `Domain` has **zero** project references — it is the core
- `Application` references only `Domain`
- `Infrastructure` and `Web` reference `Application` (and `Domain` transitively)
- **Never** add a reference from `Domain` or `Application` to `Infrastructure` or `Web`

## What Belongs Where

### Petatu.Domain
- **Entities** → `Entities/<EntityName>.cs`, extend `BaseAuditableEntity`
- **Repository interfaces** → `Repositories/I<EntityName>Repository.cs`
- **Domain events** → `Events/<EntityName>/<EventName>Event.cs`, extend `BaseEvent`
- **Domain exceptions** → `Exceptions/<EntityName>/<ExceptionName>Exception.cs`
- **Value objects** → `Common/<ValueObjectName>.cs`, extend `ValueObject`
- No framework dependencies — no EF Core, no MediatR, no FluentValidation

### Petatu.Application
- **Commands** → `<Feature>/Commands/<UseCaseName>/<UseCaseName>Command.cs`
- **Queries** → `<Feature>/Queries/<UseCaseName>/<UseCaseName>Query.cs`
- **Handlers** → same folder as the command/query they handle
- **Validators** → same folder as the command/query they validate
- **Pipeline behaviours** → `Common/Behaviours/`
- References domain repository interfaces — never implementations
- No EF Core, no `DbContext`, no `HttpContext`

### Petatu.Infrastructure
- **Repository implementations** → `Repositories/<EntityName>Repository.cs`
- **EF Core configurations** → `Data/Configurations/<EntityName>Configuration.cs`
- **DbContext** → `Data/ApplicationDbContext.cs`
- **Migrations** → `Data/Migrations/`
- Implements interfaces declared in `Domain`

### Petatu.Web
- **Controllers** → `Controllers/<Feature>Controller.cs`
- **Middleware** → `Middleware/`
- DI wiring only — `Program.cs` calls `AddApplication()` and `AddInfrastructure()`
- No business logic, no direct repository or DbContext usage

## Naming Conventions

| Type | Pattern | Example |
|---|---|---|
| Entity | `<Name>` | `Pet`, `DietEntry` |
| Repository interface | `I<Name>Repository` | `IPetRepository` |
| Repository implementation | `<Name>Repository` | `PetRepository` |
| Command | `<UseCase>Command` | `CreatePetCommand` |
| Query | `<UseCase>Query` | `GetPetByIdQuery` |
| Handler | `<UseCase>Handler` | `CreatePetHandler` |
| Validator | `<UseCase>Validator` | `CreatePetValidator` |
| EF configuration | `<Name>Configuration` | `PetConfiguration` |
| Domain event | `<Name>Event` | `PetCreatedEvent` |
| Domain exception | `<Name>Exception` | `PetNotFoundException` |

## Entity Structure

All entities extend `BaseAuditableEntity`, which provides audit fields (`CreationDate`, `CreatedBy`, `LastModifiedDate`, `LastModifiedBy`). `BaseEntity` provides `Id` (Guid) and domain event support.

```csharp
// Petatu.Domain/Entities/Pet.cs
namespace Petatu.Domain.Entities;

public class Pet : BaseAuditableEntity
{
    public string Name { get; private set; } = default!;
    public Species Species { get; private set; }
    public Guid OwnerId { get; private set; }

    // Factory method — constructors stay private/protected
    public static Pet Create(string name, Species species, Guid ownerId)
    {
        var pet = new Pet { Name = name, Species = species, OwnerId = ownerId };
        pet.AddDomainEvent(new PetCreatedEvent(pet));
        return pet;
    }
}
```

Use **private setters** and **factory methods** on entities to enforce invariants.

## Repository Interfaces

```csharp
// Petatu.Domain/Repositories/IPetRepository.cs
namespace Petatu.Domain.Repositories;

public interface IPetRepository
{
    Task<Pet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Pet>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task AddAsync(Pet pet, CancellationToken cancellationToken = default);
    void Update(Pet pet);
    void Delete(Pet pet);
}
```

## DI Registration

```csharp
// Petatu.Infrastructure/DependencyInjection.cs
public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
{
    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(config.GetConnectionString("DefaultConnection"))
               .UseSnakeCaseNamingConvention());

    services.AddScoped<IPetRepository, PetRepository>();
    // ... other repositories
    return services;
}

// Petatu.Application/DependencyInjection.cs
public static IServiceCollection AddApplication(this IServiceCollection services)
{
    services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AssemblyReference).Assembly));
    services.AddValidatorsFromAssembly(typeof(AssemblyReference).Assembly);
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));
    return services;
}
```
