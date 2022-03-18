using GameImageUtil.ShaderCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameImageUtil
{
    /// <summary>
    /// A class to hold an instance of Game Image Util.
    /// </summary>
    public class Instance
    {
        /// <summary>
        /// Gets the graphics device.
        /// </summary>
        public GraphicsDevice Device { get; private set; }

        /// <summary>
        /// Gets the shader include handler.
        /// </summary>
        public ShaderInclude? IncludeHandler { get; private set; }

        public Instance()
        {
            Device = new GraphicsDevice();
        }
    }
}
