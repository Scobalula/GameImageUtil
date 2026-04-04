namespace TextureHawk.CLI;

internal sealed class ImageInfoResult
{
    public required string FilePath { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    public required int Depth { get; init; }

    public required int ArraySize { get; init; }

    public required int MipLevels { get; init; }

    public required string Format { get; init; }

    public required bool IsCubemap { get; init; }

    public required int SliceCount { get; init; }
}