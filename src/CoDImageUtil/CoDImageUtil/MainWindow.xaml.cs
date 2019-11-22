using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using PhilLibX.Imaging;

namespace CoDImageUtil
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// The main viewmodel
        /// </summary>
        private readonly MainViewModel ViewModel = new MainViewModel();

        /// <summary>
        /// Creates the window
        /// </summary>
        public MainWindow()
        {
            DataContext = ViewModel;
            InitializeComponent();
        }

        /// <summary>
        /// Handles showing the DXGI format
        /// </summary>
        private void OutputFormatChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.DXGIVisibility = OutputFormat.SelectedItem.ToString() == ".DDS" ? Visibility.Visible : Visibility.Hidden;
        }

        /// <summary>
        /// Attempts to resolve the image most based off the file name
        /// </summary>
        private string DetectImageMode(string imageName)
        {
            // Check for NOG first from IW
            if (imageName.Contains("packed_nog") || imageName.Contains("packed_ng"))
                return "Split Normal/Gloss/Occlusion (CoD IW/MW 2019)";
            // Check for NOG from MW 2019
            if (imageName.Contains("_n&") && imageName.Contains("_g~"))
                return "Split Normal/Gloss/Occlusion (CoD IW/MW 2019)";
            // Check for color maps from IW (require splitting, then marge Alpha)
            if (imageName.Contains("packed_cs"))
                return "Split Specular Color (CoD IW/MW 2019)";
            // Check for color maps from MW 2019 (require splitting, then marge Alpha)
            if (imageName.Contains("_c&") && imageName.Contains("_s~"))
                return "Split Specular Color (CoD IW/MW 2019)";
            // Just direct convert it
            return "Direct Convert";
        }

        /// <summary>
        /// Removes alpha from the image
        /// </summary>
        private void RemoveAlpha(string imagePath, string extension, ScratchImage.DXGIFormat outFormat, string outputPath = null)
        {
            using (var inputImage = new ScratchImage(imagePath))
            {
                // Force the image to a standard format
                inputImage.ConvertImage(ScratchImage.DXGIFormat.R8G8B8A8UNORM);

                var inputPixels = inputImage.GetPixels();
                var outputPixels = new byte[inputPixels.Length];

                for (int i = 0; i < inputPixels.Length; i += 4)
                {
                    outputPixels[i + 0] = inputPixels[i + 0];
                    outputPixels[i + 1] = inputPixels[i + 1];
                    outputPixels[i + 2] = inputPixels[i + 2];
                    outputPixels[i + 3] = 0xFF;
                }

                if(outputPath == null)
                    outputPath = Path.Combine(Path.GetDirectoryName(imagePath), Path.GetFileNameWithoutExtension(imagePath));

                using (var output = new ScratchImage(inputImage.Metadata, outputPixels))
                {
                    if (extension == ".dds")
                    {
                        output.ConvertImage(outFormat);
                    }

                    output.Save(outputPath + extension);
                }
            }
        }

        /// <summary>
        /// Computes the Z channel of an XY normal map
        /// </summary>
        private void ConvertXYNormal(string imagePath, string extension, ScratchImage.DXGIFormat outFormat, string outputPath = null)
        {
            using (var inputImage = new ScratchImage(imagePath))
            {
                // Force the image to a standard format
                inputImage.ConvertImage(ScratchImage.DXGIFormat.R8G8B8A8UNORM);

                var inputPixels = inputImage.GetPixels();
                var outputPixels = new byte[inputPixels.Length];

                for (int i = 0; i < inputPixels.Length; i += 4)
                {
                    outputPixels[i + 0] = inputPixels[i + 0];
                    outputPixels[i + 1] = inputPixels[i + 1];
                    outputPixels[i + 2] = Helpers.CalculateBlueValue(inputPixels[i + 0], inputPixels[i + 1]);
                    outputPixels[i + 3] = 0xFF;
                }

                if(outputPath == null)
                    outputPath = Path.Combine(Path.GetDirectoryName(imagePath), Path.GetFileNameWithoutExtension(imagePath));

                using (var output = new ScratchImage(inputImage.Metadata, outputPixels))
                {
                    if (extension == ".dds")
                    {
                        output.ConvertImage(outFormat);
                    }

                    output.Save(outputPath + extension);
                }
            }
        }

        /// <summary>
        /// Converts an old normal map to XYZ
        /// </summary>
        private void ConvertOldNormal(string imagePath, string extension, ScratchImage.DXGIFormat outFormat, string outputPath = null)
        {
            using (var inputImage = new ScratchImage(imagePath))
            {
                // Force the image to a standard format
                inputImage.ConvertImage(ScratchImage.DXGIFormat.R8G8B8A8UNORM);

                var inputPixels = inputImage.GetPixels();
                var outputPixels = new byte[inputPixels.Length];

                for (int i = 0; i < inputPixels.Length; i += 4)
                {
                    outputPixels[i + 0] = inputPixels[i + 3];
                    outputPixels[i + 1] = inputPixels[i];
                    outputPixels[i + 2] = Helpers.CalculateBlueValue(inputPixels[i + 1], inputPixels[i + 3]);
                    outputPixels[i + 3] = 0xFF;
                }

                if(outputPath == null)
                    outputPath = Path.Combine(Path.GetDirectoryName(imagePath), Path.GetFileNameWithoutExtension(imagePath));

                using (var output = new ScratchImage(inputImage.Metadata, outputPixels))
                {
                    if (extension == ".dds")
                    {
                        output.ConvertImage(outFormat);
                    }

                    output.Save(outputPath + extension);
                }
            }
        }

        /// <summary>
        /// Splits the image by all channels
        /// </summary>
        private void SplitRGBA(string imagePath, string extension, ScratchImage.DXGIFormat outFormat, string outputPath = null)
        {
            using (var inputImage = new ScratchImage(imagePath))
            {
                // Force the image to a standard format
                inputImage.ConvertImage(ScratchImage.DXGIFormat.R8G8B8A8UNORM);

                var inputPixels = inputImage.GetPixels();
                var rPixels = new byte[inputPixels.Length];
                var gPixels = new byte[inputPixels.Length];
                var bPixels = new byte[inputPixels.Length];
                var aPixels = new byte[inputPixels.Length];

                for (int i = 0; i < inputPixels.Length; i += 4)
                {
                    // Copy Red
                    rPixels[i + 0] = inputPixels[i];
                    rPixels[i + 1] = inputPixels[i];
                    rPixels[i + 2] = inputPixels[i];
                    rPixels[i + 3] = 0xFF;
                    // Copy Blue
                    bPixels[i + 0] = inputPixels[i + 1];
                    bPixels[i + 1] = inputPixels[i + 1];
                    bPixels[i + 2] = inputPixels[i + 1];
                    bPixels[i + 3] = 0xFF;
                    // Copy Green
                    gPixels[i + 0] = inputPixels[i + 2];
                    gPixels[i + 1] = inputPixels[i + 2];
                    gPixels[i + 2] = inputPixels[i + 2];
                    gPixels[i + 3] = 0xFF;
                    // Copy Alpha
                    aPixels[i + 0] = inputPixels[i + 3];
                    aPixels[i + 1] = inputPixels[i + 3];
                    aPixels[i + 2] = inputPixels[i + 3];
                    aPixels[i + 3] = 0xFF;
                }

                if(outputPath == null)
                    outputPath = Path.Combine(
                        Path.GetDirectoryName(imagePath),
                        Path.GetFileNameWithoutExtension(imagePath).Replace("-", "_").Replace("~", "").Replace("&", "_"));

                using (var r = new ScratchImage(inputImage.Metadata, rPixels))
                using (var g = new ScratchImage(inputImage.Metadata, bPixels))
                using (var b = new ScratchImage(inputImage.Metadata, gPixels))
                using (var a = new ScratchImage(inputImage.Metadata, aPixels))
                {
                    if (extension == ".dds")
                    {
                        r.ConvertImage(outFormat);
                        g.ConvertImage(outFormat);
                        b.ConvertImage(outFormat);
                        a.ConvertImage(outFormat);
                    }

                    r.Save(outputPath + "_r" + extension);
                    g.Save(outputPath + "_g" + extension);
                    b.Save(outputPath + "_b" + extension);
                    a.Save(outputPath + "_a" + extension);
                }
            }
        }

        /// <summary>
        /// Splits normal gloss occlusion
        /// </summary>
        private void SplitNOG(string imagePath, string extension, ScratchImage.DXGIFormat outFormat, string outputPath = null)
        {
            using(var inputImage = new ScratchImage(imagePath))
            {
                // Force the image to a standard format
                inputImage.ConvertImage(ScratchImage.DXGIFormat.R8G8B8A8UNORM);

                var inputPixels = inputImage.GetPixels();
                var glossPixels = new byte[inputPixels.Length];
                var normalPixels = new byte[inputPixels.Length];
                var aoPixels = new byte[inputPixels.Length];

                for (int i = 0; i < inputPixels.Length; i += 4)
                {
                    // Copy Gloss
                    glossPixels[i + 0] = inputPixels[i];
                    glossPixels[i + 1] = inputPixels[i];
                    glossPixels[i + 2] = inputPixels[i];
                    glossPixels[i + 3] = 0xFF;
                    // Compute Normal Map (Only XY is stored)
                    normalPixels[i + 0] = inputPixels[i + 1];
                    normalPixels[i + 1] = inputPixels[i + 3];
                    normalPixels[i + 2] = Helpers.CalculateBlueValue(inputPixels[i + 1], inputPixels[i + 3]);
                    normalPixels[i + 3] = 0xFF;
                    // Copy Ambient Occlusion
                    aoPixels[i + 0] = inputPixels[i + 2];
                    aoPixels[i + 1] = inputPixels[i + 2];
                    aoPixels[i + 2] = inputPixels[i + 2];
                    aoPixels[i + 3] = 0xFF;
                }

                if(outputPath == null)
                    outputPath = Path.Combine(Path.GetDirectoryName(imagePath), Path.GetFileNameWithoutExtension(imagePath).Split(new string[] { "_n&", "_n_" }, StringSplitOptions.None)[0]);

                using (var gloss = new ScratchImage(inputImage.Metadata, glossPixels))
                using (var normal = new ScratchImage(inputImage.Metadata, normalPixels))
                using (var ao = new ScratchImage(inputImage.Metadata, aoPixels))
                {
                    if(extension == ".dds")
                    {
                        gloss.ConvertImage(outFormat);
                        normal.ConvertImage(outFormat);
                        ao.ConvertImage(outFormat);
                    }

                    gloss.Save(outputPath + "_g" + extension);
                    normal.Save(outputPath + "_n" + extension);
                    ao.Save(outputPath + "_o" + extension);
                }
            }
        }

        /// <summary>
        /// Splits by color and alpha
        /// </summary>
        private void SplitColorAlpha(string imagePath, string extension, ScratchImage.DXGIFormat outFormat, string outputPath = null)
        {
            using (var inputImage = new ScratchImage(imagePath))
            {
                // Force the image to a standard format
                inputImage.ConvertImage(ScratchImage.DXGIFormat.R8G8B8A8UNORM);

                var inputPixels = inputImage.GetPixels();
                var colorPixels = new byte[inputPixels.Length];
                var alphaPixels = new byte[inputPixels.Length];

                for (int i = 0; i < inputPixels.Length; i += 4)
                {
                    // Copy Gloss
                    colorPixels[i + 0] = inputPixels[i + 0];
                    colorPixels[i + 1] = inputPixels[i + 1];
                    colorPixels[i + 2] = inputPixels[i + 2];
                    colorPixels[i + 3] = 0xFF;
                    // Compute Normal Map (Only XY is stored)
                    alphaPixels[i + 0] = inputPixels[i + 3];
                    alphaPixels[i + 1] = inputPixels[i + 3];
                    alphaPixels[i + 2] = inputPixels[i + 3];
                    alphaPixels[i + 3] = 0xFF;
                }

                if(outputPath == null)
                    outputPath = Path.Combine(
                        Path.GetDirectoryName(imagePath), 
                        Path.GetFileNameWithoutExtension(imagePath).Split(new string[] { "&" }, StringSplitOptions.None)[0].Replace("-", "_").Replace("~", ""));

                using (var color = new ScratchImage(inputImage.Metadata, colorPixels))
                using (var alpha = new ScratchImage(inputImage.Metadata, alphaPixels))
                {
                    if (extension == ".dds")
                    {
                        color.ConvertImage(outFormat);
                        alpha.ConvertImage(outFormat);
                    }

                    color.Save(outputPath + "_s" + extension);
                    alpha.Save(outputPath + "_g" + extension);
                }
            }
        }

        /// <summary>
        /// Splits Color and Spec from IW
        /// </summary>
        private void SplitCSIW(string imagePath, string extension, ScratchImage.DXGIFormat outFormat, string outputPath = null)
        {
            using (var inputImage = new ScratchImage(imagePath))
            {
                // Force the image to a standard format
                inputImage.ConvertImage(ScratchImage.DXGIFormat.R8G8B8A8UNORM);

                var inputPixels = inputImage.GetPixels();
                var colorPixels = new byte[inputPixels.Length];
                var specPixels = new byte[inputPixels.Length];

                for (int i = 0; i < inputPixels.Length; i += 4)
                {
                    // Obtain the "Mask" from the Alpha Channel as a multiplier
                    // with our color mask being the remainder
                    var specMask = inputPixels[i + 3] / 255.0;
                    var colorMask = 1.0 - specMask;

                    // Color Values
                    colorPixels[i + 0] = (byte)(inputPixels[i + 0] * colorMask);
                    colorPixels[i + 1] = (byte)(inputPixels[i + 1] * colorMask);
                    colorPixels[i + 2] = (byte)(inputPixels[i + 2] * colorMask);
                    colorPixels[i + 3] = 255;
                    // Spec Values
                    specPixels[i + 0] = Helpers.Clamp<byte>((byte)(inputPixels[i + 0] * specMask), 255, 56);
                    specPixels[i + 1] = Helpers.Clamp<byte>((byte)(inputPixels[i + 1] * specMask), 255, 56);
                    specPixels[i + 2] = Helpers.Clamp<byte>((byte)(inputPixels[i + 2] * specMask), 255, 56);
                    specPixels[i + 3] = 255;
                }

                if(outputPath == null)
                    outputPath = Path.Combine(Path.GetDirectoryName(imagePath), Path.GetFileNameWithoutExtension(imagePath).Split(new string[] { "_c&", "_c_" }, StringSplitOptions.None)[0]);

                using (var color = new ScratchImage(inputImage.Metadata, colorPixels))
                using (var spec = new ScratchImage(inputImage.Metadata, specPixels))
                {
                    if (extension == ".dds")
                    {
                        color.ConvertImage(outFormat);
                        spec.ConvertImage(outFormat);
                    }

                    color.Save(outputPath + "_c" + extension);
                    spec.Save(outputPath + "_s" + extension);
                }
            }
        }

        /// <summary>
        /// Splits Specular Color by Mask image with same name
        /// </summary>
        private void SplitCS(string imagePath, string extension, ScratchImage.DXGIFormat outFormat, string outputPath = null)
        {
            var maskPath = Path.Combine(Path.GetDirectoryName(imagePath), Path.GetFileNameWithoutExtension(imagePath) + "_mask" + Path.GetExtension(imagePath));

            using (var maskImage = new ScratchImage(maskPath))
            using (var inputImage = new ScratchImage(imagePath))
            {
                // Force the image to a standard format
                inputImage.ConvertImage(ScratchImage.DXGIFormat.R8G8B8A8UNORM);
                maskImage.ConvertImage(ScratchImage.DXGIFormat.R8G8B8A8UNORM);

                var inputPixels = inputImage.GetPixels();
                var maskPixels = maskImage.GetPixels();

                var colorPixels = new byte[inputPixels.Length];
                var specPixels = new byte[inputPixels.Length];

                for (int i = 0; i < inputPixels.Length; i += 4)
                {
                    // Obtain the "Mask" from the Red Channel as a multiplier
                    // with our color mask being the remainder
                    var specMask = maskPixels[i] / 255.0;
                    var colorMask = 1.0 - specMask;

                    // Color Values
                    colorPixels[i + 0] = (byte)(inputPixels[i + 0] * colorMask);
                    colorPixels[i + 1] = (byte)(inputPixels[i + 1] * colorMask);
                    colorPixels[i + 2] = (byte)(inputPixels[i + 2] * colorMask);
                    colorPixels[i + 3] = 255;
                    // Spec Values
                    specPixels[i + 0] = Helpers.Clamp<byte>((byte)(inputPixels[i + 0] * specMask), 255, 56);
                    specPixels[i + 1] = Helpers.Clamp<byte>((byte)(inputPixels[i + 1] * specMask), 255, 56);
                    specPixels[i + 2] = Helpers.Clamp<byte>((byte)(inputPixels[i + 2] * specMask), 255, 56);
                    specPixels[i + 3] = 255;
                }

                if(outputPath == null)
                    outputPath = Path.Combine(Path.GetDirectoryName(imagePath), Path.GetFileNameWithoutExtension(imagePath).Split(new string[] { "_c&", "_c_" }, StringSplitOptions.None)[0]);

                using (var color = new ScratchImage(inputImage.Metadata, colorPixels))
                using (var spec = new ScratchImage(inputImage.Metadata, specPixels))
                {
                    if (extension == ".dds")
                    {
                        color.ConvertImage(outFormat);
                        spec.ConvertImage(outFormat);
                    }

                    color.Save(outputPath + "_c" + extension);
                    spec.Save(outputPath + "_s" + extension);
                }
            }
        }

        /// <summary>
        /// Converts the given image to any format
        /// </summary>
        private void ConvertImage(string imagePath, string extension, ScratchImage.DXGIFormat outFormat, string outputPath = null)
        {
            using(var image = new ScratchImage(imagePath))
            {
                // Force the image to a standard format
                image.ConvertImage(ScratchImage.DXGIFormat.R8G8B8A8UNORM);

                if(outputPath == null)
                    outputPath = Path.Combine(Path.GetDirectoryName(imagePath), Path.GetFileNameWithoutExtension(imagePath));

                if (extension == ".dds")
                    image.ConvertImage(outFormat);

                image.Save(outputPath + extension);
            }
        }

        /// <summary>
        /// Progresses the file of the given type
        /// </summary>
        private void ProcessImage(string imagePath, string extension, ScratchImage.DXGIFormat outFormat, string mode, string outputPath = null)
        {
            // Check for NOG
            switch (mode)
            {
                case "Split RGB/A (Spec/Gloss, etc.)":
                    SplitColorAlpha(imagePath, extension, outFormat, outputPath);
                    break;
                case "Split All Channels":
                    SplitRGBA(imagePath, extension, outFormat, outputPath);
                    break;
                case "Patch Grey Normal Map (Older CoDs)":
                    ConvertOldNormal(imagePath, extension, outFormat, outputPath);
                    break;
                case "Patch Yellow Normal Map (XY)":
                    ConvertXYNormal(imagePath, extension, outFormat, outputPath);
                    break;
                case "Split Normal/Gloss/Occlusion (CoD IW/MW 2019)":
                    SplitNOG(imagePath, extension, outFormat, outputPath);
                    break;
                case "Split Specular Color (CoD IW/MW 2019)":
                    SplitCSIW(imagePath, extension, outFormat, outputPath);
                    break;
                case "Split Specular Color":
                    SplitCS(imagePath, extension, outFormat, outputPath);
                    break;
                case "Direct Convert":
                    ConvertImage(imagePath, extension, outFormat, outputPath);
                    break;
                case "Remove Alpha":
                    RemoveAlpha(imagePath, extension, outFormat, outputPath);
                    break;
                case "Automatic":
                    ProcessImage(imagePath, extension, outFormat, DetectImageMode(Path.GetFileNameWithoutExtension(imagePath)), outputPath);
                    break;
            }
        }

        /// <summary>
        /// Processes a list of images
        /// </summary>
        private void ProcessImages(string[] files)
        {
            var progressWindow = new ProgressWindow()
            {
                Owner = this,
                Title = "CoDImageUtil | Working"
            };

            Dispatcher.BeginInvoke(new Action(() => progressWindow.ShowDialog()));
            progressWindow.SetProgressCount(files.Length);
            progressWindow.SetProgressMessage("Processing Images...");

            var mode      = Mode.SelectedItem.ToString();
            var format    = Helpers.GetDXGIFormat(DDSFormat.SelectedItem.ToString());
            var extension = OutputFormat.SelectedItem.ToString().ToLower();

            new Thread(() =>
            {
                Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, (file, loop) =>
                {
                    try
                    {
                        ProcessImage(file, extension, format, mode);
                    }
                    catch
                    {
                    }

                    if (progressWindow.IncrementProgress())
                        loop.Break();
                });

                progressWindow.SetProgressMessage("Complete");
                progressWindow.ProgressComplete();
            }).Start();
        }

        private void ProcessMaterials(string[] files)
        {

            var progressWindow = new ProgressWindow()
            {
                Owner = this,
                Title = "CoDImageUtil | Working"
            };

            var mode      = Mode.SelectedItem.ToString();
            var format    = Helpers.GetDXGIFormat(DDSFormat.SelectedItem.ToString());
            var extension = OutputFormat.SelectedItem.ToString().ToLower();

            Dispatcher.BeginInvoke(new Action(() => progressWindow.ShowDialog()));
            progressWindow.SetProgressCount(files.Length);
            progressWindow.SetProgressMessage("Processing Images...");

            new Thread(() =>
            {
                foreach (var file in files)
                {
                    bool quit = false;

                    try
                    {
                        progressWindow.SetProgressMessage(string.Format("Processing {0}...", Path.GetFileName(file)));

                        var images = new Dictionary<string, string>();

                        foreach (var line in File.ReadLines(file))
                        {
                            if (line == "semantic,image_name")
                                continue;

                            var lineSplit = line.Split(',');

                            var imageType = lineSplit[0];
                            var imagePath = Path.Combine(Path.GetDirectoryName(file), "_images", lineSplit[1] + extension);

                            switch(imageType)
                            {
                                case "unk_semantic_0x0":
                                    images[imagePath] = "Split Specular Color (CoD IW/MW 2019)";
                                    break;
                                case "unk_semantic_0x9":
                                    images[imagePath] = "Split Normal/Gloss/Occlusion (CoD IW/MW 2019)";
                                    break;
                                case "unk_semantic_0x7B":
                                case "unk_semantic_0x7C":
                                    images[imagePath] = "Direct Convert";
                                    break;
                            }
                        }

                        progressWindow.SetProgressCount(images.Count);

                        Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)));

                        foreach (var image in images)
                        {
                            var outputPath = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file), Path.GetFileNameWithoutExtension(image.Key).Split('&')[0]);

                            try
                            {
                                ProcessImage(image.Key, extension, format, image.Value, outputPath);
                            }
                            catch
                            {

                            }

                            if (progressWindow.IncrementProgress())
                            {
                                quit = true;
                                break;
                            }
                        }

                        if (quit)
                            break;
                    }
                    catch
                    {
                        continue;
                    }
                }

                progressWindow.SetProgressMessage("Complete");
                progressWindow.ProgressComplete();
            }).Start();
        }

        /// <summary>
        /// Handles files dropped onto the window
        /// </summary>
        private void FilesDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if(files.Length > 0)
                {
                    if(Path.GetExtension(files[0]) == ".txt")
                    {
                        ProcessMaterials(files);
                    }
                    else
                    {
                        ProcessImages(files);
                    }
                }
            }
        }
    }
}
