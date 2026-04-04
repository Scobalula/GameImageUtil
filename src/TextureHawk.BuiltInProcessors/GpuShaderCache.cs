using System.Collections.Concurrent;
using TextureHawk.Gpu;

namespace TextureHawk.BuiltInProcessors;

/// <summary>
/// Caches compiled SPIR-V bytecode and push constant layouts for built-in GPU compute shaders.
/// </summary>
/// <remarks>
/// Loads shaders from disk so they can be inspected and edited without rebuilding the assembly.
/// Prefers pre-compiled .spv files when available and falls back to runtime GLSL compilation
/// from the .comp source on disk. Push constant layouts are parsed from the GLSL source and
/// cached for the lifetime of the process.
/// </remarks>
internal static class GpuShaderCache
{
    private static readonly ConcurrentDictionary<string, byte[]> s_shaderCache = new();
    private static readonly ConcurrentDictionary<string, PushConstantLayout> s_layoutCache = new();
    private static readonly string[] s_shaderDirectories =
    [
        Path.Combine(AppContext.BaseDirectory, "Shaders"),
        Path.Combine(AppContext.BaseDirectory, "TextureHawk.BuiltInProcessors", "Shaders"),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "TextureHawk.BuiltInProcessors", "Shaders")),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "src", "TextureHawk.BuiltInProcessors", "Shaders"))
    ];

    /// <summary>
    /// Gets compiled shader bytecode for the named shader, compiling from GLSL source if needed.
    /// </summary>
    /// <param name="shaderName">Shader name without extension (e.g., "resize").</param>
    /// <returns>Compiled shader bytecode.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the shader cannot be loaded or compiled.</exception>
    internal static byte[] GetShaderBytecode(string shaderName)
    {
        return s_shaderCache.GetOrAdd(shaderName, static name =>
        {
            var spvPath = ResolveShaderPath(name, ".spv");
            if (spvPath is not null)
                return File.ReadAllBytes(spvPath);

            var glslPath = ResolveShaderPath(name, ".comp") ?? throw new InvalidOperationException($"Shader '{name}' not found on disk. Expected '{name}.spv' or '{name}.comp' in a Shaders directory.");
            var glslText = File.ReadAllText(glslPath);

            return ShaderCompiler.Compile(glslText, name);
        });
    }

    /// <summary>
    /// Gets the push constant layout for the named shader, parsed from GLSL source.
    /// </summary>
    /// <param name="shaderName">Shader name without extension (e.g., "resize").</param>
    /// <returns>The parsed <see cref="PushConstantLayout"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the GLSL source is not found.</exception>
    internal static PushConstantLayout GetLayout(string shaderName)
    {
        return s_layoutCache.GetOrAdd(shaderName, static name =>
        {
            var glslSource = GetGlslSource(name);
            return PushConstantLayout.Parse(glslSource);
        });
    }

    /// <summary>
    /// Gets the raw GLSL source for the named shader from embedded resources.
    /// </summary>
    internal static string GetGlslSource(string shaderName)
    {
        var glslPath = ResolveShaderPath(shaderName, ".comp") ?? throw new InvalidOperationException($"GLSL source '{shaderName}.comp' not found on disk.");

        return File.ReadAllText(glslPath);
    }

    private static string? ResolveShaderPath(string shaderName, string extension)
    {
        var fileName = $"{shaderName}{extension}";

        foreach (var directory in s_shaderDirectories)
        {
            var candidate = Path.Combine(directory, fileName);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }
}
