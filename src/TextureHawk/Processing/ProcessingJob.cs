using TextureHawk.Gpu;

namespace TextureHawk.Processing;

/// <summary>
/// Represents a single processing task with its inputs, outputs, and status.
/// </summary>
public sealed class ProcessingJob
{
    /// <summary>
    /// Unique identifier for this job.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Path to the primary input file.
    /// </summary>
    public required string InputPath { get; init; }

    /// <summary>
    /// Paths to additional input files (for multi-input processors).
    /// </summary>
    public IReadOnlyList<string> AdditionalInputPaths { get; init; } = [];

    /// <summary>
    /// Path where the output should be written.
    /// </summary>
    public required string OutputPath { get; set; }

    /// <summary>
    /// ID of the processor to use.
    /// </summary>
    public required string ProcessorId { get; init; }

    /// <summary>
    /// Parameter values for the processor.
    /// </summary>
    public Dictionary<string, object> ParameterValues { get; init; } = [];

    /// <summary>
    /// Optional GPU color-space override for this job. Null uses the pipeline default.
    /// </summary>
    public GpuColorSpaceMode? GpuColorSpaceMode { get; init; }

    /// <summary>
    /// Current status of this job.
    /// </summary>
    public ProcessingJobStatus Status { get; set; } = ProcessingJobStatus.Pending;

    /// <summary>
    /// Error message if the job failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
