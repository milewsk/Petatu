# Petatu — Copilot Instructions

## What Is Petatu?

Petatu is a multi-platform **pet diet monitoring** application — think Fitatu, but for animals. Users track their pets' daily food intake, nutrition targets, body weight, and diet history.

The platform consists of:
- `API/` — .NET 9 ASP.NET Core Web API (primary focus, built first)
- `WebApp/` — React web frontend
- `MobileApp/` — mobile client

---

## Solution Layout

| Project | Role |
|---|---|
| `Petatu.Domain` | Entities, value objects, domain events, repository interfaces, domain exceptions |
| `Petatu.Application` | CQRS use cases (commands/queries/handlers), DTOs, FluentValidation validators, MediatR pipeline behaviours |
| `Petatu.Infrastructure` | EF Core DbContext, repository implementations, PostgreSQL, external services |
| `Petatu.Web` | ASP.NET Core entry point, controllers, middleware, DI composition |

Solution file: `API/Petatu.sln`

---

## Core Domain Entities

| Entity | Description |
|---|---|
| `User` | Pet owner / account |
| `Pet` | Animal being monitored (name, species, breed, dateOfBirth, ownerId) |
| `Breed` | Breed reference data (name, species) |
| `FoodItem` | Food/product with nutritional values per 100 g (kcal, protein, fat, carbohydrates) |
| `DietEntry` | A pet's food consumption record (petId, foodItemId, quantityGrams, loggedAt) |
| `NutritionGoal` | Daily nutrient targets per pet |
| `WeightEntry` | Historical weight measurement for a pet |

---

## Clean Architecture Rules

- **Dependency direction**: `Domain` ← `Application` ← `Infrastructure` / `Web`
- `Domain` and `Application` must **never** reference `Infrastructure` or `Web`
- Controllers never touch `DbContext` or repositories — only MediatR (`ISender`)
- Handlers never touch `DbContext` directly — only repository interfaces from `Domain`

---

## File Placement

| Type | Location |
|---|---|
| Entity | `Petatu.Domain/Entities/` |
| Repository interface | `Petatu.Domain/Repositories/` |
| Domain event | `Petatu.Domain/Events/<Entity>/` |
| Domain exception | `Petatu.Domain/Exceptions/<Entity>/` |
| Value object | `Petatu.Domain/Common/` |
| Command + Handler + Validator | `Petatu.Application/<Feature>/Commands/<UseCaseName>/` |
| Query + Handler | `Petatu.Application/<Feature>/Queries/<UseCaseName>/` |
| MediatR pipeline behaviour | `Petatu.Application/Common/Behaviours/` |
| EF entity configuration | `Petatu.Infrastructure/Data/Configurations/` |
| EF migrations | `Petatu.Infrastructure/Data/Migrations/` |
| Repository implementation | `Petatu.Infrastructure/Repositories/` |
| Controller | `Petatu.Web/Controllers/` |

> For detailed coding patterns see `.github/instructions/`.

---

## Coding Standards

- **C# 13 / .NET 9** — use primary constructors, collection expressions, and file-scoped namespaces
- **Nullable reference types** enabled — annotate all nullability correctly
- **Implicit usings** — no redundant `using` directives
- `record` types for DTOs, commands, and queries
- Naming: `<UseCase>Command`, `<UseCase>Query`, `<UseCase>Handler`, `<UseCase>Validator`
- Keep **controllers thin**: validate HTTP contract → call MediatR → return result
- Use **Result / Error types** for expected failures; exceptions only for truly unexpected errors

---

## Tech Stack

| Concern | Library / Version |
|---|---|
| ORM | EF Core 9, Npgsql (PostgreSQL) |
| DB column naming | `EFCore.NamingConventions` → snake_case |
| Mediator | MediatR 12 |
| Validation | FluentValidation 12 + `ValidationBehaviour` pipeline |
| Logging | Serilog → Console, Seq, PostgreSQL sinks |
| Auth | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| API docs | Built-in OpenAPI (`AddOpenApi()` / `MapOpenApi()`) |
| Testing | xUnit 2, FluentAssertions 7, Moq, Shouldly, NetArchTest.Rules |
| Guard clauses | `throw` library |

Do **not** introduce new NuGet packages without discussion.

---

## Build & Test Commands

```bash
# Build
dotnet build API/Petatu.sln

# Run API
dotnet run --project API/src/Petatu.Web

# Run all tests
dotnet test API/Petatu.sln

# Add EF Core migration (run from solution root)
dotnet ef migrations add <MigrationName> \
  --project API/src/Petatu.Infrastructure \
  --startup-project API/src/Petatu.Web

# Apply migrations
dotnet ef database update \
  --project API/src/Petatu.Infrastructure \
  --startup-project API/src/Petatu.Web
```

---

## DI Registration

- Register all Infrastructure services in `Petatu.Infrastructure/DependencyInjection.cs` as `IServiceCollection` extension method
- Register all Application services in `Petatu.Application/DependencyInjection.cs` the same way
- `Program.cs` only calls `builder.Services.AddApplication()` and `builder.Services.AddInfrastructure(builder.Configuration)`
