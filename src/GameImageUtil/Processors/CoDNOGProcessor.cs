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
using System.IO;
using System.Numerics;
using Vortice.Direct3D11;
using GameImageUtil.Assets;
using GameImageUtil.ShaderCode;
using GameImageUtil.Shaders;
using GameImageUtil.Imaging.DirectXTex;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameImageUtil.Converters
{
    /// <summary>
    /// A class to handle processing CoD NOG Images (Infinite Warfare/Modern Warfare)
    /// They are stored as follows:
    ///     Gloss Map
    ///     Normal X (Hemi-Octahedron)
    ///     Occlusion
    ///     Normal Y (Hemi-Octahedron)
    /// References:
    ///     https://www.activision.com/cdn/research/2017_DD_Rendering_of_COD_IW.pdf
    ///     http://jcgt.org/published/0003/02/01/
    ///     http://media.steampowered.com/apps/valve/2015/Alex_Vlachos_Advanced_VR_Rendering_GDC2015.pdf
    ///     RenderDoc for Shader Debugging
    /// </summary>
    public class CoDNOGProcessor : IFileProcessor, IDisposable
    {
        /// <summary>
        /// Gets the ID of this Processor
        /// </summary>
        public string ID { get { return "CoDNOGProcessor"; } }

        /// <summary>
        /// Gets the Name of this Processor
        /// </summary>
        public string Name { get { return "CoD Normal/Gloss/Occlusion (Infinite Warfare/Modern Warfare)"; } }

        /// <summary>
        /// Gets the compute shader that handles computing the image.
        /// </summary>
        public ComputeShader? NOGShader { get; private set; }

        /// <summary>
        /// Gets the constants that can be assigned to the shader.
        /// </summary>
        public ShaderConstantBuffer? Constants { get; private set; }

        /// <summary>
        /// Whether or not to check the directory for "infinite_warfare"/"modern_warfare_4"
        /// </summary>
        public static bool CheckDirectoryName { get { return false; } }

        /// <inheritdoc/>
        public void Initialize(Instance instance)
        {
            NOGShader = new ComputeShader(
                instance.Device, "nog",
                ShaderCompiler.Compile(
                    File.ReadAllText("CoDNOGConverter.hlsl"),
                    ShaderType.ComputShader,
                    null,
                    instance.IncludeHandler));
            Constants = new ShaderConstantBuffer(
                instance.Device,
                "ConverterConstants",
                NOGShader.ShaderBlob);
        }

        /// <summary>
        /// Processes the provided image
        /// </summary>
        /// <param name="file">File to process</param>
        /// <param name="config">Config with settings</param>
        public void Process(string file, FileProcessorConfig config)
        {
            if (NOGShader == null)
                throw new Exception();
            if (Constants == null)
                throw new Exception();

            var ext = config.GetValue("Extension", ".png");
            var outputPath = config.GetValue("OutputPath", "");

            if (string.IsNullOrWhiteSpace(outputPath))
                outputPath = Path.Combine(Path.GetDirectoryName(file)!, Path.GetFileNameWithoutExtension(file).Split(new string[] { "_n&", "_n_" }, StringSplitOptions.None)[0]);

            using var texture = new Texture(NOGShader.Owner, "packed_input", file);

            var w = (int)texture.Metadata.Width;
            var h = (int)texture.Metadata.Width;

            Constants.Set("GlossWidth", new Vector2(0.1f, 0.1f));

            using var gMap = new Texture(NOGShader.Owner, "gloss_output", w, h, ScratchImageFormat.R8G8B8A8UNorm, false);
            using var nMap = new Texture(NOGShader.Owner, "normal_output", w, h, ScratchImageFormat.R8G8B8A8UNorm, false);
            using var oMap = new Texture(NOGShader.Owner, "ao_output", w, h, ScratchImageFormat.R8G8B8A8UNorm, false);

            NOGShader.Dispatch(w, h, 1, Constants, texture, gMap, nMap, oMap);

            gMap.Save(outputPath + "_g" + ext);
            nMap.Save(outputPath + "_n" + ext);
            oMap.Save(outputPath + "_o" + ext);
        }

        /// <inheritdoc/>
        public bool CanProcessFile(string file, FileProcessorConfig config)
        {
            // Check directory names
            if (CheckDirectoryName)
                if (!file.Contains("modern_warfare_4") && !file.Contains("infinite_warfare"))
                    return false;

            // Check for Modern Warfare's String
            if (file.Contains("_n&") || file.Contains("_g~"))
                return true;

            // Check for Infinite Warfare's String
            if (file.Contains("packed_ng") || file.Contains("packed_nog"))
                return true;

            return false;
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return Name;
        }

        /// <inheritdoc/>
        public virtual void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}