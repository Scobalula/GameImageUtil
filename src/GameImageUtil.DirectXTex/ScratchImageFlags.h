// ------------------------------------------------------------------------
// GameImageUtil - My Utility Library
// Copyright(c) 2018 Philip/Scobalula
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// ------------------------------------------------------------------------
// File: ScratchImageFileFormat.h
// Author: Philip/Scobalula
// Description: Image File Format
#pragma once

using namespace System;

namespace GameImageUtil
{
	namespace Imaging
	{
		namespace DirectXTex
		{
			/// <summary>
			/// Misc. Texture Flags
			/// </summary>
			public enum class ScratchImageFlags : UInt32
			{
				None = 0,
				TextureCube = 0x4,
			};

			/// <summary>
			/// Misc. Texture Flags
			/// </summary>
			public enum class ScratchImageFlags2 : UInt32
			{
				NONE = 0,
				TexMisc2AlphaModeMask = 0x7,
			};
		}
	}
}