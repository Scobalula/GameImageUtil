using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace GameImageUtil
{
    /// <summary>
    /// Provides helper classes for the graphics device.
    /// </summary>
    internal class GraphicsDeviceHelper
    {
        /// <summary>
        /// Attempts to create a hardware adapter, if none is found, false is returned.
        /// </summary>
        /// <param name="adapter">The adapter, if none is found, this will be null.</param>
        /// <returns>True if the adapter was created successfully, otherwise false.</returns>
        public static bool TryCreateHardwareAdapter([NotNullWhen(true)] out IDXGIAdapter1? adapter)
        {
            using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory6>();

            if (factory != null)
            {
                for (int i = 0;
                     factory.EnumAdapterByGpuPreference(i, GpuPreference.HighPerformance, out adapter).Success;
                     i++)
                {
                    if(adapter == null)
                    {
                        continue;
                    }

                    if(adapter.Description1.Flags.HasFlag(AdapterFlags.Software))
                    {
                        adapter.Dispose();
                        continue;
                    }

                    return true;
                }
            }

            adapter = null;
            return false;
        }

        /// <summary>
        /// Attempts to create the underlying device and context.
        /// </summary>
        /// <param name="adapter">The adapter to use, if null, a default one will be used.</param>
        /// <param name="device">The device, null if unable to create.</param>
        /// <param name="context">The device context, null if unable to create.</param>
        /// <returns>True if the device was created successfully, otherwise false.</returns>
        public static bool TryCreateDevice(IDXGIAdapter1? adapter,
                                           [NotNullWhen(true)] out ID3D11Device1? device,
                                           [NotNullWhen(true)] out ID3D11DeviceContext1? context)
        {
            var featureLevels = new[]
            {
                FeatureLevel.Level_11_1,
            };
            var result = D3D11.D3D11CreateDevice(
                adapter,
                adapter != null ? DriverType.Unknown : DriverType.Hardware,
                DeviceCreationFlags.Debug,
                featureLevels,
                out var tempDevice,
                out var tempContext);

            if(result.Failure)
            {
                D3D11.D3D11CreateDevice(
                    null,
                    DriverType.Warp,
                    DeviceCreationFlags.Debug,
                    featureLevels,
                    out tempDevice,
                    out tempContext);
            }

            if(result.Success)
            {
                device = tempDevice.QueryInterface<ID3D11Device1>();
                context = tempContext.QueryInterface<ID3D11DeviceContext1>();
                device.Dispose();
                context.Dispose();
                return true;
            }

            context = null;
            device = null;
            return false;
        }
    }
}
