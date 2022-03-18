using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameImageUtil.ShaderCode
{
    /// <summary>
    /// An enum that defines a shader resource type.
    /// </summary>
    public enum ShaderResourceType
    {
        InputTexture,

        OutputTexture,

        Sampler,

        ConstantBuffer,

        Unknown = int.MaxValue
    }
}
