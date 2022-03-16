using GameImageUtil.Imaging.DirectXTex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;

namespace GameImageUtil.Assets
{
    /// <summary>
    /// A class to hold a texture.
    /// </summary>
    public class Texture : GraphicsObject
    {
        /// <summary>
        /// Gets the <see cref="GraphicsDevice"/> that owns this.
        /// </summary>
        public GraphicsDevice Owner { get; set; }

        /// <summary>
        /// Gets or Sets the name of the texture.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the width of the texture.
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// Gets the height of the texture.
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// Gets or Sets the view over the resource.
        /// </summary>
        private ID3D11ShaderResourceView View { get; set; }

        public Texture(GraphicsDevice owner, string filePath)
        {
            using var image = new ScratchImage(filePath);

            Owner = owner;
            View = new ID3D11ShaderResourceView(image.CreateShaderResourceView(owner.Device.NativePointer));
        }

        public override void Release()
        {
            throw new NotImplementedException();
        }
    }
}
