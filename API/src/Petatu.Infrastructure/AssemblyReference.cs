using System.Reflection;

namespace Petatu.Infrastructure;

public static class AssemblyReference
{  
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}