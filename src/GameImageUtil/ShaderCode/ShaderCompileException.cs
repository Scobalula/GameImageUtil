using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameImageUtil.Shaders
{
    public class ShaderCompileException : Exception
    {
        public ShaderCompileException()
        {
        }

        public ShaderCompileException(string message) : base(message)
        {
        }

        public ShaderCompileException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
