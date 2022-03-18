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
        public static ID3D11UnorderedAccessView CreateBlankUAV(GraphicsDevice device, ScratchImageMetadata metadata)
        {
            // TODO: Move texture related stuff to their own methods.
            // TODO: Some params for options.
            if (metadata.ArraySize == 0)
                throw new Exception("Invalid array size in metadata, cannot be 0.");
            if (metadata.MipLevels == 0)
                throw new Exception("Invalid mip levels in metadata, cannot be 0.");

            ID3D11Resource? temp = null;
            var viewDesc = new UnorderedAccessViewDescription()
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
                            viewDesc.ViewDimension = UnorderedAccessViewDimension.Texture1DArray;
                            viewDesc.Texture1DArray.ArraySize = (int)metadata.ArraySize;
                        }
                        else
                        {
                            viewDesc.ViewDimension = UnorderedAccessViewDimension.Texture1D;
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

                        if (metadata.ArraySize > 1)
                        {
                            viewDesc.ViewDimension = UnorderedAccessViewDimension.Texture2DArray;
                            viewDesc.Texture2DArray.ArraySize = (int)metadata.ArraySize;
                        }
                        else
                        {
                            viewDesc.ViewDimension = UnorderedAccessViewDimension.Texture2D;
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

                        viewDesc.ViewDimension = UnorderedAccessViewDimension.Texture3D;

                        temp = device.Device.CreateTexture3D(desc);
                        break;
                    }
            }

            using var resource = temp;

            if(temp == null)
                throw new Exception($"Failed to create texture.");

            return device.Device.CreateUnorderedAccessView(resource, viewDesc);
        }
    }
}
