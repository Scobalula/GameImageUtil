using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameImageUtil
{
    /// <summary>
    /// A class to hold File Processor Config
    /// </summary>
    public class FileProcessorConfig : IDisposable
    {
        /// <summary>
        /// Message Types for logging
        /// </summary>
        public enum MessageType
        {
            INFO,
            WARNING,
            ERROR,
        }

        /// <summary>
        /// Gets or Sets the Log Writer
        /// </summary>
        private StreamWriter? LogWriter { get; set; }

        /// <summary>
        /// Gets the Settings List
        /// </summary>
        public Dictionary<string, object> Settings { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Initializes a new instance of the FileProcessorConfig Class
        /// </summary>
        public FileProcessorConfig()
        {
            try
            {
                var logPath = Path.Combine("data\\Logs", string.Format("Log_{0}.txt", DateTime.Now.ToString("dd_MM_yyyy___HH_mm_ss")));

                if (Directory.Exists(Path.GetDirectoryName("data\\Logs")))
                    Directory.CreateDirectory("data\\Logs");

                LogWriter = new StreamWriter(logPath);
            }
            catch
            {
                LogWriter?.Dispose();
            }
        }

        /// <summary>
        /// Initializes a new instance of the FileProcessorConfig Class with existing values
        /// </summary>
        /// <param name="existingValues">Existing values to copy</param>
        public FileProcessorConfig(Dictionary<string, string> existingValues) : this()
        {
            foreach (var val in existingValues)
                Settings[val.Key] = val.Value;
        }

        /// <summary>
        /// Gets the Setting for the key
        /// </summary>
        /// <param name="setting">Key to obtain</param>
        /// <param name="defaultVal">Default Value</param>
        /// <returns>Key if found, otherwise default</returns>
        public T GetValue<T>(string setting, T defaultVal) => Settings.TryGetValue(setting, out var val) && val is T result ? result : defaultVal;


        /// <summary>
        /// Writes a message to the log
        /// </summary>
        /// <param name="message">Message to write</param>
        /// <param name="messageType">Message Type</param>
        public void Log(object message, MessageType messageType)
        {
            // Write to file
            if (LogWriter == null)
                return;

            lock (LogWriter)
            {
                LogWriter.WriteLine(string.Format("{0} [ {1} ] {2}", DateTime.Now.ToString("dd-MM-yyyy - HH:mm:ss"), messageType, message));
                LogWriter.Flush();
            }
        }

        /// <summary>
        /// Disposes of the config
        /// </summary>
        public void Dispose()
        {
            LogWriter?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
