namespace TextureHawk.Processing;

/// <summary>
/// Optional processor capability for hidden path-based auto-detection.
/// </summary>
/// <remarks>
/// Implement this only for processors that can reliably identify applicable inputs from the file path.
/// The UI uses it behind a keybind-only workflow rather than exposing it as a normal mode selector.
/// </remarks>
public interface IAutoDetectProcessor : IProcessor
{
    /// <summary>
    /// Returns whether this processor should be selected for the supplied input path.
    /// </summary>
    bool CanConvert(string inputPath);
}