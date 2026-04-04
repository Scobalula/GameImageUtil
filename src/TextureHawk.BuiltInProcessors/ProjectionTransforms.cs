using RedFox.Graphics2D;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors;

internal static class ProjectionTransforms
{
    internal static Image PrepareCubemapSource(ProcessorContext context, Image source)
    {
        var layout = context.GetParameterValue("inputLayout", "Native");

        if (string.Equals(layout, "Native", StringComparison.OrdinalIgnoreCase))
            return source;

        if (source.IsCubemap && source.Slices.Length >= 6)
            return source;

        return layout switch
        {
            "HCross" => ConvertHCrossToCubemap(source),
            "VCross" => ConvertVCrossToCubemap(source),
            "HStrip" => ConvertHStripToCubemap(source),
            "VStrip" => ConvertVStripToCubemap(source),
            _ => throw new InvalidOperationException($"Unsupported cubemap input layout: {layout}")
        };
    }

    internal static (Image Source, Image? LowerHalf, bool HasLowerHalf, bool UpperOnly) ResolveHemiOctaSource(ProcessorContext context, Image source)
    {
        var sourceLayout = context.GetParameterValue("sourceLayout", "Packed");
        var hasLowerHalf = context.GetParameterValue("hasLowerHalf", true);
        var lowerHalfPath = context.GetParameterValue("lowerHalfPath", string.Empty);
        var isPacked = string.Equals(sourceLayout, "Packed", StringComparison.OrdinalIgnoreCase);

        // Packed source with both halves already present — pass through directly.
        if (isPacked && hasLowerHalf && string.IsNullOrWhiteSpace(lowerHalfPath))
            return (source, null, true, false);

        // Packed source but lower half is disabled — shader handles blackout via push constant.
        if (isPacked && !hasLowerHalf && string.IsNullOrWhiteSpace(lowerHalfPath))
            return (source, null, false, false);

        // Packed source but a separate lower half file overrides the right half.
        // Pass upper and lower as separate images — the shader samples them independently.
        if (isPacked && !string.IsNullOrWhiteSpace(lowerHalfPath))
        {
            if ((source.Width & 1) != 0)
                throw new InvalidOperationException("Packed hemi-octa input width must be even.");

            var halfWidth = source.Width / 2;
            var upper = ExtractHalf(source, 0, halfWidth);
            var lower = context.TranslatorManager.Read(lowerHalfPath);
            if (lower.Width != halfWidth || lower.Height != source.Height)
                throw new InvalidOperationException("Lower half image dimensions must match the upper half dimensions.");

            return (upper, lower, true, false);
        }

        // Upper Only with no lower half — pass through, shader treats full image as upper hemisphere.
        if (!hasLowerHalf && string.IsNullOrWhiteSpace(lowerHalfPath))
            return (source, null, false, true);

        // Upper Only with separate lower half file — pass as separate images.
        if (!string.IsNullOrWhiteSpace(lowerHalfPath))
        {
            var lower = context.TranslatorManager.Read(lowerHalfPath);
            if (lower.Width != source.Width || lower.Height != source.Height)
                throw new InvalidOperationException("Lower half image dimensions must match the upper half dimensions.");

            return (source, lower, true, false);
        }

        // Upper Only, hasLowerHalf=true but no file — treat as upper-only since there's no lower data.
        return (source, null, false, true);
    }

    internal static Image CombineFacesIntoCubemap(IReadOnlyList<Image> faces, ImageFormat format, int faceSize)
    {
        if (faces.Count != 6)
            throw new InvalidOperationException("Cubemap conversion must produce exactly six face images.");

        var cubemap = new Image(faceSize, faceSize, 1, 6, 1, format, true);
        if (cubemap.Slices.Length < 6)
            throw new InvalidOperationException("Cubemap destination must allocate 6 face slices.");
        for (var i = 0; i < 6; i++)
            faces[i].Slices[0].Pixels.Span.CopyTo(cubemap.Slices[i].Pixels.Span);

        return cubemap;
    }

    internal static ProcessingResult AssembleCubemapOutput(IReadOnlyList<Image> faces, string layout, ImageFormat format, int faceSize)
    {
        if (faces.Count != 6)
            throw new InvalidOperationException("Cubemap conversion must produce exactly six face images.");

        return layout switch
        {
            "Cubemap" => ProcessingResult.Ok(CombineFacesIntoCubemap(faces, format, faceSize)),
            "HCross" => ProcessingResult.Ok(AssembleHCross(faces, format, faceSize)),
            "VCross" => ProcessingResult.Ok(AssembleVCross(faces, format, faceSize)),
            "HStrip" => ProcessingResult.Ok(AssembleHStrip(faces, format, faceSize)),
            "VStrip" => ProcessingResult.Ok(AssembleVStrip(faces, format, faceSize)),
            "Individual" => ProcessingResult.Ok(
                (IReadOnlyList<Image>)faces,
                (IReadOnlyList<string>)["_PosX", "_NegX", "_PosY", "_NegY", "_PosZ", "_NegZ"]),
            _ => throw new InvalidOperationException($"Unsupported cubemap output layout: {layout}")
        };
    }

