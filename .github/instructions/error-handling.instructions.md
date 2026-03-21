---
description: "Use when implementing error handling, domain exceptions, Result types, or HTTP error responses in Petatu. Covers ValidationException, domain exceptions, ProblemDetails, and the error flow through the MediatR pipeline."
---

# Error Handling Patterns — Petatu

## Error Categories

| Category | Where it originates | How it propagates |
|---|---|---|
| Validation errors | `ValidationBehaviour` (FluentValidation failures) | Throws `ValidationException` → caught by middleware → 400 |
| Domain errors | Domain entity or handler | Throws typed domain exception → caught by middleware → 4xx |
| Unexpected errors | Anywhere | Caught by `UnhandledExceptionBehaviour` → logged → re-thrown → 500 |

## Domain Exceptions

Define typed exceptions per entity in `Petatu.Domain/Exceptions/<Entity>/`. They communicate expected failure states in domain language.

```csharp
// Petatu.Domain/Exceptions/Pet/PetNotFoundException.cs
namespace Petatu.Domain.Exceptions.Pet;

public sealed class PetNotFoundException(Guid petId)
    : Exception($"Pet with id '{petId}' was not found.");
```

```csharp
// Petatu.Domain/Exceptions/Pet/PetAlreadyExistsException.cs
namespace Petatu.Domain.Exceptions.Pet;

public sealed class PetAlreadyExistsException(string name)
    : Exception($"A pet named '{name}' already exists for this owner.");
```

Throw domain exceptions from **handlers** when a domain rule is violated:

```csharp
var pet = await petRepository.GetByIdAsync(request.PetId, cancellationToken)
    ?? throw new PetNotFoundException(request.PetId);
```

## ValidationException

`FluentValidation.ValidationException` is thrown automatically by `ValidationBehaviour`. It carries a collection of `ValidationFailure`.

Never catch `ValidationException` in handlers — let the middleware handle it.

## Global Exception Middleware

Map exceptions to `ProblemDetails` in a global middleware registered in `Petatu.Web`:

```csharp
// Petatu.Web/Middleware/ExceptionHandlingMiddleware.cs
namespace Petatu.Web.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (Exception ex) when (ex.GetType().Namespace?.StartsWith("Petatu.Domain.Exceptions") == true)
        {
            await HandleDomainExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error processing request {Method} {Path}", context.Request.Method, context.Request.Path);
            await HandleUnexpectedExceptionAsync(context);
        }
    }

    private static async Task HandleValidationExceptionAsync(HttpContext context, ValidationException ex)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        var problem = new ValidationProblemDetails(
            ex.Errors.GroupBy(e => e.PropertyName)
                     .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
        {
            Title = "Validation failed",
            Status = StatusCodes.Status400BadRequest
        };
        await context.Response.WriteAsJsonAsync(problem);
    }

    private static async Task HandleDomainExceptionAsync(HttpContext context, Exception ex)
    {
        // NotFoundException → 404, others → 422
        var statusCode = ex is { Message: var msg } && msg.Contains("not found", StringComparison.OrdinalIgnoreCase)
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status422UnprocessableEntity;

        context.Response.StatusCode = statusCode;
        var problem = new ProblemDetails { Title = ex.Message, Status = statusCode };
        await context.Response.WriteAsJsonAsync(problem);
    }

    private static async Task HandleUnexpectedExceptionAsync(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        var problem = new ProblemDetails { Title = "An unexpected error occurred.", Status = 500 };
        await context.Response.WriteAsJsonAsync(problem);
    }
}
```

Register in `Program.cs` **before** `app.MapControllers()`:

```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

## Controller Pattern

Controllers stay thin — they only handle HTTP contract, then delegate to MediatR. Errors surface through the middleware, not in controllers.

```csharp
[ApiController]
[Route("api/pets")]
public sealed class PetsController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(CreatePetRequest request, CancellationToken cancellationToken)
    {
        var command = new CreatePetCommand(request.Name, request.Species, request.OwnerId, request.DateOfBirth);
        var petId = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = petId }, petId);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<PetDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetPetByIdQuery(id), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
```

## Rules

- Domain exceptions carry a **user-readable** message (they go directly into `ProblemDetails.Title`)
- Never expose stack traces or internal details in HTTP responses
- Controllers must not contain `try/catch` — all error handling is centralized in middleware
- `ValidationBehaviour` runs before the handler — handlers can assume input is valid
- Use the `throw` guard library for preconditions inside handlers/entities: `Throw.IfNull(pet)`
