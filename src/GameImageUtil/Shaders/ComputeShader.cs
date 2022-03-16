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

namespace GameImageUtil.Shaders
{
    /// <summary>
    /// A class to hold a compute shader.
    /// </summary>
    public class ComputeShader
    {
        /// <summary>
        /// Gets the <see cref="GraphicsDevice"/> that owns this.
        /// </summary>
        public GraphicsDevice Owner { get; private set; }

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
    }
}
