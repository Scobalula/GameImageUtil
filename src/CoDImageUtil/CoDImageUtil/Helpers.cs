using PhilLibX.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoDImageUtil
{
    /// <summary>
    /// Helper methods
    /// </summary>
    internal static class Helpers
    {
        /// <summary>
        /// Clamps the value to the given range
        /// </summary>
        public static T Clamp<T>(T value, T max, T min) where T : IComparable<T>
        {
            return value.CompareTo(min) < 0 ? min : value.CompareTo(max) > 0 ? max : value;
        }

        /// <summary>
        /// Calculates the Z value based off the unit vector's X and Y values
        /// </summary>
        public static byte CalculateBlueValue(int R, int G)
        {
            double X = 2.0 * (R / 255.0000) - 1;
            double Y = 2.0 * (G / 255.0000) - 1;
            double Z = 0.0000000;

            if ((1 - (X * X) - (Y * Y)) > 0)
                Z = Math.Sqrt(1 - (X * X) - (Y * Y));

            return (byte)(Clamp((Z + 1.0) / 2.0, 1.0, 0.0) * 255);
        }

        /// <summary>
        /// Gets the DXGI Format based off the input string
        /// </summary>
        public static ScratchImage.DXGIFormat GetDXGIFormat(string format)
        {
            switch (format)
            {
                case "DXT1": return ScratchImage.DXGIFormat.BC1UNORM;
                case "DXT3": return ScratchImage.DXGIFormat.BC2UNORM;
                case "DXT5": return ScratchImage.DXGIFormat.BC3UNORM;
                case "BC4": return ScratchImage.DXGIFormat.BC4UNORM;
                case "BC5": return ScratchImage.DXGIFormat.BC4UNORM;
            }

            return ScratchImage.DXGIFormat.BC1UNORM;
        }
    }
}
