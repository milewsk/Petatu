using NetArchTest.Rules;
using Petatu.Domain.Common;
using Shouldly;
using BindingFlags = System.Reflection.BindingFlags;

namespace Petatu.CleanArchitectureTests.CleanArchitecture;

public class DomainTests : BaseTests
{
    [Fact]
    public void Entities_Should_Have_Parameterless_Constructor()
    {
        var entityTypes = Types.InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(BaseAuditableEntity))
            .GetTypes();

        var failures = new List<Type>();
        foreach (var entityType in entityTypes)
        {
            var constructor = entityType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
            if (!constructor.Any(c => c.GetParameters().Length == 0) && c.IsPrivate))
            {
                failures.Add(entityType);
            }
        }
   
        failures.ShouldBeEmpty();
    }
}
