---
description: "Use when creating commands, queries, handlers, or validators in Petatu.Application. Covers CQRS structure with MediatR 12 and FluentValidation 12 pipeline integration."
---

# CQRS + MediatR Patterns — Petatu

## Folder Structure per Use Case

Every use case lives in its own subfolder under the feature folder:

```
Petatu.Application/
  Pets/
    Commands/
      CreatePet/
        CreatePetCommand.cs      ← IRequest<Guid>
        CreatePetHandler.cs      ← IRequestHandler<CreatePetCommand, Guid>
        CreatePetValidator.cs    ← AbstractValidator<CreatePetCommand>
    Queries/
      GetPetById/
        GetPetByIdQuery.cs       ← IRequest<PetDto>
        GetPetByIdHandler.cs     ← IRequestHandler<GetPetByIdQuery, PetDto>
      GetPetsByOwner/
        GetPetsByOwnerQuery.cs
        GetPetsByOwnerHandler.cs
```

## Command Pattern

Commands mutate state. Return minimal data (typically `Guid` for created resource, or a dedicated response record).

```csharp
// CreatePetCommand.cs
namespace Petatu.Application.Pets.Commands.CreatePet;

public record CreatePetCommand(
    string Name,
    Species Species,
    Guid OwnerId,
    DateOnly DateOfBirth
) : IRequest<Guid>;
```

```csharp
// CreatePetHandler.cs
namespace Petatu.Application.Pets.Commands.CreatePet;

public sealed class CreatePetHandler(IPetRepository petRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<CreatePetCommand, Guid>
{
    public async Task<Guid> Handle(CreatePetCommand request, CancellationToken cancellationToken)
    {
        var pet = Pet.Create(request.Name, request.Species, request.OwnerId, request.DateOfBirth);

        await petRepository.AddAsync(pet, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return pet.Id;
    }
}
```

## Query Pattern

Queries are read-only. They return DTOs — never domain entities.

```csharp
// GetPetByIdQuery.cs
namespace Petatu.Application.Pets.Queries.GetPetById;

public record GetPetByIdQuery(Guid PetId) : IRequest<PetDto?>;
```

```csharp
// GetPetByIdHandler.cs
namespace Petatu.Application.Pets.Queries.GetPetById;

public sealed class GetPetByIdHandler(IPetRepository petRepository)
    : IRequestHandler<GetPetByIdQuery, PetDto?>
{
    public async Task<PetDto?> Handle(GetPetByIdQuery request, CancellationToken cancellationToken)
    {
        var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken);
        return pet is null ? null : new PetDto(pet.Id, pet.Name, pet.Species);
    }
}
```

## DTO Pattern

DTOs are `record` types, defined close to the query that produces them or in a shared `Dtos/` folder within the feature.

```csharp
public record PetDto(Guid Id, string Name, Species Species);
```

## Validator Pattern

Every command that mutates state must have a corresponding `AbstractValidator<T>`. Validation runs automatically via `ValidationBehaviour` before the handler.

```csharp
// CreatePetValidator.cs
namespace Petatu.Application.Pets.Commands.CreatePet;

public sealed class CreatePetValidator : AbstractValidator<CreatePetCommand>
{
    public CreatePetValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.OwnerId)
            .NotEmpty();

        RuleFor(x => x.DateOfBirth)
            .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth cannot be in the future.");
    }
}
```

## ValidationBehaviour

The `ValidationBehaviour` runs all `IValidator<TRequest>` implementations before the handler. If any validator fails, it should throw a `ValidationException` (or return an error result) so the pipeline short-circuits.

```csharp
// Common/Behaviours/ValidationBehaviour.cs
namespace Petatu.Application.Common.Behaviours;

public sealed class ValidationBehaviour<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

## UnhandledExceptionBehaviour

Catches unexpected exceptions, logs them via `ILogger`, and rethrows.

```csharp
// Common/Behaviours/UnhandledExceptionBehaviour.cs
namespace Petatu.Application.Common.Behaviours;

public sealed class UnhandledExceptionBehaviour<TRequest, TResponse>(ILogger<TRequest> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception ex) when (ex is not ValidationException)
        {
            logger.LogError(ex, "Unhandled exception for request {RequestName}: {@Request}", typeof(TRequest).Name, request);
            throw;
        }
    }
}
```

## Rules

- Handlers are `sealed` — they are not designed for inheritance
- Use `primary constructors` for dependencies
- Handlers depend only on repository interfaces and `IUnitOfWork` — never on `DbContext`
- Queries must not modify state
- Never return domain entities from handlers — always map to DTOs
- Validators are optional for queries (typically no validation needed)
