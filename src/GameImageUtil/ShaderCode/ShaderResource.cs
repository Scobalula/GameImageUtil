using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameImageUtil.Shaders
{
    /// <summary>
    /// A struct to hold a shader resource.
    /// </summary>
    public struct ShaderResource
    {
        /// <summary>
        /// Gets the resource name.
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// Gets the resource type.
        /// </summary>
        public ShaderResourceType Type { get; internal set; }

        /// <summary>
        /// Gets the index of the resource within the shader.
        /// </summary>
        public int Index { get; internal set; }

        /// <summary>
        /// Initializes a new <see cref="ShaderResource"/>.
        /// </summary>
        /// <param name="name">Name of the resource.</param>
        /// <param name="type">Resource type.</param>
        /// <param name="index">Index of the resource within the shader.</param>
        public ShaderResource(string name, ShaderResourceType type, int index)
        {
            Name = name;
            Type = type;
            Index = index;
        }
    }
}
