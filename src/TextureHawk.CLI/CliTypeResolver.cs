using Spectre.Console.Cli;

namespace TextureHawk.CLI;

internal sealed class CliTypeResolver(IReadOnlyDictionary<Type, Type> registrations, IReadOnlyDictionary<Type, object> instances) : ITypeResolver, IDisposable
{
    public object? Resolve(Type? type)
    {
        if (type is null)
            return null;

        if (instances.TryGetValue(type, out var instance))
            return instance is Lazy<object> lazy ? lazy.Value : instance;

        if (registrations.TryGetValue(type, out var implementation))
            return CreateInstance(implementation);

        return CreateInstance(type);
    }

    public void Dispose()
    {
    }

    private object? CreateInstance(Type type)
    {
        if (type.IsAbstract || type.IsInterface)
            return null;

        var constructor = type.GetConstructors()
            .OrderByDescending(ctor => ctor.GetParameters().Length)
            .FirstOrDefault();
        if (constructor is null)
            return Activator.CreateInstance(type);

        var parameters = constructor.GetParameters();
        if (parameters.Length == 0)
            return Activator.CreateInstance(type);

        var arguments = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++)
        {
            arguments[i] = Resolve(parameters[i].ParameterType)
                ?? throw new InvalidOperationException($"Unable to resolve service '{parameters[i].ParameterType.FullName}' for '{type.FullName}'.");
        }

        return Activator.CreateInstance(type, arguments);
    }
}