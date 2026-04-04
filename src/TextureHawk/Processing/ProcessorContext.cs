using RedFox.Graphics2D.IO;
using TextureHawk.Gpu;

namespace TextureHawk.Processing;

/// <summary>
/// Provides processor execution state, configured parameters, and access to services.
/// </summary>
/// <remarks>
/// Includes the selected GPU color-space mode, image translators, processor registry,
/// and the <see cref="ComputeEngine"/> used by GPU-backed processors.
/// </remarks>
public sealed class ProcessorContext
{
    /// <summary>
    /// Gets the file system path to the input resource required for processing.
    /// </summary>
    public required string InputPath { get; init; }

    /// <summary>
    /// Gets the collection of additional file system paths to include as input sources.
    /// </summary>
    public IReadOnlyList<string> AdditionalInputPaths { get; init; } = [];

    /// <summary>
    /// Selected GPU color-space mode for this processor execution.
    /// </summary>
    public required GpuColorSpaceMode GpuColorSpaceMode { get; init; }

    /// <summary>
    /// Gets the collection of parameter names and their associated values.
    /// </summary>
    public IReadOnlyDictionary<string, object> ParameterValues { get; init; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets the processor registry used to manage available processors.
    /// </summary>
    public required ProcessorRegistry Registry { get; init; }

    /// <summary>
    /// Gets the manager responsible for handling image translation operations.
    /// </summary>
    public required ImageTranslatorManager TranslatorManager { get; init; }

    /// <summary>
    /// Gets the compute engine used to perform computational operations.
    /// </summary>
    public required IComputeEngine ComputeEngine { get; init; }

    /// <summary>
    /// Gets the output directory path where generated files will be written.
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Gets whether nested processor execution may overwrite existing files.
    /// </summary>
    public bool OverwriteExisting { get; init; }


    /// <summary>
    /// Gets the value of the specified parameter, converted to the requested type, or returns a default value if the
    /// parameter is not found or cannot be converted.
    /// </summary>
    /// <remarks>If the parameter exists but cannot be converted to the requested type, the method returns the
    /// provided default value. Conversion uses the invariant culture.</remarks>
    /// <typeparam name="T">The type to which the parameter value should be converted and returned.</typeparam>
    /// <param name="parameterId">The identifier of the parameter whose value is to be retrieved.</param>
    /// <param name="defaultValue">The value to return if the parameter is not found or cannot be converted to the specified type.</param>
    /// <returns>The value of the parameter converted to type T if found and convertible; otherwise, the specified default value.</returns>
    public T GetParameterValue<T>(string parameterId, T defaultValue)
    {
        if (!ParameterValues.TryGetValue(parameterId, out var value) || value is null)
            return defaultValue;

        if (value is T typed)
            return typed;

        return ProcessorParameterValueConverter.TryConvert(value, out T convertedValue)
            ? convertedValue
            : defaultValue;
    }

    /// <summary>
    /// Executes another registered processor through the pipeline using the derived nested output path for the supplied input.
    /// </summary>
    public ValueTask<ProcessingResult> ExecuteProcessorAsync<TProcessor>(string inputPath, IReadOnlyDictionary<string, object>? parameterValues, IReadOnlyList<string>? additionalInputPaths, GpuColorSpaceMode? gpuColorSpaceMode, CancellationToken cancellationToken) where TProcessor : class, IProcessor
    {
        return ExecuteProcessorAsync<TProcessor>(inputPath, BuildNestedOutputPath(inputPath), parameterValues, additionalInputPaths, gpuColorSpaceMode, cancellationToken);
    }

    /// <summary>
    /// Executes another registered processor through the pipeline and writes its outputs.
    /// </summary>
    public ValueTask<ProcessingResult> ExecuteProcessorAsync<TProcessor>(string inputPath, string outputPath, IReadOnlyDictionary<string, object>? parameterValues, IReadOnlyList<string>? additionalInputPaths, GpuColorSpaceMode? gpuColorSpaceMode, CancellationToken cancellationToken) where TProcessor : class, IProcessor
    {
        var processor = Registry.GetProcessor<TProcessor>();
        return processor is null
            ? ValueTask.FromResult(ProcessingResult.Fail($"Processor '{typeof(TProcessor).FullName}' not found."))
            : ExecuteProcessingJobAsync(CreateProcessingJob(processor.Id, inputPath, outputPath, parameterValues, additionalInputPaths, gpuColorSpaceMode), cancellationToken);
    }

    /// <summary>
    /// Executes another registered processor through the pipeline using the derived nested output path for the supplied input.
    /// </summary>
    public ValueTask<ProcessingResult> ExecuteProcessorAsync(string processorId, string inputPath, IReadOnlyDictionary<string, object>? parameterValues, IReadOnlyList<string>? additionalInputPaths, GpuColorSpaceMode? gpuColorSpaceMode, CancellationToken cancellationToken)
    {
        return ExecuteProcessorAsync(processorId, inputPath, BuildNestedOutputPath(inputPath), parameterValues, additionalInputPaths, gpuColorSpaceMode, cancellationToken);
    }

    /// <summary>
    /// Executes another registered processor through the pipeline and writes its outputs.
    /// </summary>
    public ValueTask<ProcessingResult> ExecuteProcessorAsync(string processorId, string inputPath, string outputPath, IReadOnlyDictionary<string, object>? parameterValues, IReadOnlyList<string>? additionalInputPaths, GpuColorSpaceMode? gpuColorSpaceMode, CancellationToken cancellationToken)
    {
        var processor = Registry.GetProcessor(processorId);
        return processor is null
            ? ValueTask.FromResult(ProcessingResult.Fail($"Processor '{processorId}' not found."))
            : ExecuteProcessingJobAsync(CreateProcessingJob(processor.Id, inputPath, outputPath, parameterValues, additionalInputPaths, gpuColorSpaceMode), cancellationToken);
    }

    private ProcessingPipeline CreateNestedPipeline()
    {
        return new ProcessingPipeline(Registry, TranslatorManager, ComputeEngine, () => OverwriteExisting, () => GpuColorSpaceMode);
    }

    private ValueTask<ProcessingResult> ExecuteProcessingJobAsync(ProcessingJob job, CancellationToken cancellationToken)
    {
        return CreateNestedPipeline().ProcessAsync(job, null, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Builds the default nested output path for a child input using the current output suffix, extension, and explicit output directory override.
    /// </summary>
    public string BuildNestedOutputPath(string inputPath)
    {
        var parentDirectory = Path.GetDirectoryName(InputPath) ?? ".";
        var configuredDirectory = Path.GetDirectoryName(OutputPath) ?? parentDirectory;
        var outputDirectory = string.Equals(configuredDirectory, parentDirectory, StringComparison.OrdinalIgnoreCase)
            ? Path.GetDirectoryName(inputPath) ?? configuredDirectory
            : configuredDirectory;

        var inputName = Path.GetFileNameWithoutExtension(inputPath);
        var parentInputName = Path.GetFileNameWithoutExtension(InputPath);
        var configuredOutputName = Path.GetFileNameWithoutExtension(OutputPath);
        var suffix = configuredOutputName.StartsWith(parentInputName, StringComparison.OrdinalIgnoreCase)
            ? configuredOutputName[parentInputName.Length..]
            : string.Empty;
        var extension = Path.GetExtension(OutputPath);
        return Path.Combine(outputDirectory, $"{inputName}{suffix}{extension}");
    }

    private static ProcessingJob CreateProcessingJob(string processorId, string inputPath, string outputPath, IReadOnlyDictionary<string, object>? parameterValues, IReadOnlyList<string>? additionalInputPaths, GpuColorSpaceMode? gpuColorSpaceMode)
    {
        return new ProcessingJob
        {
            InputPath = inputPath,
            AdditionalInputPaths = additionalInputPaths ?? [],
            OutputPath = outputPath,
            ProcessorId = processorId,
            ParameterValues = parameterValues is null
                ? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, object>(parameterValues, StringComparer.OrdinalIgnoreCase),
            GpuColorSpaceMode = gpuColorSpaceMode
        };
    }
}
