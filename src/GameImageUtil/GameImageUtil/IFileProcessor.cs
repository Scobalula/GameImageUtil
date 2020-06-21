// ------------------------------------------------------------------------
// GameImageUtil - Tool to process game images
// Copyright (C) 2018 Philip/Scobalula
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// ------------------------------------------------------------------------

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
