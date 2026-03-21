---
description: "Use when writing unit tests, integration tests, or architecture tests for the Petatu API. Covers xUnit 2, FluentAssertions 7, Moq, Shouldly, and NetArchTest.Rules conventions."
applyTo: "API/tests/**/*.cs"
---

# Testing Patterns — Petatu API

## Test Project Layout

```
API/tests/
  Petatu.Domain.Tests/           — unit tests for domain logic (entities, value objects)
  Petatu.Application.Tests/      — unit tests for handlers (mock repositories)
  Petatu.Architecture.Tests/     — NetArchTest architecture enforcement
  Petatu.Integration.Tests/      — WebApplicationFactory integration tests (API layer)
```

## Naming Conventions

- **Test class**: `<SubjectUnderTest>Tests` (e.g., `CreatePetHandlerTests`)
- **Test method**: `<MethodOrScenario>_<StateUnderTest>_<ExpectedBehavior>`
  - `Handle_WhenOwnerDoesNotExist_ThrowsNotFoundException`
  - `Handle_WithValidCommand_ReturnsPetId`
  - `Create_WithEmptyName_ThrowsDomainException`

## xUnit Structure

```csharp
namespace Petatu.Application.Tests.Pets.Commands;

public sealed class CreatePetHandlerTests
{
    private readonly Mock<IPetRepository> _petRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private CreatePetHandler CreateSut() =>
        new(_petRepositoryMock.Object, _unitOfWorkMock.Object);

    [Fact]
    public async Task Handle_WithValidCommand_AddsPetAndReturnsId()
    {
        // Arrange
        var command = new CreatePetCommand("Burek", Species.Dog, Guid.NewGuid(), new DateOnly(2020, 1, 1));
        var sut = CreateSut();

        // Act
        var result = await sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _petRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Pet>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

## FluentAssertions

Prefer FluentAssertions over bare xUnit asserts for readability:

```csharp
// Collections
result.Should().NotBeEmpty();
result.Should().HaveCount(3);
result.Should().ContainSingle(p => p.Name == "Burek");

// Objects
pet.Should().NotBeNull();
pet!.Name.Should().Be("Burek");
pet.Species.Should().Be(Species.Dog);

// Exceptions
act.Should().ThrowAsync<NotFoundException>()
   .WithMessage("*Pet*not found*");

// Numeric / date
entry.QuantityGrams.Should().BePositive();
entry.LoggedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
```

## Moq Patterns

```csharp
// Setup return
_petRepositoryMock
    .Setup(r => r.GetByIdAsync(petId, It.IsAny<CancellationToken>()))
    .ReturnsAsync(pet);

// Setup null (not found)
_petRepositoryMock
    .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync((Pet?)null);

// Verify call
_petRepositoryMock.Verify(
    r => r.AddAsync(It.Is<Pet>(p => p.Name == "Burek"), It.IsAny<CancellationToken>()),
    Times.Once);

// Verify no calls
_unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
```

## Validator Tests

Test validators independently from handlers:

```csharp
public sealed class CreatePetValidatorTests
{
    private readonly CreatePetValidator _sut = new();

    [Fact]
    public void Validate_WithEmptyName_HasValidationError()
    {
        var command = new CreatePetCommand("", Species.Dog, Guid.NewGuid(), new DateOnly(2020, 1, 1));
        var result = _sut.Validate(command);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreatePetCommand.Name));
    }

    [Fact]
    public void Validate_WithValidCommand_PassesValidation()
    {
        var command = new CreatePetCommand("Burek", Species.Dog, Guid.NewGuid(), new DateOnly(2020, 1, 1));
        var result = _sut.Validate(command);
        result.IsValid.Should().BeTrue();
    }
}
```

## Architecture Tests (NetArchTest)

Place in `Petatu.Architecture.Tests/`. These enforce Clean Architecture boundaries automatically:

```csharp
public sealed class ArchitectureTests
{
    private static readonly Assembly DomainAssembly       = typeof(Petatu.Domain.AssemblyReference).Assembly;
    private static readonly Assembly ApplicationAssembly  = typeof(Petatu.Application.AssemblyReference).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Petatu.Infrastructure.AssemblyReference).Assembly;
    private static readonly Assembly WebAssembly          = typeof(Petatu.Web.Program).Assembly;

    [Fact]
    public void Domain_ShouldNot_ReferenceApplication()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should().NotHaveDependencyOn("Petatu.Application")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Domain_ShouldNot_ReferenceInfrastructure()
    {
        var result = Types.InAssembly(DomainAssembly)
            .Should().NotHaveDependencyOn("Petatu.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Application_ShouldNot_ReferenceInfrastructure()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .Should().NotHaveDependencyOn("Petatu.Infrastructure")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Handlers_ShouldBe_Sealed()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That().HaveNameEndingWith("Handler")
            .Should().BeSealed()
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }
}
```

## Integration Tests

Use `Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program>` for end-to-end HTTP tests. Keep a shared `IntegrationTestBase` with a test DB connection.

```csharp
public abstract class IntegrationTestBase : IClassFixture<WebApplicationFactory<Program>>
{
    protected readonly HttpClient Client;

    protected IntegrationTestBase(WebApplicationFactory<Program> factory)
    {
        Client = factory.CreateClient();
    }
}
```

## Rules

- Each test class tests **one subject** (handler, validator, entity, or controller endpoint)
- No production code changes to make tests pass unless the production code is genuinely wrong
- Use `[Fact]` for single-case tests, `[Theory]` + `[InlineData]` / `[MemberData]` for parameterized tests
- Do not use `Thread.Sleep` — use `async/await` with `CancellationToken`
- Test file location mirrors the production file: `Petatu.Application.Tests/Pets/Commands/CreatePetHandlerTests.cs`
