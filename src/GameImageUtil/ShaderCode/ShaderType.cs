using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameImageUtil.ShaderCode
{
    public enum ShaderType
    {
        /// <summary>
        /// Invalid shader type.
        /// </summary>
        Invalid,

        /// <summary>
        /// Shader is for the vertex stage.
        /// </summary>
        VertexShader,

        /// <summary>
        /// Shader is for the hull stage for tessellation.
        /// </summary>
        HullShader,

        /// <summary>
        /// Shader is for the domain stage for tessellation.
        /// </summary>
        DomainShader,

        /// <summary>
        /// Shader is for the geometry stage.
        /// </summary>
        GeometryShader,

        /// <summary>
        /// Shader is for the pixel shader stage.
        /// </summary>
        PixelShader,

        /// <summary>
        /// Shader is for the computation.
        /// </summary>
        ComputShader,
    }
}
