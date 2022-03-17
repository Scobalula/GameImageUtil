using GameImageUtil.Imaging.DirectXTex;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D;
using Vortice.Direct3D11;

namespace GameImageUtil.Assets
{
    internal class TextureHelper
    {
        public static ID3D11ShaderResourceView CreateBlankUAV(GraphicsDevice device, ScratchImageMetadata metadata)
        {
            // TODO: Move texture related stuff to their own methods.
            // TODO: Some params for options.
            if (metadata.ArraySize == 0)
                throw new Exception("Invalid array size in metadata, cannot be 0.");
            if (metadata.MipLevels == 0)
                throw new Exception("Invalid mip levels in metadata, cannot be 0.");

            ID3D11Resource? temp = null;
            var viewDesc = new ShaderResourceViewDescription()
            {
                Format = (Vortice.DXGI.Format)metadata.Format
            };

            switch(metadata.Dimension)
            {
                case ScratchImageDimension.Texture1D:
                    {
                        var desc = new Texture1DDescription()
                        {
                            Width          = (int)metadata.Width,
                            MipLevels      = (int)metadata.MipLevels,
                            ArraySize      = (int)metadata.ArraySize,
                            Format         = (Vortice.DXGI.Format)metadata.Format,
                            Usage          = ResourceUsage.Dynamic,
                            BindFlags      = BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                            OptionFlags    = ResourceOptionFlags.None,
                            CpuAccessFlags = CpuAccessFlags.None,
                        };

                        if(metadata.ArraySize > 1)
                        {
                            viewDesc.ViewDimension = ShaderResourceViewDimension.Texture1DArray;
                            viewDesc.Texture1DArray.MipLevels = (int)metadata.MipLevels;
                            viewDesc.Texture1DArray.ArraySize = (int)metadata.ArraySize;
                        }
                        else
                        {
                            viewDesc.ViewDimension = ShaderResourceViewDimension.Texture1D;
                            viewDesc.Texture1D.MipLevels = (int)metadata.MipLevels;
                        }

                        temp = device.Device.CreateTexture1D(desc);
                        break;
                    }
                case ScratchImageDimension.Texture2D:
                    {
                        var desc = new Texture2DDescription()
                        {
                            Width             = (int)metadata.Width,
                            Height            = (int)metadata.Height,
                            MipLevels         = (int)metadata.MipLevels,
                            ArraySize         = (int)metadata.ArraySize,
                            Format            = (Vortice.DXGI.Format)metadata.Format,
                            Usage             = ResourceUsage.Default,
                            BindFlags         = BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                            OptionFlags       = ResourceOptionFlags.None,
                            CpuAccessFlags    = CpuAccessFlags.None,
                            SampleDescription = new Vortice.DXGI.SampleDescription(1, 0)
                        };

                        if (metadata.MiscFlags.HasFlag(ScratchImageFlags.TextureCube))
                        {
                            if(metadata.ArraySize > 6)
                            {
                                if (metadata.ArraySize % 6 != 0)
                                    throw new Exception($"Invalid array size for cube array: {metadata.ArraySize}");

                                viewDesc.ViewDimension = ShaderResourceViewDimension.TextureCubeArray;
                                viewDesc.TextureCubeArray.MipLevels = (int)metadata.MipLevels;
                                viewDesc.TextureCubeArray.NumCubes = (int)metadata.ArraySize / 6;
                            }
                            else
                            {
                                viewDesc.ViewDimension = ShaderResourceViewDimension.TextureCube;
                                viewDesc.TextureCube.MipLevels = (int)metadata.MipLevels;
                            }

                            desc.OptionFlags = ResourceOptionFlags.TextureCube;
                        }
                        else if (metadata.ArraySize > 1)
                        {
                            viewDesc.ViewDimension = ShaderResourceViewDimension.Texture2DArray;
                            viewDesc.Texture2DArray.MipLevels = (int)metadata.MipLevels;
                            viewDesc.Texture2DArray.ArraySize = (int)metadata.ArraySize;
                        }
                        else
                        {
                            viewDesc.ViewDimension = ShaderResourceViewDimension.Texture2D;
                            viewDesc.Texture2D.MipLevels = (int)metadata.MipLevels;
                        }

                        temp = device.Device.CreateTexture2D(desc);
                        break;
                    }
                case ScratchImageDimension.Texture3D:
                    {
                        var desc = new Texture3DDescription()
                        {
                            Width             = (int)metadata.Width,
                            Height            = (int)metadata.Height,
                            Depth             = (int)metadata.Depth,
                            MipLevels         = (int)metadata.MipLevels,
                            Format            = (Vortice.DXGI.Format)metadata.Format,
                            Usage             = ResourceUsage.Dynamic,
                            BindFlags         = BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                            OptionFlags       = ResourceOptionFlags.None,
                            CpuAccessFlags    = CpuAccessFlags.None,
                        };

                        if(metadata.ArraySize != 1)
                            throw new Exception($"Invalid array size for 3D Texture: {metadata.ArraySize}");

                        viewDesc.ViewDimension = ShaderResourceViewDimension.Texture3D;
                        viewDesc.Texture3D.MipLevels = (int)metadata.MipLevels;

                        temp = device.Device.CreateTexture3D(desc);
                        break;
                    }
            }

            using var resource = temp;

            if(temp == null)
                throw new Exception($"Failed to create texture.");

            return device.Device.CreateShaderResourceView(resource, viewDesc);
        }
    }
}
