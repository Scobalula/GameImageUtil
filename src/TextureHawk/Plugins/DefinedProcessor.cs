using TextureHawk.Configuration;
using TextureHawk.Gpu;
using TextureHawk.Processing;
using System.Text.Json;

namespace TextureHawk.Plugins;

/// <summary>
/// An <see cref="IProcessor"/> wrapping a <see cref="ProcessorDefinition"/>.
/// </summary>
/// <remarks>
/// Delegates actual processing to a base processor with overridden defaults,
/// or to a compute shader via the GPU engine. Definitions are loaded from
/// external files (currently TOML; extensible to other formats).
/// </remarks>
public sealed class DefinedProcessor : IProcessor
{
    private readonly ProcessorDefinition _definition;
    private readonly string? _shaderDirectory;

    /// <summary>
    /// Creates a new <see cref="DefinedProcessor"/> from the given definition.
    /// </summary>
    /// <param name="definition">The processor definition to wrap.</param>
    public DefinedProcessor(ProcessorDefinition definition, string? shaderDirectory = null)
    {
        _definition = definition;
        _shaderDirectory = shaderDirectory;
    }

    /// <inheritdoc />
    public string Id => _definition.Id;

    /// <inheritdoc />
    public string Name => _definition.Name;

    /// <inheritdoc />
    public string Description => _definition.Description;

    /// <inheritdoc />
    public string Category => _definition.Category;

    /// <inheritdoc />
    public string Icon => _definition.Icon;

    /// <inheritdoc />
    public IReadOnlyList<ProcessorParameter> Parameters => _definition.Parameters.Select(p => new ProcessorParameter
    {
        Id = p.Id,
        Name = p.Name,
        Description = p.Description,
        Type = ParseParameterType(p.Type),
        DefaultValue = p.Default,
        MinValue = p.Min,
        MaxValue = p.Max,
        Options = p.Options
    }).ToList();

    /// <inheritdoc />
    public async ValueTask<ProcessingResult> ProcessAsync(ProcessorContext context, CancellationToken cancellationToken)
    {
        var mergedValues = CreateMergedValues(context.ParameterValues);

        if (_definition.BasedOnProcessorId is { } baseId)
        {
            var baseProcessor = context.Registry.GetProcessor(baseId);
            if (baseProcessor is null)
                return ProcessingResult.Fail($"Base processor '{baseId}' not found.");

            var delegateContext = new ProcessorContext
            {
                InputPath = context.InputPath,
                AdditionalInputPaths = context.AdditionalInputPaths,
                ParameterValues = mergedValues,
                GpuColorSpaceMode = context.GpuColorSpaceMode,
                Registry = context.Registry,
                TranslatorManager = context.TranslatorManager,
                ComputeEngine = context.ComputeEngine,
                OutputPath = context.OutputPath
            };

            return await baseProcessor.ProcessAsync(delegateContext, cancellationToken);
        }

        if (_definition.ShaderPath is not null)
        {
            try
            {
                return await ExecuteShaderAsync(context, mergedValues, cancellationToken);
            }
            catch (Exception ex)
            {
                return ProcessingResult.Fail(ex.Message);
            }
        }

        return ProcessingResult.Fail("Defined processor must specify either 'basedOn' or 'shader'.");
    }

    /// <summary>
    /// Loads all processor definitions from a directory, detecting format by file extension.
    /// </summary>
    /// <remarks>
    /// Currently supports <c>.toml</c> files. Additional formats can be added
    /// by extending the extension-to-parser dispatch.
    /// </remarks>
    /// <param name="directory">The directory to scan for definition files.</param>
    /// <returns>A list of processors created from the definitions found.</returns>
    public static List<DefinedProcessor> LoadFromDirectory(string directory, string? shaderDirectory = null)
    {
        var processors = new List<DefinedProcessor>();
        if (!Directory.Exists(directory))
            return processors;

        foreach (var file in Directory.EnumerateFiles(directory))
        {
            var ext = Path.GetExtension(file);
            if (!ext.Equals(".toml", StringComparison.OrdinalIgnoreCase)
                && !ext.Equals(".json", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                var definition = LoadDefinition(file);
                if (definition is not null)
                    processors.Add(new DefinedProcessor(definition, shaderDirectory));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to load processor definition from {file}: {ex.Message}");
            }
        }
        return processors;
    }

    private static ProcessorDefinition? LoadDefinition(string path)
    {
        var text = File.ReadAllText(path);
        return Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase)
            ? ParseJsonDefinition(text)
            : ParseTomlDefinition(text);
    }

