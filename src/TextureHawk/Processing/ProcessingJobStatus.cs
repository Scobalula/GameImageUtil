namespace TextureHawk.Processing;

/// <summary>
/// Tracks the status of a processing job.
/// </summary>
public enum ProcessingJobStatus
{
    /// <summary>
    /// Job is queued and has not started.
    /// </summary>
    Pending,

    /// <summary>
    /// Input files are being loaded from disk.
    /// </summary>
    Loading,

    /// <summary>
    /// The processor is actively transforming the image.
    /// </summary>
    Processing,

    /// <summary>
    /// The output image is being written to disk.
    /// </summary>
    Writing,

    /// <summary>
    /// Job completed successfully.
    /// </summary>
    Completed,

    /// <summary>
    /// Job failed with an error.
    /// </summary>
    Failed
}