    // Face order: 0=+X, 1=-X, 2=+Y, 3=-Y, 4=+Z, 5=-Z
    // HCross (4×3):        [+Y]
    //                [-X]  [+Z]  [+X]  [-Z]
    //                      [-Y]
    private static Image ConvertHCrossToCubemap(Image source)
    {
        if ((source.Width % 4) != 0 || (source.Height % 3) != 0)
            throw new InvalidOperationException("Horizontal cross layout must use a 4×3 grid.");

        var faceSize = source.Width / 4;
        if ((source.Height / 3) != faceSize)
            throw new InvalidOperationException("Horizontal cross layout must have square faces (width/4 == height/3).");

        var cubemap = new Image(faceSize, faceSize, 1, 6, 1, source.Format, true);
        CopyFaceFromGrid(source, cubemap, 0, faceSize, gridX: 2, gridY: 1);
        CopyFaceFromGrid(source, cubemap, 1, faceSize, gridX: 0, gridY: 1);
        CopyFaceFromGrid(source, cubemap, 2, faceSize, gridX: 1, gridY: 0);
        CopyFaceFromGrid(source, cubemap, 3, faceSize, gridX: 1, gridY: 2);
        CopyFaceFromGrid(source, cubemap, 4, faceSize, gridX: 1, gridY: 1);
        CopyFaceFromGrid(source, cubemap, 5, faceSize, gridX: 3, gridY: 1);
        return cubemap;
    }

    // VCross (3×4):        [+Y]
    //                [-X]  [+Z]  [+X]
    //                      [-Y]
    //                      [-Z]
    private static Image ConvertVCrossToCubemap(Image source)
    {
        if ((source.Width % 3) != 0 || (source.Height % 4) != 0)
            throw new InvalidOperationException("Vertical cross layout must use a 3×4 grid.");

        var faceSize = source.Width / 3;
        if ((source.Height / 4) != faceSize)
            throw new InvalidOperationException("Vertical cross layout must have square faces (width/3 == height/4).");

        var cubemap = new Image(faceSize, faceSize, 1, 6, 1, source.Format, true);
        CopyFaceFromGrid(source, cubemap, 0, faceSize, gridX: 2, gridY: 1);
        CopyFaceFromGrid(source, cubemap, 1, faceSize, gridX: 0, gridY: 1);
        CopyFaceFromGrid(source, cubemap, 2, faceSize, gridX: 1, gridY: 0);
        CopyFaceFromGrid(source, cubemap, 3, faceSize, gridX: 1, gridY: 2);
        CopyFaceFromGrid(source, cubemap, 4, faceSize, gridX: 1, gridY: 1);
        CopyFaceFromGrid(source, cubemap, 5, faceSize, gridX: 1, gridY: 3);
        return cubemap;
    }

    // HStrip (6×1): [+X] [-X] [+Y] [-Y] [+Z] [-Z] left to right
    private static Image ConvertHStripToCubemap(Image source)
    {
        if ((source.Width % 6) != 0)
            throw new InvalidOperationException("Horizontal strip layout width must be divisible by 6.");

        var faceSize = source.Width / 6;
        if (source.Height != faceSize)
            throw new InvalidOperationException("Horizontal strip layout must have square faces (width/6 == height).");

        var cubemap = new Image(faceSize, faceSize, 1, 6, 1, source.Format, true);
        for (var i = 0; i < 6; i++)
            CopyFaceFromGrid(source, cubemap, i, faceSize, gridX: i, gridY: 0);
        return cubemap;
    }

    // VStrip (1×6): [+X] [-X] [+Y] [-Y] [+Z] [-Z] top to bottom
    private static Image ConvertVStripToCubemap(Image source)
    {
        if ((source.Height % 6) != 0)
            throw new InvalidOperationException("Vertical strip layout height must be divisible by 6.");

        var faceSize = source.Height / 6;
        if (source.Width != faceSize)
            throw new InvalidOperationException("Vertical strip layout must have square faces (height/6 == width).");

        var cubemap = new Image(faceSize, faceSize, 1, 6, 1, source.Format, true);
        for (var i = 0; i < 6; i++)
            CopyFaceFromGrid(source, cubemap, i, faceSize, gridX: 0, gridY: i);
        return cubemap;
    }

    private static Image AssembleHCross(IReadOnlyList<Image> faces, ImageFormat format, int faceSize)
    {
        var output = new Image(faceSize * 4, faceSize * 3, 1, 1, 1, format, false);
        CopyFaceToGrid(faces[0], output, faceSize, gridX: 2, gridY: 1);
        CopyFaceToGrid(faces[1], output, faceSize, gridX: 0, gridY: 1);
        CopyFaceToGrid(faces[2], output, faceSize, gridX: 1, gridY: 0);
        CopyFaceToGrid(faces[3], output, faceSize, gridX: 1, gridY: 2);
        CopyFaceToGrid(faces[4], output, faceSize, gridX: 1, gridY: 1);
        CopyFaceToGrid(faces[5], output, faceSize, gridX: 3, gridY: 1);
        return output;
    }

