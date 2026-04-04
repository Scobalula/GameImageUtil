using System.Runtime.InteropServices;

namespace TextureHawk.Gpu.Vulkan;

/// <summary>
/// RenderDoc API entry point function signature for retrieving the function pointer table.
/// </summary>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
internal delegate int RenderDocGetApiDelegate(int version, out IntPtr apiPointer);
