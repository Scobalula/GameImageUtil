using RedFox.Graphics2D.Bmp;
using RedFox.Graphics2D.Exr;
using RedFox.Graphics2D.IO;
using RedFox.Graphics2D.Png;
using RedFox.Graphics2D.Tiff;
using TextureHawk.BuiltInProcessors.Basics;
using TextureHawk.BuiltInProcessors.ColorTextures;
using TextureHawk.BuiltInProcessors.EnvironmentTextures;
using TextureHawk.BuiltInProcessors.NormalMapping;
using TextureHawk.BuiltInProcessors.PackedGameTextures;
using TextureHawk.BuiltInProcessors.PackedGameTextures.CallOfDuty;
using TextureHawk.BuiltInProcessors.PackedGameTextures.IWEngine;
using TextureHawk.Processing;

namespace TextureHawk.BuiltInProcessors;

/// <summary>
/// Registers all built-in translators and processors.
/// </summary>
/// <remarks>
/// Call <see cref="RegisterAll"/> during application startup to make all
/// built-in image format translators and processors available.
/// </remarks>
public static class BuiltInRegistrar
{
    /// <summary>
    /// Registers all built-in translators and processors.
    /// </summary>
    public static void RegisterAll(ProcessorRegistry processorRegistry, ImageTranslatorManager translatorManager)
    {
        RegisterTranslators(translatorManager);
        RegisterProcessors(processorRegistry);
    }

    /// <summary>
    /// Registers all built-in image format translators.
    /// </summary>
    public static void RegisterTranslators(ImageTranslatorManager manager)
    {
        manager.Register(new PngImageTranslator());
        manager.Register(new DdsImageTranslator());
        manager.Register(new BmpImageTranslator());
        manager.Register(new TiffImageTranslator());
        manager.Register(new ExrImageTranslator());
    }

    /// <summary>
    /// Registers all built-in processors.
    /// </summary>
    public static void RegisterProcessors(ProcessorRegistry registry)
    {
        registry.Register(new FileConvertProcessor());
        registry.Register(new ChannelMergeProcessor());
        registry.Register(new ChannelSwizzleProcessor());
        registry.Register(new SplitRgbaProcessor());
        registry.Register(new SplitColorAlphaProcessor());
        registry.Register(new IWSpecGlossSplitProcessor());
        registry.Register(new IWSgoBlueProcessor());
        registry.Register(new IWSgoGreenProcessor());
        registry.Register(new NormalMapFlipYProcessor());
        registry.Register(new NormalMapReconstructZProcessor());
        // Color Textures
        registry.Register(new MetalnessToSpecularProcessor());
        registry.Register(new SpecularToMetalnessProcessor());


        registry.Register(new IWNogProcessor());
        registry.Register(new ResizeProcessor());
        registry.Register(new CubemapToEquirectProcessor());
        registry.Register(new EquirectToCubemapProcessor());
        registry.Register(new HemiOctaToEquirectProcessor());
        registry.Register(new EquirectToHemiOctaProcessor());
        registry.Register(new CubemapToHemiOctaProcessor());
        registry.Register(new HemiOctaToCubemapProcessor());
        registry.Register(new GrayscaleProcessor());
        registry.Register(new InvertProcessor());
        registry.Register(new BrightnessContrastProcessor());
    }
}
