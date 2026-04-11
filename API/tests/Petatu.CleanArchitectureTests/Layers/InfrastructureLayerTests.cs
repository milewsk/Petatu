using NetArchTest.Rules;
using Shouldly;

namespace Petatu.CleanArchitectureTests.Layers;

public class InfrastructureLayerTests : BaseTests
{
    public void InfrastructureLayer_Should_NotHaveDependencyOn_PresentationLayer()
    {
        TestResult result = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .NotHaveDependencyOn("Web")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    public void InfrastructureLayer_Should_NotHaveDependencyOn_ApplicationLayer()
    {
        TestResult result = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .NotHaveDependencyOn("Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    public void InfrastructureLayer_Should_HaveDependencyOn_DomainLayer()
    {
        TestResult result = Types.InAssembly(InfrastructureAssembly)
            .Should()
            .HaveDependencyOn("Domain")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }
}
