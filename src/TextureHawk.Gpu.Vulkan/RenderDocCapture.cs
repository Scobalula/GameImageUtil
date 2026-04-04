using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace TextureHawk.Gpu.Vulkan;

internal sealed class RenderDocCapture
{
    private const int RenderDocApiVersion_1_6_0 = 10600;
    private const string RenderDocModuleName = "renderdoc.dll";

    private readonly RenderDocApi _api;

    private RenderDocCapture(RenderDocApi api)
    {
        _api = api;
    }

    public static unsafe RenderDocCapture? TryCreate()
    {
        if (!OperatingSystem.IsWindows())
            return null;

        if (!NativeLibrary.TryLoad(RenderDocModuleName, out var module))
            return null;
        if (!NativeLibrary.TryGetExport(module, "RENDERDOC_GetAPI", out var getApiExport))
            return null;

        var getApi = Marshal.GetDelegateForFunctionPointer<RenderDocGetApiDelegate>(getApiExport);
        if (getApi(RenderDocApiVersion_1_6_0, out var apiPointer) == 0 || apiPointer == IntPtr.Zero)
            return null;

        var api = Unsafe.Read<RenderDocApi>((void*)apiPointer);
        return api.StartFrameCapture == null || api.EndFrameCapture == null || api.IsFrameCapturing == null
            ? null
            : new RenderDocCapture(api);
    }

    public unsafe bool TryStart(string captureName)
    {
        if (_api.IsFrameCapturing() != 0)
            return false;

        _api.StartFrameCapture(IntPtr.Zero, IntPtr.Zero);
        if (_api.IsFrameCapturing() == 0)
            return false;

        if (_api.SetCaptureTitle != null && !string.IsNullOrWhiteSpace(captureName))
        {
            var utf8 = Encoding.UTF8.GetBytes(captureName + '\0');
            fixed (byte* captureNamePtr = utf8)
                _api.SetCaptureTitle(captureNamePtr);
        }

        return true;
    }

    public unsafe void End()
    {
        if (_api.IsFrameCapturing() == 0)
            return;

        _api.EndFrameCapture(IntPtr.Zero, IntPtr.Zero);
    }
}