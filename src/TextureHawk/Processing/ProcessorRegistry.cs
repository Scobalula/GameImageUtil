using System.Collections.Frozen;

namespace TextureHawk.Processing;

/// <summary>
/// Thread-safe registry of all available processors.
/// </summary>
public sealed class ProcessorRegistry
{
    private readonly object _lock = new();
    private Dictionary<string, IProcessor> _processors = [];
    private FrozenDictionary<string, IProcessor>? _frozen;

    /// <summary>
    /// Registers a processor. Replaces any existing processor with the same ID.
    /// </summary>
    public void Register(IProcessor processor)
    {
        lock (_lock)
        {
            _processors[processor.Id] = processor;
            _frozen = null;
        }
    }

    /// <summary>
    /// Unregisters a processor by ID.
    /// </summary>
    public bool Unregister(string id)
    {
        lock (_lock)
        {
            var removed = _processors.Remove(id);
            if (removed) _frozen = null;
            return removed;
        }
    }

    /// <summary>
    /// Gets a processor by ID, or null if not found.
    /// </summary>
    public IProcessor? GetProcessor(string id)
    {
        var snapshot = GetSnapshot();
        return snapshot.TryGetValue(id, out var processor) ? processor : null;
    }

    /// <summary>
    /// Gets the first registered processor of the requested type, or null if not found.
    /// </summary>
    public TProcessor? GetProcessor<TProcessor>() where TProcessor : class, IProcessor
    {
        return GetSnapshot().Values.OfType<TProcessor>().FirstOrDefault();
    }

    /// <summary>
    /// Gets all registered processors.
    /// </summary>
    public IReadOnlyCollection<IProcessor> GetAll() => GetSnapshot().Values;

    /// <summary>
    /// Gets processors grouped by category.
    /// </summary>
    public IReadOnlyDictionary<string, List<IProcessor>> GetByCategory()
    {
        var result = new Dictionary<string, List<IProcessor>>();
        foreach (var processor in GetSnapshot().Values)
        {
            if (!result.TryGetValue(processor.Category, out var list))
            {
                list = [];
                result[processor.Category] = list;
            }
            list.Add(processor);
        }
        return result;
    }

    private FrozenDictionary<string, IProcessor> GetSnapshot()
    {
        var snapshot = _frozen;
        if (snapshot is not null) return snapshot;

        lock (_lock)
        {
            return _frozen ??= _processors.ToFrozenDictionary();
        }
    }
}
