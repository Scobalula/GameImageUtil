using System.Runtime.InteropServices;

namespace TextureHawk.Gpu;

/// <summary>
/// Builds push constant byte data by setting named fields from a <see cref="PushConstantLayout"/>.
/// </summary>
/// <remarks>
/// Eliminates manual byte-offset bookkeeping by using field names
/// that match the GLSL <c>layout(push_constant) uniform</c> declaration.
/// </remarks>
public sealed class PushConstantBuilder
{
    private readonly PushConstantLayout _layout;
    private readonly byte[] _data;

    internal PushConstantBuilder(PushConstantLayout layout)
    {
        _layout = layout;
        _data = new byte[layout.TotalSize];
    }

    /// <summary>
    /// Sets an <c>int</c> field by name.
    /// </summary>
    public PushConstantBuilder Set(string name, int value)
    {
        var field = _layout.GetField(name);
        MemoryMarshal.Write(_data.AsSpan(field.Offset), in value);
        return this;
    }

    /// <summary>
    /// Sets a <c>float</c> field by name.
    /// </summary>
    public PushConstantBuilder Set(string name, float value)
    {
        var field = _layout.GetField(name);
        MemoryMarshal.Write(_data.AsSpan(field.Offset), in value);
        return this;
    }

    /// <summary>
    /// Sets a <c>uint</c> field by name.
    /// </summary>
    public PushConstantBuilder Set(string name, uint value)
    {
        var field = _layout.GetField(name);
        MemoryMarshal.Write(_data.AsSpan(field.Offset), in value);
        return this;
    }

    /// <summary>
    /// Returns a copy of the push constant byte data.
    /// </summary>
    public byte[] Build() => _data.ToArray();
}
