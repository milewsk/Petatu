---
description: "Generates xUnit unit tests for a Petatu handler, validator, or domain entity. Covers Arrange/Act/Assert with FluentAssertions, Moq, and architecture tests with NetArchTest."
agent: "agent"
argument-hint: "Path to the class to test (e.g., 'Petatu.Application/Pets/Commands/CreatePet/CreatePetHandler.cs')"
tools: ["read", "search", "edit"]
---

Generate comprehensive xUnit tests for the following class: **$ARGUMENTS**

Follow the instructions in [testing-xunit.instructions.md](../instructions/testing-xunit.instructions.md).

## Steps

### 1. Read the subject under test

- Read the target file
- Identify all public methods and their signatures
- Note dependencies (constructor parameters) — these will be mocked

### 2. Read related files

- Read the corresponding command/query record
- Read repository interface(s) used by the handler
- Read any validators for the same use case
- Check for domain exceptions that can be thrown

### 3. Determine test file location

Mirror the production path under `API/tests/`:
- `API/src/Petatu.Application/Pets/Commands/CreatePet/CreatePetHandler.cs`
  → `API/tests/Petatu.Application.Tests/Pets/Commands/CreatePet/CreatePetHandlerTests.cs`

### 4. Write tests

For each **handler**, cover:
- Happy path — valid input, returns expected result, calls repository and unit of work correctly
- Not found — when `GetByIdAsync` returns `null`, throws the domain `NotFoundException`
- Dependency verification — verify `SaveChangesAsync` is called (or not) as expected

For each **validator**, cover:
- One test per validation rule that can fail
- One test for a fully valid input passing validation
- Use `result.IsValid.Should().BeFalse()` and check `result.Errors` for the specific property

For each **domain entity**, cover:
- Factory method creates entity with correct property values
- Domain events are raised (`entity.DomainEvents.Should().ContainSingle(...)`)
- Any invariant that throws an exception

### 5. Test structure rules

- One `[Fact]` or `[Theory]` per scenario
- Mocks declared as `private readonly Mock<T>` fields, instantiated in field initializers
- Use a `CreateSut()` helper method to construct the subject under test
- Method names follow: `<Method>_<State>_<ExpectedBehavior>`
- Use `CancellationToken.None` in tests
- No `Thread.Sleep`, no real HTTP calls, no real database
