using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.Direct3D11.Shader;

namespace GameImageUtil.ShaderCode
{
    public class ShaderConstantBuffer : GraphicsObject
    {
        /// <summary>
        /// Gets or Sets the name of the constant buffer.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the underlying buffer.
        /// </summary>
        public ID3D11Buffer? GraphicsBuffer { get; set; }

        /// <summary>
        /// Gets or Sets the raw data for this buffer.
        /// </summary>
        private byte[]? Data { get; set; }

        private ShaderVariableDescription[]? Variables { get; set; }

        public ShaderConstantBuffer(GraphicsDevice device, string name, Blob shaderBlob)
        {
            Name = name;

            using var reflection = Compiler.Reflect<ID3D11ShaderReflection>(shaderBlob.AsSpan());

            var constantBufferCount = reflection.Description.ConstantBuffers;

            for (int c = 0; c < constantBufferCount; c++)
            {
                using var constantBuffer = reflection.GetConstantBufferByIndex(c);
                var refDesc = constantBuffer.Description;

                if (!refDesc.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                    continue;

                var desc = new BufferDescription()
                {
                    BindFlags = BindFlags.ConstantBuffer,
                    SizeInBytes = refDesc.Size,
                    Usage = ResourceUsage.Default
                };

                Variables = new ShaderVariableDescription[refDesc.VariableCount];
                Owner = device;
                Name = name;
                GraphicsBuffer = device.Device.CreateBuffer(desc);
                Data = new byte[refDesc.Size];

                for (int i = 0; i < refDesc.VariableCount; i++)
                {
                    using var variable = constantBuffer.GetVariableByIndex(i);
                    Variables[i] = variable.Description;
                }

                break;
            }
        }


        public ShaderConstantBuffer Update()
        {
            if(GraphicsBuffer != null && Data != null)
                Owner.Context.UpdateSubresource(Data, GraphicsBuffer);

            return this;
        }

        /// <summary>
        /// Assigns the variable with the given name.
        /// </summary>
        /// <typeparam name="T">Variable Type.</typeparam>
        /// <param name="name">Name of the variable.</param>
        /// <param name="data">Data to assign.</param>
        /// <returns></returns>
        public bool Set<T>(string name, T data) where T : unmanaged
        {
            if (Variables != null && Data != null)
            {
                var variable = Variables!.FirstOrDefault(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));

                if(!string.IsNullOrWhiteSpace(variable.Name))
                {
                    MemoryMarshal.Cast<T, byte>(stackalloc T[1]
                    {
                        data
                    }).CopyTo(Data.AsSpan()[variable.StartOffset..]);
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public override void Release()
        {
            GraphicsBuffer?.Release();
        }
    }
}
