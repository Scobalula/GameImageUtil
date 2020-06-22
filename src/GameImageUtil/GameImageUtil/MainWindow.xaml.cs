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
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace GameImageUtil
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>
        /// Gets the Main View Model
        /// </summary>
        public AppViewModel ViewModel { get; } = new AppViewModel();

        /// <summary>
        /// Gets Progress Messages
        /// </summary>
        private string[] ProgressMessages { get; } =
        {
            "GameImageUtil | Doing the all thing....",
            "GameImageUtil | Taking over the world....",
            "GameImageUtil | Enhancing your life....",
            "GameImageUtil | Creating Black Ops II Mod Tools....",
            "GameImageUtil | Starting World War III....",
            "GameImageUtil | Getting rid of Shiba Charmy....",
            "GameImageUtil | Building Shot Shobe's Arena....",
            "GameImageUtil | Porting images in 600 hours....",
            "GameImageUtil | Deleting Box Maps off BO3 Workshop....",
            "GameImageUtil | Optimizing your map....",
            "GameImageUtil | Creating a keylogger....",
            "GameImageUtil | Calling in the moderation team on Discord....",
            "GameImageUtil | A bit slow are we....",
            "GameImageUtil | Developing nuclear weapons....",
            "GameImageUtil | xSanz78....",
            "GameImageUtil | Releasing the FX Pack....",
            "GameImageUtil | Shot Shobe > Shiba Charmy....",
            "GameImageUtil | Scamming someone for character ports....",
            "GameImageUtil | Creating a pandemic....",
            "GameImageUtil | Rocks in the brain....",
            "GameImageUtil | Nevis Christmas Release....",
            "GameImageUtil | E-Begging for Reece....",
        };

        /// <summary>
        /// Initializes new Window
        /// </summary>
        public MainWindow()
        {
            DataContext = ViewModel;
            InitializeComponent();

            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        }

        /// <summary>
        /// Attempts to resolve missing assemblys that are in the lib directories
        /// Fixes probing "probing privatePath="dir"" failing once outside Releases/Debug....
        /// </summary>
        private Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            // We should attempt to see if the assembly is already loaded first
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);

            if (assembly == null)
            {
                try
                {
                    // We're given a full string of info, so we need to split for just the name
                    assembly = Assembly.LoadFrom(Path.Combine("data\\lib", args.Name.Split(',')[0] + ".dll".ToLower()));
                }
#if DEBUG
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
#else
                catch { }
#endif
            }

            return assembly;
        }

        /// <summary>
        /// Opens the donation window (something you should do right now :))
        /// </summary>
        private void DonateClick(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.paypal.me/Scobalula");
        }

        /// <summary>
        /// Handles on drop
        /// </summary>
        private void DropBoxDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                new ProgressWindow(ProcessFiles, null, null, "Processing Images", files, files.Length, this, ProgressMessages[new Random().Next(0, ProgressMessages.Length)]).ShowDialog();
            }
        }

        /// <summary>
        /// Processes the provided files
        /// </summary>
        public void ProcessFiles(object sender, DoWorkEventArgs e)
        {
            var window = e.Argument as ProgressWindow;

            using (var config = new FileProcessorConfig() { ViewModel = ViewModel })
            {
                config.Settings["Extension"] = ViewModel.Extensions[ViewModel.SelectedExtension].ToLower();

                var processor = ViewModel.Processors[ViewModel.SelectedProcessor];

                switch (ViewModel.DXGIFormats[ViewModel.SelectedDXGIFormat])
                {
                    case "DXT1":
                        config.Settings["DXGIFormat"] = 71;
                        break;
                    case "DXT3":
                        config.Settings["DXGIFormat"] = 74;
                        break;
                    case "DXT5":
                        config.Settings["DXGIFormat"] = 77;
                        break;
                    case "BC4":
                        config.Settings["DXGIFormat"] = 80;
                        break;
                    case "BC5":
                        config.Settings["DXGIFormat"] = 83;
                        break;
                    default:
                        config.Settings["DXGIFormat"] = 71;
                        break;
                }

                Parallel.ForEach(window.Data as string[], new ParallelOptions { MaxDegreeOfParallelism = ViewModel.ThreadCount }, (file, loop) =>
                {
                    try
                    {
                        config.Log(string.Format("Processing {0}....", file), FileProcessorConfig.MessageType.INFO);
                        processor.Process(file, config);
                        config.Log(string.Format("Processed {0} successfully", file), FileProcessorConfig.MessageType.INFO);
                    }
                    catch (Exception exception)
                    {
                        config.Log(string.Format("Failed to process file {0}. Error:\n{1}", Path.GetFileNameWithoutExtension(file), exception), FileProcessorConfig.MessageType.ERROR);
                    }

                    window.Worker.ReportProgress(0);

                    if (window.Worker.CancellationPending)
                        loop.Break();
                });
            }
        }

        /// <summary>
        /// Handles on Window Closing
        /// </summary>
        private void WindowClosing(object sender, CancelEventArgs e)
        {
            ViewModel.CurrentSettings.Save("data\\Settings.bcfg");
        }
    }
}
