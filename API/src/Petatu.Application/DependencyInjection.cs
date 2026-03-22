using DefaultNamespace;
using Microsoft.Extensions.DependencyInjection;

namespace Petatu.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            
            config.AddOpenBehavior(typeof(RequestLoggingPipelineBehaviour<,>));
            config.AddOpenBehavior(typeof(Behaviors.ValidationBehavior<,>));
        });
        
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        
        return services;
    }
}
