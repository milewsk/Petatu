using System.Reflection;
using Petatu.Application.Common.Abstractions;
using Petatu.Domain.Entities;
using Petatu.Infrastructure.Data.Configurations;
using Petatu.Web.Controllers;

namespace Petatu.CleanArchitectureTests;

public abstract class BaseTests
{
    public static readonly Assembly DomainAssembly = typeof(User).Assembly;
    public static readonly Assembly ApplicationAssembly = typeof(ICommand<>).Assembly;
    public static readonly Assembly InfrastructureAssembly = typeof(UserConfiguration).Assembly;
    public static readonly Assembly WebAssembly = typeof(UserController).Assembly;
}
