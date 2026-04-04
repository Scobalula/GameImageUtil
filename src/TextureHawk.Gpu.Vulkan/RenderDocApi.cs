using System.Runtime.InteropServices;

namespace TextureHawk.Gpu.Vulkan;

/// <summary>
/// Marshalled RenderDoc API 1.6.0 function pointer table, populated by RENDERDOC_GetAPI.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
#pragma warning disable CS0649
internal unsafe struct RenderDocApi
{
    public delegate* unmanaged[Cdecl]<int*, int*, int*, void> GetAPIVersion;
    public delegate* unmanaged[Cdecl]<int, uint, int> SetCaptureOptionU32;
    public delegate* unmanaged[Cdecl]<int, float, int> SetCaptureOptionF32;
    public delegate* unmanaged[Cdecl]<int, uint> GetCaptureOptionU32;
    public delegate* unmanaged[Cdecl]<int, float> GetCaptureOptionF32;
    public delegate* unmanaged[Cdecl]<IntPtr, int, void> SetFocusToggleKeys;
    public delegate* unmanaged[Cdecl]<IntPtr, int, void> SetCaptureKeys;
    public delegate* unmanaged[Cdecl]<uint> GetOverlayBits;
    public delegate* unmanaged[Cdecl]<uint, uint, void> MaskOverlayBits;
    public IntPtr RemoveHooks;
    public IntPtr UnloadCrashHandler;
    public delegate* unmanaged[Cdecl]<byte*, void> SetCaptureFilePathTemplate;
    public delegate* unmanaged[Cdecl]<byte*> GetCaptureFilePathTemplate;
    public delegate* unmanaged[Cdecl]<uint> GetNumCaptures;
    public IntPtr GetCapture;
    public delegate* unmanaged[Cdecl]<void> TriggerCapture;
    public delegate* unmanaged[Cdecl]<uint> IsTargetControlConnected;
    public IntPtr LaunchReplayUI;
    public delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> SetActiveWindow;
    public delegate* unmanaged[Cdecl]<IntPtr, IntPtr, void> StartFrameCapture;
    public delegate* unmanaged[Cdecl]<uint> IsFrameCapturing;
    public delegate* unmanaged[Cdecl]<IntPtr, IntPtr, uint> EndFrameCapture;
    public IntPtr TriggerMultiFrameCapture;
    public IntPtr SetCaptureFileComments;
    public IntPtr DiscardFrameCapture;
    public IntPtr ShowReplayUI;
    public delegate* unmanaged[Cdecl]<byte*, void> SetCaptureTitle;
}
#pragma warning restore CS0649
