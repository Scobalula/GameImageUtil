using RedFox.Graphics2D.IO;
using TextureHawk.Gpu;
using TextureHawk.Logging;

namespace TextureHawk.Processing;

/// <summary>
/// Orchestrates the execution of processing jobs.
/// </summary>
/// <remarks>
/// The pipeline provides processor context (input/output paths, parameters, services),
/// resolves the target processor from the <see cref="ProcessorRegistry"/>,
/// and writes the output using the appropriate translator.
/// Supports multi-output processors — additional images are written with an index suffix.
/// </remarks>
public sealed class ProcessingPipeline
{
    private readonly ProcessorRegistry _registry;
    private readonly ImageTranslatorManager _translatorManager;
    private readonly IComputeEngine _computeEngine;
    private readonly Func<bool> _overwriteExistingProvider;
    private readonly Func<GpuColorSpaceMode> _gpuColorSpaceModeProvider;

    public ProcessingPipeline(ProcessorRegistry registry, ImageTranslatorManager translatorManager, IComputeEngine computeEngine)
        : this(registry, translatorManager, computeEngine, static () => false, static () => GpuColorSpaceMode.Auto)
    {
    }

    public ProcessingPipeline(ProcessorRegistry registry, ImageTranslatorManager translatorManager, IComputeEngine computeEngine, Func<bool> overwriteExistingProvider)
        : this(registry, translatorManager, computeEngine, overwriteExistingProvider, static () => GpuColorSpaceMode.Auto)
    {
    }

    public ProcessingPipeline(ProcessorRegistry registry, ImageTranslatorManager translatorManager, IComputeEngine computeEngine, Func<bool> overwriteExistingProvider, Func<GpuColorSpaceMode> gpuColorSpaceModeProvider)
    {
        _registry = registry;
        _translatorManager = translatorManager;
        _computeEngine = computeEngine;
        _overwriteExistingProvider = overwriteExistingProvider;
        _gpuColorSpaceModeProvider = gpuColorSpaceModeProvider;
    }

