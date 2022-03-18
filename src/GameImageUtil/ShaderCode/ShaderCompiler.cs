using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.D3DCompiler;
using Vortice.Direct3D;

namespace GameImageUtil.ShaderCode
{
    public static class ShaderCompiler
    {
        public static Blob Compile(string shaderStr,
                                   ShaderType type,
                                   ShaderMacro[]? macros,
                                   ShaderInclude? include)
        {
            // TODO: Caching based off macros + str?
            // TODO: Move to helper and merge switch
            // TODO: Remove hardcoded SM5.0 
            string entryPoint = type switch
            {
                ShaderType.VertexShader => $"vs_main",
                ShaderType.HullShader => $"hs_main",
                ShaderType.DomainShader => $"ds_main",
                ShaderType.GeometryShader => $"gs_main",
                ShaderType.PixelShader => $"ps_main",
                ShaderType.ComputShader => $"cs_main",
                _ => $"unknown",
            };
            string profile = type switch
            {
                ShaderType.VertexShader => $"vs_5_0",
                ShaderType.HullShader => $"hs_5_0",
                ShaderType.DomainShader => $"ds_5_0",
                ShaderType.GeometryShader => $"gs_5_0",
                ShaderType.PixelShader => $"ps_5_0",
                ShaderType.ComputShader => $"cs_5_0",
                _ => $"vs_5_0",
            };

            // TODO: Set up exception
            if (Compiler.Compile(
                shaderStr,
                macros!,
                include!,
                entryPoint,
                "anon",
                profile,
                ShaderFlags.None,
                EffectFlags.None,
                out var blob,
                out var errorBlob
                ).Failure)
                throw new ShaderCompileException(errorBlob.AsString());


            return blob;
        }
    }
}
