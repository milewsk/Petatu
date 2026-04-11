using NetArchTest.Rules;
using Shouldly;

namespace Petatu.CleanArchitectureTests.Layers;

public class ApplicationLayerTests : BaseTests
{
    public void ApplicationLayer_Should_NotHaveDependencyOn_PresentationLayer()
    {
        TestResult result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn("Web")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    public void ApplicationLayer_Should_NotHaveDependencyOn_InfrastructureLayer()
    {
        TestResult result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn("Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    public void ApplicationLayer_Should_HaveDependencyOn_DomainLayer()
    {
        TestResult result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .HaveDependencyOn("Domain")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }
}
