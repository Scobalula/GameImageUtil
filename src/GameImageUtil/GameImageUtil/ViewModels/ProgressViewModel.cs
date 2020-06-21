// ------------------------------------------------------------------------
// GameImageUtil - Tool to process game images
// Copyright (C) 2020 Philip/Scobalula
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
    /// Progress Window View Model
    /// </summary>
    internal class ProgressWindowViewModel : Notifiable
    {
        /// <summary>
        /// Gets or Sets the number of items
        /// </summary>
        public double Count
        {
            get
            {
                return GetValue<double>("Count");
            }
            set
            {
                SetValue(value, "Count");
            }
        }

        /// <summary>
        /// Gets or Sets the current progress value
        /// </summary>
        public double Value
        {
            get
            {
                return GetValue<double>("Value");
            }
            set
            {
                SetValue(value, "Value");
            }
        }

        /// <summary>
        /// Gets or Sets if the progress bar is Indeterminate
        /// </summary>
        public bool Indeterminate
        {
            get
            {
                return GetValue<bool>("Indeterminate");
            }
            set
            {
                SetValue(value, "Indeterminate");
            }
        }

        /// <summary>
        /// Gets or Sets the Display Text
        /// </summary>
        public string Text
        {
            get
            {
                return GetValue<string>("Text");
            }
            set
            {
                SetValue(value, "Text");
            }
        }
    }
}