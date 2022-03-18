using Vortice.Direct3D11;
using Vortice.DXGI;

namespace GameImageUtil
{
    /// <summary>
    /// A class to hold a graphics device.
    /// </summary>
    public class GraphicsDevice : IDisposable
    {
        /// <summary>
        /// Gets the current device.
        /// </summary>
        public ID3D11Device1 Device { get; private set; }

        /// <summary>
        /// Gets the current device context.
        /// </summary>
        public ID3D11DeviceContext1 Context { get; private set; }

        /// <summary>
        /// Gets the current adapter.
        /// </summary>
        public IDXGIAdapter1 Adapter { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsDevice"/> class and creates the required handles.
        /// </summary>
        /// <exception cref="NotSupportedException">Thrown if the device fails to initialize.</exception>
        public GraphicsDevice()
        {
            if (!GraphicsDeviceHelper.TryCreateHardwareAdapter(out var adapter))
                throw new NotSupportedException("Failed to create hardware adapter.");
            if (!GraphicsDeviceHelper.TryCreateDevice(adapter, out var device, out var context))
                throw new NotSupportedException("Failed to create device and context.");

            Device = device;
            Context = context;
            Adapter = adapter;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Device.Dispose();
            Context.Dispose();
            Adapter.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}