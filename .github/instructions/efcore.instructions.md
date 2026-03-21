---
description: "Use when writing EF Core entity configurations, modifying ApplicationDbContext, or creating database migrations. Covers PostgreSQL, snake_case naming, IEntityTypeConfiguration, and migration safety rules."
---

# EF Core Patterns — Petatu

## DbContext

`ApplicationDbContext` in `Petatu.Infrastructure/Data/ApplicationDbContext.cs`. It:
- Applies all `IEntityTypeConfiguration<T>` via `ApplyConfigurationsFromAssembly`
- Uses `UseSnakeCaseNamingConvention()` (EFCore.NamingConventions) — all column and table names are **snake_case** automatically
- Has a `DbSet<T>` per aggregate root entity

```csharp
namespace Petatu.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<FoodItem> FoodItems => Set<FoodItem>();
    public DbSet<DietEntry> DietEntries => Set<DietEntry>();
    public DbSet<NutritionGoal> NutritionGoals => Set<NutritionGoal>();
    public DbSet<WeightEntry> WeightEntries => Set<WeightEntry>();
    public DbSet<Breed> Breeds => Set<Breed>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

## Entity Configuration

One configuration class per entity in `Petatu.Infrastructure/Data/Configurations/`.

```csharp
// PetConfiguration.cs
namespace Petatu.Infrastructure.Data.Configurations;

public class PetConfiguration : IEntityTypeConfiguration<Pet>
{
    public void Configure(EntityTypeBuilder<Pet> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Species)
            .IsRequired()
            .HasConversion<string>();    // store enum as string

        // Relationships
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore domain events — they are not persisted
        builder.Ignore(p => p.DomainEvents);
    }
}
```

## Naming Conventions (snake_case)

`EFCore.NamingConventions` converts everything automatically:
- Table: `DietEntry` → `diet_entries` (pluralized by EF Core)
- Column: `OwnerId` → `owner_id`
- FK: automatically follows the same convention

**Do not** manually specify `.HasColumnName()` or `.ToTable()` unless you need to override the convention.

## Migrations

Run from the **solution root**:

```bash
dotnet ef migrations add <MigrationName> \
  --project API/src/Petatu.Infrastructure \
  --startup-project API/src/Petatu.Web

dotnet ef database update \
  --project API/src/Petatu.Infrastructure \
  --startup-project API/src/Petatu.Web
```

### Migration Safety Rules

- **Never drop a column** in the same migration that removes the property from code. First deploy code that stops writing the column, then drop it in a later migration.
- **Never rename** a column directly — EF Core generates `DROP + ADD`. Use explicit `migrationBuilder.RenameColumn()` instead.
- **Always review** the generated `Up()` and `Down()` methods before committing.
- **Data migrations** that transform existing rows should be separate from schema migrations.
- Use **descriptive names**: `AddPetBreedId`, `CreateDietEntriesTable`, not `Migration1`.

### Migration File Structure

Each migration in `Petatu.Infrastructure/Data/Migrations/` consists of:
- `<Timestamp>_<Name>.cs` — the migration class
- `<Timestamp>_<Name>.Designer.cs` — EF Core snapshot metadata (do not edit)
- `ApplicationDbContextModelSnapshot.cs` — current model snapshot (auto-generated)

## Repository Implementation

```csharp
// PetRepository.cs
namespace Petatu.Infrastructure.Repositories;

public sealed class PetRepository(ApplicationDbContext dbContext) : IPetRepository
{
    public async Task<Pet?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Pets.FindAsync([id], cancellationToken);

    public async Task<IReadOnlyList<Pet>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
        => await dbContext.Pets
            .Where(p => p.OwnerId == ownerId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Pet pet, CancellationToken cancellationToken = default)
        => await dbContext.Pets.AddAsync(pet, cancellationToken);

    public void Update(Pet pet)
        => dbContext.Pets.Update(pet);

    public void Delete(Pet pet)
        => dbContext.Pets.Remove(pet);
}
```

## IUnitOfWork

`IUnitOfWork` is defined in `Petatu.Application` and implemented by `ApplicationDbContext` in `Infrastructure`. Handlers call `SaveChangesAsync` through this interface — never through `DbContext` directly.

```csharp
// Petatu.Application/Common/Interfaces/IUnitOfWork.cs
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

`ApplicationDbContext` implements `IUnitOfWork` (it already has `SaveChangesAsync`). Register it:

```csharp
services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ApplicationDbContext>());
```
