using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vortice.Direct3D11;

namespace GameImageUtil
{
    /// <summary>
    /// An interface for file processors
    /// </summary>
    public interface IFileProcessor
    {
        /// <summary>
        /// Gets the ID of this Processor
        /// </summary>
        string ID { get; }

        /// <summary>
        /// Gets the Name of this Processor
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Initializes the processor.
        /// </summary>
        void Initialize(Instance instance);

        /// <summary>
        /// Processes the provided file
        /// </summary>
        /// <param name="file">File to process</param>
        /// <param name="config">Config with settings</param>
        void Process(string file, FileProcessorConfig config);

        /// <summary>
        /// Checks if we can process this file, this is used by the Automatic Processor and does not affect manually selected modes
        /// </summary>
        /// <param name="file">File to process</param>
        /// <param name="config">Config with settings</param>
        /// <returns>True if we can, otherwise False</returns>
        bool CanProcessFile(string file, FileProcessorConfig config);
    }
}
