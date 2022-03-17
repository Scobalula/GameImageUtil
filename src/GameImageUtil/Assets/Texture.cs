using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;
using Vortice.DXGI;
using GameImageUtil.Imaging.DirectXTex;

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
        /// Gets the metadata.
        /// </summary>
        public ScratchImageMetadata Metadata { get; private set; }

        /// <summary>
        /// Gets or Sets the view over the resource.
        /// </summary>
        private ID3D11ShaderResourceView View { get; set; }

        /// <summary>
        /// Gets whether or not this texture is an output texture.
        /// </summary>
        public bool IsOutputTexture { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Texture"/> class as an input.
        /// </summary>
        /// <param name="owner">The device that owns this texture.</param>
        /// <param name="name">The name of the image.</param>
        /// <param name="filePath">The file to copy to the GPU.</param>
        public Texture(GraphicsDevice owner, string name, string filePath)
        {
            using var image = new ScratchImage(filePath);

            Owner           = owner;
            Name            = name;
            Metadata        = image.Metadata;
            View            = new ID3D11ShaderResourceView(image.CreateShaderResourceView(owner.Device.NativePointer));
            IsOutputTexture = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Texture"/> class as an input.
        /// </summary>
        /// <param name="owner">The device that owns this texture.</param>
        /// <param name="name">The name of the image.</param>
        /// <param name="image">The scratch image to copy to the GPU.</param>
        public Texture(GraphicsDevice owner, string name, ScratchImage image)
        {
            Owner           = owner;
            Name            = name;
            Metadata        = image.Metadata;
            View            = new ID3D11ShaderResourceView(image.CreateShaderResourceView(owner.Device.NativePointer));
            IsOutputTexture = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Texture"/> class as an output UAV.
        /// </summary>
        /// <param name="owner">The device that owns this texture.</param>
        /// <param name="name">The name of the image.</param>
        /// <param name="metadata">The metadata to initialize from.</param>
        public Texture(GraphicsDevice owner, string name, ScratchImageMetadata metadata)
        {
            Owner           = owner;
            Name            = name;
            Metadata        = metadata;
            View            = TextureHelper.CreateBlankUAV(owner, metadata);
            IsOutputTexture = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Texture"/> class as an output UAV.
        /// </summary>
        /// <param name="owner">The device that owns this texture.</param>
        /// <param name="name">The name of the image.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="cubeMap">Whether or not this is a cube map with multiple images.</param>
        public Texture(GraphicsDevice owner,
                       string name,
                       int width,
                       int height,
                       bool cubeMap) : this(owner,
                                            name,
                                            width,
                                            height,
                                            ScratchImageFormat.R32G32B32A32Float,
                                            cubeMap)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Texture"/> class as an output UAV.
        /// </summary>
        /// <param name="owner">The device that owns this texture.</param>
        /// <param name="name">The name of the image.</param>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="format">The format of the image.</param>
        /// <param name="cubeMap">Whether or not this is a cube map with multiple images.</param>
        public Texture(GraphicsDevice owner,
                       string name,
                       int width,
                       int height,
                       ScratchImageFormat format,
                       bool cubeMap)
        {
            Owner = owner;
            Name = name;
            Metadata = new()
            {
                Width = (ulong)width,
                Height = (ulong)height,
                Depth = 1,
                ArraySize = (ulong)(cubeMap ? 6 : 1),
                MipLevels = 1,
                MiscFlags = cubeMap ? ScratchImageFlags.TextureCube : ScratchImageFlags.None,
                MiscFlags2 = ScratchImageFlags2.NONE,
                Dimension = ScratchImageDimension.Texture2D,
                Format = format
            };
            View = TextureHelper.CreateBlankUAV(owner, Metadata);
            IsOutputTexture = true;
        }

        /// <summary>
        /// Creates a <see cref="ScratchImage"/> from this image, copying from the GPU.
        /// </summary>
        /// <returns>Resulting scratch image.</returns>
        public ScratchImage CreateScratchImage()
        {
            return new ScratchImage(
                Owner.Device.NativePointer,
                Owner.Context.NativePointer,
                View.NativePointer);
        }

        /// <summary>
        /// Saves the image to the provided file path.
        /// </summary>
        /// <param name="filePath">Path to save the image to.</param>
        public void Save(string filePath)
        {
            using var image = CreateScratchImage();
            image.Save(filePath);
        }

        /// <inheritdoc/>
        public override void Release()
        {
            View.Dispose();
        }
    }
}
