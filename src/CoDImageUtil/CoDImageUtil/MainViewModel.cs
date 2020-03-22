using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CoDImageUtil
{
    /// <summary>
    /// Main Viewmodel
    /// </summary>
    class MainViewModel : INotifyPropertyChanged
    {
        #region BackingVariables
        // Visibility
        private Visibility _DXGIVisibility = Visibility.Hidden;
        // Selected Index
        private int _SelectedIndex = 0;
        #endregion

        /// <summary>
        /// Gets or Sets the DXGI Visibility
        /// </summary>
        public Visibility DXGIVisibility
        {
            get
            {
                return _DXGIVisibility;
            }
            set
            {
                _DXGIVisibility = value;
                OnPropertyChanged("DXGIVisibility");
            }
        }

        /// <summary>
        /// Gets or Sets the DXGI Visibility
        /// </summary>
        public int SelectedIndex
        {
            get
            {
                return _SelectedIndex;
            }
            set
            {
                CurrentSettings["SelectedFormat"] = value.ToString();
                _SelectedIndex = value;
                OnPropertyChanged("SelectedIndex");
            }
        }

        /// <summary>
        /// Valid File Extensions for Read/Write
        /// </summary>
        public ObservableCollection<string> ValidExtensions { get; private set; }

        /// <summary>
        /// Valid DXGI Formats for DDS
        /// </summary>
        public ObservableCollection<string> ValidDXGIFormats { get; private set; }

        /// <summary>
        /// Valid Image Modes
        /// </summary>
        public ObservableCollection<string> ValidModes { get; private set; }

        /// <summary>
        /// Gets or Sets the current settings
        /// </summary>
        public Settings CurrentSettings { get; set; }

        /// <summary>
        /// Inits the ViewModel
        /// </summary>
        public MainViewModel()
        {
            ValidExtensions = new ObservableCollection<string>()
            {
                ".PNG",
                ".TIF",
                ".TIFF",
                ".JPG",
                ".TGA",
                ".BMP",
                ".DDS",
            };
            ValidDXGIFormats = new ObservableCollection<string>()
            {
                "DXT1",
                "DXT3",
                "DXT5",
                "BC4",
                "BC5",
            };
            ValidModes = new ObservableCollection<string>()
            {
                "Automatic",
                "Direct Convert",
                "Remove Alpha",
                "Patch Yellow Normal Map (XY)",
                "Patch Grey Normal Map (Older CoDs)",
                "Split Normal/Gloss/Occlusion (CoD IW/MW 2019)",
                "Split Normal/Gloss/Occlusion (CoD IW/MW 2019) (Experimental Normal Map Fix)",
                "Split RGB/A (Spec/Gloss, etc.)",
                "Split All Channels",
                "Split Specular Color (CoD IW/MW 2019)",
                "Split Specular Color",
                "Merge RGB/A",
            };
            CurrentSettings = new Settings("CoDImageUtilSettings.cfg");
            SelectedIndex = int.TryParse(CurrentSettings["SelectedFormat", "0"], out var index) ? index : 0;
        }

        /// <summary>
        /// Property Changed Event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Updates the Property on Change
        /// </summary>
        /// <param name="name">Property Name</param>
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