    /// <summary>
    /// Parses a TOML string into a <see cref="ProcessorDefinition"/>.
    /// </summary>
    /// <param name="toml">The TOML text to parse.</param>
    /// <returns>The parsed definition, or null if required fields are missing.</returns>
    private static ProcessorDefinition? ParseTomlDefinition(string toml)
    {
        var data = SimpleTomlParser.Parse(toml);

        if (!data.TryGetValue("id", out var idObj) || idObj is not string id)
            return null;
        if (!data.TryGetValue("name", out var nameObj) || nameObj is not string name)
            return null;

        var definition = new ProcessorDefinition
        {
            Id = id,
            Name = name,
            Description = data.TryGetValue("description", out var desc) ? desc as string ?? "" : "",
            Category = data.TryGetValue("category", out var cat) ? cat as string ?? "Custom" : "Custom",
            Icon = data.TryGetValue("icon", out var icon) ? icon as string ?? "🔧" : "🔧",
            ShaderPath = data.TryGetValue("shader", out var shader) ? shader as string : null,
            BasedOnProcessorId = data.TryGetValue("basedOn", out var basedOn) ? basedOn as string : null,
            OutputWidthParameterId = data.TryGetValue("outputWidthParameter", out var outWidthParam) ? outWidthParam as string : null,
            OutputHeightParameterId = data.TryGetValue("outputHeightParameter", out var outHeightParam) ? outHeightParam as string : null,
            WorkGroupSizeX = Math.Max(1, GetIntValue(data, "workGroupSizeX", 16)),
            WorkGroupSizeY = Math.Max(1, GetIntValue(data, "workGroupSizeY", 16)),
            WorkGroupSizeZ = Math.Max(1, GetIntValue(data, "workGroupSizeZ", 1))
        };

        // Parse defaults table
        if (data.TryGetValue("defaults", out var defaultsObj) && defaultsObj is Dictionary<string, object> defaults)
        {
            foreach (var (key, value) in defaults)
                definition.Defaults[key] = value;
        }

        // Parse parameters array-of-tables
        if (data.TryGetValue("parameters", out var paramsObj) && paramsObj is List<Dictionary<string, object>> paramsList)
        {
            foreach (var paramDict in paramsList)
            {
                if (!paramDict.TryGetValue("id", out var pid) || pid is not string paramId)
                    continue;
                if (!paramDict.TryGetValue("type", out var ptype) || ptype is not string paramType)
                    continue;

                var param = new ParameterDefinition
                {
                    Id = paramId,
                    Name = paramDict.TryGetValue("name", out var pname) ? pname as string ?? paramId : paramId,
                    Description = paramDict.TryGetValue("description", out var pdesc) ? pdesc as string ?? "" : "",
                    Type = paramType,
                    Default = paramDict.GetValueOrDefault("default"),
                    Min = paramDict.GetValueOrDefault("min"),
                    Max = paramDict.GetValueOrDefault("max"),
                    Options = paramDict.TryGetValue("options", out var opts) && opts is List<string> optList ? optList : null
                };
                definition.Parameters.Add(param);
            }
        }

        return definition;
    }

    private static ProcessorDefinition? ParseJsonDefinition(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!root.TryGetProperty("id", out var idElement) || idElement.ValueKind != JsonValueKind.String)
            return null;
        if (!root.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
            return null;

        var definition = new ProcessorDefinition
        {
            Id = idElement.GetString()!,
            Name = nameElement.GetString()!,
            Description = GetString(root, "description"),
            Category = string.IsNullOrWhiteSpace(GetString(root, "category")) ? "Custom" : GetString(root, "category"),
            Icon = string.IsNullOrWhiteSpace(GetOptionalString(root, "icon")) ? "🔧" : GetOptionalString(root, "icon")!,
            ShaderPath = GetOptionalString(root, "shader"),
            BasedOnProcessorId = GetOptionalString(root, "basedOn"),
            OutputWidthParameterId = GetOptionalString(root, "outputWidthParameter"),
            OutputHeightParameterId = GetOptionalString(root, "outputHeightParameter"),
            WorkGroupSizeX = Math.Max(1, GetInt(root, "workGroupSizeX", 16)),
            WorkGroupSizeY = Math.Max(1, GetInt(root, "workGroupSizeY", 16)),
            WorkGroupSizeZ = Math.Max(1, GetInt(root, "workGroupSizeZ", 1))
        };

        if (root.TryGetProperty("defaults", out var defaultsElement) && defaultsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in defaultsElement.EnumerateObject())
                definition.Defaults[property.Name] = ConvertJsonValue(property.Value);
        }

