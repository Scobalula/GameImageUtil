using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Direct3D11.Debug;
using Vortice.Direct3D11.Shader;
using GameImageUtil.ShaderCode;
using GameImageUtil.Assets;

namespace GameImageUtil.Shaders
{
    /// <summary>
    /// A class to hold a compute shader.
    /// </summary>
    public class ComputeShader : GraphicsObject
    {
        /// <summary>
        /// Gets the name of the compute shader.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the shader blob.
        /// </summary>
        public Blob ShaderBlob { get; private set; }

        /// <summary>
        /// Gets the underlying shader.
        /// </summary>
        public ID3D11ComputeShader Shader { get; private set; }

        /// <summary>
        /// Gets the number of X Threads.
        /// </summary>
        public int XThreads { get; private set; }

        /// <summary>
        /// Gets the number of Y Threads.
        /// </summary>
        public int YThreads { get; private set; }

        /// <summary>
        /// Gets the number of Z Threads.
        /// </summary>
        public int ZThreads { get; private set; }

        /// <summary>
        /// Gets the input textures.
        /// </summary>
        public ShaderResource[] Resources { get; private set; }

        /// <summary>
        /// Initializes a new instance of a <see cref="ComputeShader"/>.
        /// </summary>
        /// <param name="device"></param>
        /// <param name="name"></param>
        /// <param name="shaderBlob"></param>
        public ComputeShader(GraphicsDevice device, string name, Blob shaderBlob)
        {
            Owner          = device;
            Name           = name;
            ShaderBlob     = shaderBlob;
            Shader         = device.Device.CreateComputeShader(shaderBlob);
            Resources      = ShaderHelper.GenerateResources(shaderBlob, out int x, out int y, out int z);
            XThreads       = x;
            YThreads       = y;
            ZThreads       = z;
        }

        /// <summary>
        /// Binds the texture to the provided resource with the given name.
        /// </summary>
        /// <param name="resName">Name of the resource within the shader.</param>
        /// <param name="texture">Texture to bind.</param>
        public void Bind(string resName, Texture texture)
        {
            if (!TryGetResource(resName, out var res))
                throw new Exception();

            if(res.Type == ShaderResourceType.InputTexture)
            {
                Owner.Context.CSSetShaderResource(res.Index, texture.View as ID3D11ShaderResourceView);
            }
            else
            {
                Owner.Context.CSSetUnorderedAccessView(res.Index, texture.View as ID3D11UnorderedAccessView);
            }
        }

        /// <summary>
        /// Binds the texture to the provided resource with the given name.
        /// </summary>
        /// <param name="texture">Textures to bind.</param>
        public void Bind(params Texture[] textures)
        {
            foreach (var texture in textures)
            {
                Bind(texture.Name, texture);
            }
        }

        public bool TryGetResource(string resName, out ShaderResource res)
        {
            foreach (var r in Resources)
            {
                if(r.Name.Equals(resName, StringComparison.CurrentCultureIgnoreCase))
                {
                    res = r;
                    return true;
                }
            }

            res = new();
            return false;
        }

        /// <summary>
        /// Dispatches the compute shader.
        /// </summary>
        /// <param name="x">Number of X Threads.</param>
        /// <param name="y">Number of Y Threads.</param>
        /// <param name="z">Number of Z Threads.</param>
        public void Dispatch(int x, int y, int z)
        {
            Owner.Context.CSSetShader(Shader);
            Owner.Context.Dispatch(
                x / XThreads,
                y / XThreads,
                z / XThreads);
            Owner.Context.CSSetShader(Shader);
            Owner.Context.Flush();
        }

        /// <summary>
        /// Dispatches the compute shader.
        /// </summary>
        /// <param name="x">Number of X Threads.</param>
        /// <param name="y">Number of Y Threads.</param>
        /// <param name="z">Number of Z Threads.</param>
        /// <param name="textures"></param>
        public void Dispatch(int x, int y, int z, params Texture[] textures)
        {
            Bind(textures);

            Owner.Context.CSSetShader(Shader);
            Owner.Context.Dispatch(
                x / XThreads,
                y / XThreads,
                z / XThreads);
        }

        /// <summary>
        /// Dispatches the compute shader.
        /// </summary>
        /// <param name="x">Number of X Threads.</param>
        /// <param name="y">Number of Y Threads.</param>
        /// <param name="z">Number of Z Threads.</param>
        /// <param name="buffer">Number of Z Threads.</param>
        /// <param name="textures"></param>
        public void Dispatch(int x, int y, int z, ShaderConstantBuffer buffer, params Texture[] textures)
        {
            Bind(textures);

            if (TryGetResource(buffer.Name, out var res))
            {
                buffer.Update();
                Owner.Context.CSSetConstantBuffer(res.Index, buffer.GraphicsBuffer);
            }

            Owner.Context.CSSetShader(Shader);
            Owner.Context.Dispatch(
                x / XThreads,
                y / XThreads,
                z / XThreads);
        }

        /// <inheritdoc/>
        public override void Release()
        {
            Shader.Dispose();
            ShaderBlob.Dispose();
        }
    }
}