    /// <summary>
    /// Executes a single processing job.
    /// </summary>
    public async ValueTask<ProcessingResult> ProcessAsync(ProcessingJob job, IProgress<ProcessingJobStatus>? progress, CancellationToken cancellationToken)
    {
        try
        {
            var processor = _registry.GetProcessor(job.ProcessorId);
            if (processor is null)
            {
                AppLog.Error("Pipeline", $"Processor '{job.ProcessorId}' not found");
                return ProcessingResult.Fail($"Processor '{job.ProcessorId}' not found.");
            }

            // Prepare processor context
            job.Status = ProcessingJobStatus.Loading;
            progress?.Report(job.Status);

            AppLog.Debug("Pipeline", $"Preparing input path: {job.InputPath}");

            // Process
            job.Status = ProcessingJobStatus.Processing;
            progress?.Report(job.Status);

            var context = new ProcessorContext
            {
                InputPath = job.InputPath,
                AdditionalInputPaths = job.AdditionalInputPaths,
                ParameterValues = BuildParameterValues(job.ParameterValues),
                GpuColorSpaceMode = job.GpuColorSpaceMode ?? _gpuColorSpaceModeProvider(),
                Registry = _registry,
                TranslatorManager = _translatorManager,
                ComputeEngine = _computeEngine,
                OutputPath = job.OutputPath,
                OverwriteExisting = _overwriteExistingProvider()
            };

            AppLog.Info("Pipeline", $"Running processor '{processor.Name}' on {Path.GetFileName(job.InputPath)}");
            var captureStarted = TryStartProcessorCapture(processor.Name);
            ProcessingResult result;
            try
            {
                result = await processor.ProcessAsync(context, cancellationToken);
            }
            finally
            {
                TryEndProcessorCapture(processor.Name, captureStarted);
            }

            if (!result.Success)
            {
                job.Status = ProcessingJobStatus.Failed;
                job.ErrorMessage = result.ErrorMessage;
                progress?.Report(job.Status);
                AppLog.Error("Pipeline", $"Processor failed: {result.ErrorMessage}");
                return result;
            }

            // Write output(s)
            job.Status = ProcessingJobStatus.Writing;
            progress?.Report(job.Status);

            var writeResult = ProcessingOutputWriter.Write(_translatorManager, job.OutputPath, result, context.OverwriteExisting, job.ParameterValues);
            if (!writeResult.Success)
                return FailWrite(job, progress, writeResult.ErrorMessage);

            job.Status = ProcessingJobStatus.Completed;
            progress?.Report(job.Status);
            var finalOutputPath = result.OutputPathOverride ?? job.OutputPath;
            AppLog.Info("Pipeline", $"Completed: {Path.GetFileName(finalOutputPath)}");
            return writeResult;
        }
        catch (OperationCanceledException)
        {
            job.Status = ProcessingJobStatus.Failed;
            job.ErrorMessage = "Processing was cancelled.";
            AppLog.Warn("Pipeline", "Job cancelled");
            return ProcessingResult.Fail(job.ErrorMessage);
        }
        catch (Exception ex)
        {
            job.Status = ProcessingJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            AppLog.Error("Pipeline", $"Job failed: {ex.Message}", ex);
            return ProcessingResult.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Processes multiple jobs sequentially, reporting progress for each.
    /// </summary>
    public async ValueTask<IReadOnlyList<ProcessingResult>> ProcessBatchAsync(IReadOnlyList<ProcessingJob> jobs, IProgress<(int Index, ProcessingJobStatus Status)>? progress, CancellationToken cancellationToken)
    {
        AppLog.Info("Pipeline", $"Starting batch of {jobs.Count} jobs");
        var results = new List<ProcessingResult>(jobs.Count);
        for (var i = 0; i < jobs.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var index = i;
            var jobProgress = progress is not null
                ? new Progress<ProcessingJobStatus>(s => progress.Report((index, s)))
                : null;

            results.Add(await ProcessAsync(jobs[i], jobProgress, cancellationToken));
        }
        AppLog.Info("Pipeline", $"Batch complete: {results.Count(r => r.Success)}/{jobs.Count} succeeded");
        return results;
    }

    private static IReadOnlyDictionary<string, object> BuildParameterValues(IReadOnlyDictionary<string, object> values)
    {
        return new Dictionary<string, object>(values, StringComparer.OrdinalIgnoreCase);
    }

    private static ProcessingResult FailWrite(ProcessingJob job, IProgress<ProcessingJobStatus>? progress, string? error)
    {
        job.Status = ProcessingJobStatus.Failed;
        job.ErrorMessage = error;
        progress?.Report(job.Status);
        AppLog.Warn("Pipeline", error ?? "Output write blocked.");
        return ProcessingResult.Fail(error ?? "Output write blocked.");
    }

    private bool TryStartProcessorCapture(string processorName)
    {
        try
        {
            var captureStarted = _computeEngine.CaptureStart(processorName);
            if (captureStarted)
                AppLog.Info("Pipeline", $"Started RenderDoc capture for processor '{processorName}'");

            return captureStarted;
        }
        catch (Exception ex)
        {
            AppLog.Warn("Pipeline", $"Failed to start RenderDoc capture for processor '{processorName}': {ex.Message}");
            return false;
        }
    }

    private void TryEndProcessorCapture(string processorName, bool captureStarted)
    {
        if (!captureStarted)
            return;

        try
        {
            _computeEngine.CaptureEnd();
            AppLog.Info("Pipeline", $"Ended RenderDoc capture for processor '{processorName}'");
        }
        catch (Exception ex)
        {
            AppLog.Warn("Pipeline", $"Failed to end RenderDoc capture for processor '{processorName}': {ex.Message}");
        }
    }
}
