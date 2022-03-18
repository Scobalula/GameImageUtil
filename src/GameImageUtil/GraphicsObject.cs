using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameImageUtil
{
    /// <summary>
    /// A interface to define a graphics object that holds resources stored on the GPU.
    /// </summary>
    public abstract class GraphicsObject : IDisposable
    {
        /// <summary>
        /// Gets the <see cref="GraphicsDevice"/> that owns this.
        /// </summary>
        public GraphicsDevice Owner { get; set; }

        /// <summary>
        /// Releases resources owned by this graphics object.
        /// </summary>
        public abstract void Release();

        /// <inheritdoc/>
        public virtual void Dispose()
        {
            Release();
            GC.SuppressFinalize(this);
        }
    }
}
