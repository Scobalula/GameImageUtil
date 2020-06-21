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
using System.Collections.Generic;
using System.IO;

namespace GameImageUtil
{
    public class Settings
    {
        /// <summary>
        /// Setting Values
        /// </summary>
        private Dictionary<string, string> Values = new Dictionary<string, string>();

        /// <summary>
        /// Gets the setting with the given name, if not found, returns defaultVal
        /// </summary>
        public string this[string key, string defaultVal]
        {
            get
            {
                return Values.TryGetValue(key, out var val) ? val : defaultVal;
            }
        }

        /// <summary>
        /// Sets the setting with the given name
        /// </summary>
        public string this[string key]
        {
            set
            {
                Values[key] = value;
            }
        }

        /// <summary>
        /// Initializes an instance of the Settings Class
        /// </summary>
        public Settings() { }

        /// <summary>
        /// Initializes an instance of the Settings Class and loads the settings
        /// </summary>
        /// <param name="fileName">File Name</param>
        public Settings(string fileName)
        {
            Load(fileName);
        }

        /// <summary>
        /// Loads Settings from a file
        /// </summary>
        /// <param name="fileName">File Name</param>
        public void Load(string fileName)
        {
            try
            {
                if (!File.Exists(fileName))
                {
                    Save(fileName);
                }
                else
                {
                    using (var reader = new BinaryReader(new FileStream(fileName, FileMode.Open)))
                    {
                        if (reader.ReadUInt32() == 0x47464348)
                        {
                            int count = reader.ReadInt32();

                            for (int i = 0; i < count; i++)
                                Values[reader.ReadString()] = reader.ReadString();
                        }
                    }
                }
            }
            catch
            {
                return;
            }
        }

        /// <summary>
        /// Saves all settings to a file
        /// </summary>
        /// <param name="fileName">File Name</param>
        public void Save(string fileName)
        {
            try
            {
                using (var writer = new BinaryWriter(new FileStream(fileName, FileMode.Create)))
                {
                    writer.Write(0x47464348);
                    writer.Write(Values.Count);

                    foreach (var value in Values)
                    {
                        writer.Write(value.Key);
                        writer.Write(value.Value);
                    }
                }
            }
            catch
            {
                return;
            }
        }
    }
}