    private static Image AssembleVCross(IReadOnlyList<Image> faces, ImageFormat format, int faceSize)
    {
        var output = new Image(faceSize * 3, faceSize * 4, 1, 1, 1, format, false);
        CopyFaceToGrid(faces[0], output, faceSize, gridX: 2, gridY: 1);
        CopyFaceToGrid(faces[1], output, faceSize, gridX: 0, gridY: 1);
        CopyFaceToGrid(faces[2], output, faceSize, gridX: 1, gridY: 0);
        CopyFaceToGrid(faces[3], output, faceSize, gridX: 1, gridY: 2);
        CopyFaceToGrid(faces[4], output, faceSize, gridX: 1, gridY: 1);
        CopyFaceToGrid(faces[5], output, faceSize, gridX: 1, gridY: 3);
        return output;
    }

    private static Image AssembleHStrip(IReadOnlyList<Image> faces, ImageFormat format, int faceSize)
    {
        var output = new Image(faceSize * 6, faceSize, 1, 1, 1, format, false);
        for (var i = 0; i < 6; i++)
            CopyFaceToGrid(faces[i], output, faceSize, gridX: i, gridY: 0);
        return output;
    }

    private static Image AssembleVStrip(IReadOnlyList<Image> faces, ImageFormat format, int faceSize)
    {
        var output = new Image(faceSize, faceSize * 6, 1, 1, 1, format, false);
        for (var i = 0; i < 6; i++)
            CopyFaceToGrid(faces[i], output, faceSize, gridX: 0, gridY: i);
        return output;
    }

    private static void CopyFaceFromGrid(Image source, Image destinationCubemap, int destinationSliceIndex, int faceSize, int gridX, int gridY)
    {
        var sourceSlice = source.Slices[0];
        var sourcePixels = sourceSlice.Pixels.Span;
        var destinationPixels = destinationCubemap.Slices[destinationSliceIndex].Pixels.Span;
        var sourceRowPitch = sourceSlice.RowPitch;
        var (faceRowPitch, _) = ImageFormatInfo.CalculatePitch(source.Format, faceSize, faceSize);
        var sourceX = gridX * faceSize;
        var sourceY = gridY * faceSize;
        var sourceXBytes = ImageFormatInfo.IsBlockCompressed(source.Format)
            ? (sourceX / 4) * ImageFormatInfo.GetBlockSize(source.Format)
            : sourceX * ImageFormatInfo.GetBitsPerPixel(source.Format) / 8;

        for (var y = 0; y < faceSize; y++)
        {
            var sourceOffset = ((sourceY + y) * sourceRowPitch) + sourceXBytes;
            sourcePixels.Slice(sourceOffset, faceRowPitch)
                .CopyTo(destinationPixels.Slice(y * faceRowPitch, faceRowPitch));
        }
    }

    private static void CopyFaceToGrid(Image face, Image destination, int faceSize, int gridX, int gridY)
    {
        var facePixels = face.Slices[0].Pixels.Span;
        var destSlice = destination.Slices[0];
        var destPixels = destSlice.Pixels.Span;
        var destRowPitch = destSlice.RowPitch;
        var (faceRowPitch, _) = ImageFormatInfo.CalculatePitch(face.Format, faceSize, faceSize);
        var destX = gridX * faceSize;
        var destY = gridY * faceSize;
        var destXBytes = ImageFormatInfo.IsBlockCompressed(face.Format)
            ? (destX / 4) * ImageFormatInfo.GetBlockSize(face.Format)
            : destX * ImageFormatInfo.GetBitsPerPixel(face.Format) / 8;

        for (var y = 0; y < faceSize; y++)
        {
            var destOffset = ((destY + y) * destRowPitch) + destXBytes;
            facePixels.Slice(y * faceRowPitch, faceRowPitch)
                .CopyTo(destPixels.Slice(destOffset, faceRowPitch));
        }
    }

    private static Image ExtractHalf(Image source, int xOffset, int width)
    {
        var half = new Image(width, source.Height, 1, 1, 1, source.Format, false);
        var sourceSlice = source.Slices[0];
        var sourcePixels = sourceSlice.Pixels.Span;
        var destinationPixels = half.Slices[0].Pixels.Span;
        var sourceRowPitch = sourceSlice.RowPitch;
        var (halfRowPitch, _) = ImageFormatInfo.CalculatePitch(source.Format, width, source.Height);
        var sourceOffsetBytes = ImageFormatInfo.IsBlockCompressed(source.Format)
            ? (xOffset / 4) * ImageFormatInfo.GetBlockSize(source.Format)
            : xOffset * ImageFormatInfo.GetBitsPerPixel(source.Format) / 8;

        for (var y = 0; y < source.Height; y++)
        {
            sourcePixels.Slice(y * sourceRowPitch + sourceOffsetBytes, halfRowPitch)
                .CopyTo(destinationPixels.Slice(y * halfRowPitch, halfRowPitch));
        }

        return half;
    }
}