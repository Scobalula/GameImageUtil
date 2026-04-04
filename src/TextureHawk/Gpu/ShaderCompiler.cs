using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.Shaderc;

namespace TextureHawk.Gpu;

/// <summary>
/// Compiles GLSL shader sources to SPIR-V bytecode using the bundled shaderc library.
/// </summary>
/// <remarks>
/// Uses Silk.NET.Shaderc (which bundles the native shaderc library) so no external
/// Vulkan SDK or glslangValidator installation is required. Thread-safe: the shaderc
/// compiler instance is created per call.
/// </remarks>
public static class ShaderCompiler
{
    /// <summary>
    /// Compiles a GLSL compute shader source to SPIR-V bytecode.
    /// </summary>
    /// <param name="glslSource">The GLSL source code.</param>
    /// <param name="shaderName">A descriptive name for error messages.</param>
    /// <returns>The compiled SPIR-V bytecode.</returns>
    /// <exception cref="InvalidOperationException">Thrown when compilation fails.</exception>
    public static unsafe byte[] Compile(string glslSource, string shaderName)
    {
        var api = Shaderc.GetApi();

        var compiler = api.CompilerInitialize();
        if (compiler == null)
            throw new InvalidOperationException("Failed to initialize shaderc compiler.");

        try
        {
            var options = api.CompileOptionsInitialize();
            if (options == null)
            {
                api.CompilerRelease(compiler);
                throw new InvalidOperationException("Failed to initialize shaderc compile options.");
            }

            try
            {
                // Target Vulkan 1.2
                api.CompileOptionsSetTargetEnv(options, TargetEnv.Vulkan, (uint)EnvVersion.Vulkan12);
                api.CompileOptionsSetOptimizationLevel(options, OptimizationLevel.Performance);

                var sourceBytes = Encoding.UTF8.GetBytes(glslSource);
                fixed (byte* pSource = sourceBytes)
                {
                    var result = api.CompileIntoSpv(
                        compiler,
                        pSource,
                        (nuint)sourceBytes.Length,
                        ShaderKind.ComputeShader,
                        shaderName,
                        "main",
                        options);

                    if (result == null)
                        throw new InvalidOperationException($"Shader compilation returned null for '{shaderName}'.");

                    try
                    {
                        var status = api.ResultGetCompilationStatus(result);
                        if (status != CompilationStatus.Success)
                        {
                            var errorPtr = api.ResultGetErrorMessage(result);
                            var errorMsg = errorPtr != null
                                ? Marshal.PtrToStringUTF8((nint)errorPtr) ?? "Unknown error"
                                : "Unknown error";
                            throw new InvalidOperationException(
                                $"Shader compilation failed for '{shaderName}' (status {status}):\n{errorMsg}");
                        }

                        var length = (int)api.ResultGetLength(result);
                        var bytesPtr = api.ResultGetBytes(result);
                        if (bytesPtr == null || length == 0)
                            throw new InvalidOperationException($"Shader compilation produced no output for '{shaderName}'.");

                        var spirv = new byte[length];
                        new ReadOnlySpan<byte>(bytesPtr, length).CopyTo(spirv);
                        return spirv;
                    }
                    finally
                    {
                        api.ResultRelease(result);
                    }
                }
            }
            finally
            {
                api.CompileOptionsRelease(options);
            }
        }
        finally
        {
            api.CompilerRelease(compiler);
        }
    }
}
