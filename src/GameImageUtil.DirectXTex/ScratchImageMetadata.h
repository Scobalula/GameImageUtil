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
// File: DirectXException.h
// Author: Philip/Scobalula
// Description: A class that holds info about a ScratchImage
#pragma once
#include "ScratchImageDimension.h"
#include "ScratchImageFlags.h"
#include "ScratchImageFormat.h"

using namespace System;

namespace GameImageUtil
{
	namespace Imaging
	{
		namespace DirectXTex
		{
			/// <summary>
			/// Texture Metadata
			/// </summary>
			public ref class ScratchImageMetadata
			{
			public:
				/// <summary>
				/// Gets or Sets the width of the image
				/// </summary>
				property UInt64 Width;

				/// <summary>
				/// Gets or Sets the height of the image
				/// </summary>
				property UInt64 Height;

				/// <summary>
				/// Gets or Sets the Depth of the image
				/// </summary>
				property UInt64 Depth;

				/// <summary>
				/// Gets or Sets the number of images
				/// </summary>
				property UInt64 ArraySize;

				/// <summary>
				/// Gets or Sets the number of mip maps the image has
				/// </summary>
				property UInt64 MipLevels;

				/// <summary>
				/// Gets or Sets the misc flags
				/// </summary>
				property ScratchImageFlags MiscFlags;

				/// <summary>
				/// Gets or Sets the extended misc flags
				/// </summary>
				property ScratchImageFlags2 MiscFlags2;

				/// <summary>
				/// Gets or Sets the image format
				/// </summary>
				property ScratchImageFormat Format;

				/// <summary>
				/// Gets or Sets the texture dimension
				/// </summary>
				property ScratchImageDimension Dimension;
			};
		}
	}
}