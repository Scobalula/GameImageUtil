using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11.Shader;

namespace GameImageUtil.ShaderCode
{
    internal class ShaderHelper
    {
        public static ShaderResource[] GenerateResources(Blob shaderBlob)
        {
            return GenerateResources(shaderBlob, out _, out _, out _);
        }

        public static ShaderResource[] GenerateResources(Blob shaderBlob,
                                                         out int xThreads,
                                                         out int yThreads,
                                                         out int zThreads)
        {
            using var reflection = Compiler.Reflect<ID3D11ShaderReflection>(shaderBlob.AsSpan());

            var resources = reflection.Resources;

            xThreads = reflection.ThreadGroupSize.X;
            yThreads = reflection.ThreadGroupSize.Y;
            zThreads = reflection.ThreadGroupSize.Z;

            var results = new ShaderResource[resources.Length];

            for (int i = 0; i < resources.Length; i++)
            {
                var resource = resources[i];

                results[i] = new ShaderResource(
                    resource.Name,
                    resource.Type switch
                    {
                        ShaderInputType.Texture                    => ShaderResourceType.InputTexture,
                        ShaderInputType.Sampler                    => ShaderResourceType.Sampler,
                        ShaderInputType.UnorderedAccessViewRWTyped => ShaderResourceType.OutputTexture,
                        ShaderInputType.ConstantBuffer             => ShaderResourceType.ConstantBuffer,
                        _                                          => ShaderResourceType.Unknown
                    }, resource.BindPoint);
            }

            return results;
        }
    }
}
