using Spectre.Console.Cli;

namespace TextureHawk.CLI;

internal sealed class CliTypeRegistrar : ITypeRegistrar
{
    private readonly Dictionary<Type, Type> _registrations = [];
    private readonly Dictionary<Type, object> _instances = [];

    public ITypeResolver Build()
    {
        return new CliTypeResolver(_registrations, _instances);
    }

    public void Register(Type service, Type implementation)
    {
        _registrations[service] = implementation;
    }

    public void RegisterInstance(Type service, object implementation)
    {
        _instances[service] = implementation;
    }

    public void RegisterLazy(Type service, Func<object> factory)
    {
        _instances[service] = new Lazy<object>(factory);
    }
}