using System.Text.Json;
using TextureHawk.Settings;
using Xunit;

namespace TextureHawk.Tests;

public sealed class SettingsManagerTests : IDisposable
{
    private readonly string _rootDirectory;
    private readonly string _settingsPath;

    public SettingsManagerTests()
    {
        _rootDirectory = Path.Combine(Path.GetTempPath(), $"TextureHawk-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_rootDirectory);
        _settingsPath = Path.Combine(_rootDirectory, "settings.json");
    }

    [Fact]
    public void Load_PreservesExistingProcessorOutputDirectory()
    {
        var globalDirectory = Directory.CreateDirectory(Path.Combine(_rootDirectory, "global")).FullName;
        var processorDirectory = Directory.CreateDirectory(Path.Combine(_rootDirectory, "processor")).FullName;

        WriteSettings(new AppSettings
        {
            OutputDirectory = globalDirectory,
            ProcessorDefaults = new Dictionary<string, ProcessorDefaults>
            {
                ["builtin.test"] = new()
                {
                    OutputDirectory = processorDirectory
                }
            }
        });

        var loaded = new SettingsManager(_settingsPath).Load();

        Assert.Equal(processorDirectory, loaded.ProcessorDefaults["builtin.test"].OutputDirectory);
    }

    [Fact]
    public void Load_NormalizesBlankProcessorOverridesToInheritance()
    {
        WriteSettings(new AppSettings
        {
            ProcessorDefaults = new Dictionary<string, ProcessorDefaults>
            {
                ["builtin.test"] = new()
                {
                    OutputFormat = "   ",
                    OutputSuffix = "   ",
                    GpuColorSpaceMode = "   ",
                    ParameterValues = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["keep"] = "123"
                    }
                }
            }
        });

        var loaded = new SettingsManager(_settingsPath).Load();
        var defaults = loaded.ProcessorDefaults["builtin.test"];

        Assert.Null(defaults.OutputFormat);
        Assert.Null(defaults.OutputSuffix);
        Assert.Null(defaults.GpuColorSpaceMode);
        Assert.Equal("123", defaults.ParameterValues["keep"]);
    }

    [Fact]
    public void Load_PreservesExplicitEmptyProcessorDirectoryAndSuffix()
    {
        WriteSettings(new AppSettings
        {
            ProcessorDefaults = new Dictionary<string, ProcessorDefaults>
            {
                ["builtin.test"] = new()
                {
                    OutputDirectory = string.Empty,
                    OutputSuffix = string.Empty
                }
            }
        });

        var loaded = new SettingsManager(_settingsPath).Load();
        var defaults = loaded.ProcessorDefaults["builtin.test"];

        Assert.Equal(string.Empty, defaults.OutputDirectory);
        Assert.Equal(string.Empty, defaults.OutputSuffix);
    }

    [Fact]
    public void Load_LegacySettingsDefaultProcessorOutputTogglesToTrue()
    {
        File.WriteAllText(_settingsPath, "{}");

        var loaded = new SettingsManager(_settingsPath).Load();

        Assert.True(loaded.UseProcessorDefaultOutputDirectory);
        Assert.True(loaded.UseProcessorDefaultOutputFormat);
        Assert.True(loaded.UseProcessorDefaultOutputSuffix);
        Assert.True(loaded.UseProcessorDefaultGpuColorSpaceMode);
    }

    [Fact]
    public void Save_PersistsExplicitFalseProcessorOutputToggles()
    {
        var manager = new SettingsManager(_settingsPath);
        manager.Current.UseProcessorDefaultOutputDirectory = false;
        manager.Current.UseProcessorDefaultOutputFormat = false;
        manager.Current.UseProcessorDefaultOutputSuffix = false;
        manager.Current.UseProcessorDefaultGpuColorSpaceMode = false;

        manager.Save();

        var loaded = new SettingsManager(_settingsPath).Load();
        Assert.False(loaded.UseProcessorDefaultOutputDirectory);
        Assert.False(loaded.UseProcessorDefaultOutputFormat);
        Assert.False(loaded.UseProcessorDefaultOutputSuffix);
        Assert.False(loaded.UseProcessorDefaultGpuColorSpaceMode);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootDirectory))
            Directory.Delete(_rootDirectory, recursive: true);
    }

    private void WriteSettings(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_settingsPath, json);
    }
}