        if (root.TryGetProperty("parameters", out var parametersElement) && parametersElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var paramElement in parametersElement.EnumerateArray())
            {
                if (!paramElement.TryGetProperty("id", out var paramIdElement) || paramIdElement.ValueKind != JsonValueKind.String)
                    continue;
                if (!paramElement.TryGetProperty("type", out var paramTypeElement) || paramTypeElement.ValueKind != JsonValueKind.String)
                    continue;

                definition.Parameters.Add(new ParameterDefinition
                {
                    Id = paramIdElement.GetString()!,
                    Name = GetOptionalString(paramElement, "name") ?? paramIdElement.GetString()!,
                    Description = GetString(paramElement, "description"),
                    Type = paramTypeElement.GetString()!,
                    Default = paramElement.TryGetProperty("default", out var defaultElement) ? ConvertJsonValue(defaultElement) : null,
                    Min = paramElement.TryGetProperty("min", out var minElement) ? ConvertJsonValue(minElement) : null,
                    Max = paramElement.TryGetProperty("max", out var maxElement) ? ConvertJsonValue(maxElement) : null,
                    Options = paramElement.TryGetProperty("options", out var optionsElement) ? GetStringList(optionsElement) : null
                });
            }
        }

        return definition;
    }

    private async ValueTask<ProcessingResult> ExecuteShaderAsync(ProcessorContext context, IReadOnlyDictionary<string, object> mergedValues, CancellationToken cancellationToken)
    {
        var shaderPath = ResolveShaderPath();
        var shaderSource = File.ReadAllText(shaderPath);
        var spirv = Path.GetExtension(shaderPath).Equals(".spv", StringComparison.OrdinalIgnoreCase)
            ? File.ReadAllBytes(shaderPath)
            : ShaderCompiler.Compile(shaderSource, Path.GetFileName(shaderPath));
        var layoutSource = Path.GetExtension(shaderPath).Equals(".spv", StringComparison.OrdinalIgnoreCase)
            ? ReadLayoutCompanionSource(shaderPath)
            : shaderSource;
        var layout = PushConstantLayout.Parse(layoutSource);
        var primaryInput = context.TranslatorManager.Read(context.InputPath);
        var outputWidth = ResolveOutputDimension(mergedValues, _definition.OutputWidthParameterId, primaryInput.Width);
        var outputHeight = ResolveOutputDimension(mergedValues, _definition.OutputHeightParameterId, primaryInput.Height);
        var pushConstants = BuildPushConstants(layout, primaryInput, mergedValues, outputWidth, outputHeight);
        var inputs = new List<RedFox.Graphics2D.Image>(1 + context.AdditionalInputPaths.Count) { primaryInput };
        for (var i = 0; i < context.AdditionalInputPaths.Count; i++)
        {
            var path = context.AdditionalInputPaths[i];
            var extension = Path.GetExtension(path);
            if (!context.TranslatorManager.TryGetTranslator(path, extension, out var translator) || !translator.CanRead)
                return ProcessingResult.Fail($"Additional input is not readable as an image: {path}");

            inputs.Add(context.TranslatorManager.Read(path));
        }
        var output = await context.ComputeEngine.DispatchAsync(
            spirv,
            GpuImageFormats.PrepareInputsForSampling(inputs, context.GpuColorSpaceMode),
            pushConstants,
            outputWidth,
            outputHeight,
            GpuImageFormats.ResolveWritableOutputFormat(primaryInput.Format),
            (_definition.WorkGroupSizeX, _definition.WorkGroupSizeY, _definition.WorkGroupSizeZ),
            cancellationToken);
        return ProcessingResult.Ok(output);
    }

    private string ResolveShaderPath()
    {
        var shaderPath = _definition.ShaderPath ?? throw new InvalidOperationException("Shader path was not defined.");
        if (Path.IsPathRooted(shaderPath))
            return shaderPath;

        if (string.IsNullOrWhiteSpace(_shaderDirectory))
            throw new InvalidOperationException($"Relative shader path '{shaderPath}' cannot be resolved without a shader directory.");

        return Path.Combine(_shaderDirectory, shaderPath);
    }

    private static string ReadLayoutCompanionSource(string spirvPath)
    {
        var companionSource = Path.ChangeExtension(spirvPath, ".comp");
        if (!File.Exists(companionSource))
            throw new FileNotFoundException($"Shader source companion not found for '{spirvPath}'. Expected '{companionSource}'.");

        return File.ReadAllText(companionSource);
    }

    private static byte[] BuildPushConstants(PushConstantLayout layout, RedFox.Graphics2D.Image primaryInput, IReadOnlyDictionary<string, object> values, int outputWidth, int outputHeight)
    {
        if (layout.TotalSize == 0)
            return [];

        var builder = layout.CreateBuilder();
        foreach (var field in layout.Fields)
        {
            switch (field.Name)
            {
                case "outWidth":
                    builder.Set(field.Name, outputWidth);
                    continue;
                case "outHeight":
                    builder.Set(field.Name, outputHeight);
                    continue;
                case "srcWidth":
                    builder.Set(field.Name, primaryInput.Width);
                    continue;
                case "srcHeight":
                    builder.Set(field.Name, primaryInput.Height);
                    continue;
            }

            if (!values.TryGetValue(field.Name, out var value) || value is null)
                continue;

            SetPushConstantValue(builder, field, value);
        }

        return builder.Build();
    }

    private static void SetPushConstantValue(PushConstantBuilder builder, PushConstantField field, object value)
    {
        switch (field.Type)
        {
            case ShaderParameterType.Int:
            case ShaderParameterType.Int2:
            case ShaderParameterType.Int3:
            case ShaderParameterType.Int4:
                builder.Set(field.Name, Convert.ToInt32(value));
                break;
            case ShaderParameterType.UInt:
                builder.Set(field.Name, Convert.ToUInt32(value));
                break;
            case ShaderParameterType.Float:
            case ShaderParameterType.Float2:
            case ShaderParameterType.Float3:
            case ShaderParameterType.Float4:
                builder.Set(field.Name, Convert.ToSingle(value));
                break;
        }
    }

    private Dictionary<string, object> CreateMergedValues(IReadOnlyDictionary<string, object> parameterValues)
    {
        var mergedValues = new Dictionary<string, object>(_definition.Defaults.Count + parameterValues.Count, StringComparer.Ordinal);
        foreach (var (key, value) in _definition.Defaults)
        {
            if (value is not null)
                mergedValues[key] = value;
        }
        foreach (var (key, value) in parameterValues)
            mergedValues[key] = value;
        return mergedValues;
    }

    private static int ResolveOutputDimension(IReadOnlyDictionary<string, object> values, string? parameterId, int defaultValue)
    {
        if (parameterId is null)
            return defaultValue;

        if (!values.TryGetValue(parameterId, out var value) || value is null)
            return defaultValue;

        var resolved = Convert.ToInt32(value);
        return resolved > 0 ? resolved : defaultValue;
    }

    private static int GetIntValue(Dictionary<string, object> values, string key, int defaultValue = 0)
    {
        if (!values.TryGetValue(key, out var value) || value is null)
            return defaultValue;

        return Convert.ToInt32(value);
    }

    private static List<string> GetStringListValue(Dictionary<string, object> values, string key)
    {
        if (!values.TryGetValue(key, out var value) || value is null)
            return [];

        return value switch
        {
            List<string> strings => strings,
            IEnumerable<object> objects => objects.Select(item => item?.ToString() ?? string.Empty).Where(item => item.Length > 0).ToList(),
            _ => []
        };
    }

    private static string GetString(JsonElement root, string propertyName) => GetOptionalString(root, propertyName) ?? string.Empty;

    private static string? GetOptionalString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String)
            return null;

        return element.GetString();
    }

    private static int GetInt(JsonElement root, string propertyName, int defaultValue = 0)
    {
        if (!root.TryGetProperty(propertyName, out var element))
            return defaultValue;

        return element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(element.GetString(), out var parsed) => parsed,
            _ => defaultValue
        };
    }

    private static List<string> GetStringList(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var element))
            return [];

        return GetStringList(element);
    }

    private static List<string> GetStringList(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
            return [];

        var values = new List<string>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && item.GetString() is { Length: > 0 } text)
                values.Add(text);
        }

        return values;
    }

    private static object? ConvertJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when element.TryGetInt32(out var intValue) => intValue,
            JsonValueKind.Number when element.TryGetSingle(out var floatValue) => floatValue,
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonValue).ToList(),
            _ => null
        };
    }

    private static ProcessorParameterType ParseParameterType(string type) => type.ToLowerInvariant() switch
    {
        "bool" or "boolean" => ProcessorParameterType.Boolean,
        "int" or "integer" => ProcessorParameterType.Integer,
        "float" or "double" => ProcessorParameterType.Float,
        "string" or "text" => ProcessorParameterType.String,
        "choice" or "select" => ProcessorParameterType.Choice,
        "format" or "imageformat" => ProcessorParameterType.ImageFormat,
        "filepath" or "file" => ProcessorParameterType.FilePath,
        _ => ProcessorParameterType.String
    };
}
