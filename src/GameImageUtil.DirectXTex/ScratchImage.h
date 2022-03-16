#pragma once
#include "ScratchImageDimension.h"
#include "ScratchImageException.h"
#include "ScratchImageFileFormat.h"
#include "ScratchImageFlags.h"
#include "ScratchImageFormat.h"
#include "ScratchImageHelper.h"
#include "ScratchImageMetadata.h"

using namespace System;
using namespace System::Drawing;
using namespace System::Runtime::InteropServices;
using namespace System::Numerics;

namespace GameImageUtil
{
	namespace Imaging
	{
		namespace DirectXTex
		{
			public ref class ScratchImage
			{
			private:
				/// <summary>
				/// Native Scratch Image Pointer
				/// </summary>
				DirectX::ScratchImage* ScratchImagePointer;

			public:
				/// <summary>
				/// Gets the <see cref="ScratchImage"/>'s Width
				/// </summary>
				property Int32 Width
				{
					Int32 get()
					{
						return (Int32)ScratchImagePointer->GetMetadata().width;
					}
				}

				/// <summary>
				/// Gets the <see cref="ScratchImage"/>'s Height
				/// </summary>
				property Int32 Height
				{
					Int32 get()
					{
						return (Int32)ScratchImagePointer->GetMetadata().height;
					}
				}

				/// <summary>
				/// Gets the <see cref="ScratchImage"/>'s Depth
				/// </summary>
				property Int32 Depth
				{
					Int32 get()
					{
						return (Int32)ScratchImagePointer->GetMetadata().depth;
					}
				}

				/// <summary>
				/// Gets the <see cref="ScratchImage"/>'s Array Size (for Cubemaps, etc.)
				/// </summary>
				property Int32 Array
				{
					Int32 get()
					{
						return (Int32)ScratchImagePointer->GetMetadata().arraySize;
					}
				}

				/// <summary>
				/// Gets the <see cref="ScratchImage"/>'s Mip Map Count
				/// </summary>
				property Int32 MipLevels
				{
					Int32 get()
					{
						return (Int32)ScratchImagePointer->GetMetadata().mipLevels;
					}
				}

				/// <summary>
				/// Gets the <see cref="ScratchImage"/>'s Format
				/// </summary>
				property ScratchImageFormat Format
				{
					ScratchImageFormat get()
					{
						return (ScratchImageFormat)ScratchImagePointer->GetMetadata().format;
					}
				}

				/// <summary>
				/// Gets the <see cref="ScratchImage"/>'s Metadata
				/// </summary>
				property ScratchImageMetadata^ Metadata
				{
					ScratchImageMetadata^ get()
					{
						auto result = gcnew ScratchImageMetadata();

						result->Width      = (Int32)ScratchImagePointer->GetMetadata().width;
						result->Height     = (Int32)ScratchImagePointer->GetMetadata().height;
						result->Depth      = (Int32)ScratchImagePointer->GetMetadata().depth;
						result->ArraySize  = (Int32)ScratchImagePointer->GetMetadata().arraySize;
						result->MipLevels  = (Int32)ScratchImagePointer->GetMetadata().mipLevels;
						result->MiscFlags  = (ScratchImageFlags)ScratchImagePointer->GetMetadata().miscFlags;
						result->MiscFlags2 = (ScratchImageFlags2)ScratchImagePointer->GetMetadata().miscFlags2;
						result->Format     = (ScratchImageFormat)ScratchImagePointer->GetMetadata().format;
						result->Dimension  = (ScratchImageDimension)ScratchImagePointer->GetMetadata().dimension;

						return result;
					}
				}

				/// <summary>
				/// Destructs the currently loaded <see cref="ScratchImage"/>
				/// </summary>
				void ClearLoadedImage();

				/// <summary>
				/// Initializes an instance of the <see cref="ScratchImage"/> class with the given params
				/// </summary>
				/// <param name="resource">Resource to initialize the image from</param>
				ScratchImage(IntPtr device, IntPtr context, IntPtr resource);

				/// <summary>
				/// Initializes an instance of the <see cref="ScratchImage"/> class with the given params
				/// </summary>
				/// <param name="metaData">Metadata to initialize the image with</param>
				ScratchImage(ScratchImageMetadata^ metaData);

				/// <summary>
				/// Initializes an instance of the <see cref="ScratchImage"/> class with the params and given raw image buffer
				/// </summary>
				/// <param name="metaData">Metadata to initialize the image with</param>
				/// <param name="buffer">Pixel buffer to apply to the image</param>
				ScratchImage(ScratchImageMetadata^ metaData, array<Byte>^ buffer);

				/// <summary>
				/// Initializes an instance of the <see cref="ScratchImage"/> class with the given image path
				/// </summary>
				/// <param name="filePath">Image file path</param>
				ScratchImage(String^ filePath);

				/// <summary>
				/// Initializes an instance of the <see cref="ScratchImage"/> class with the given image path
				/// </summary>
				/// <param name="filePath">Image file path</param>
				/// <param name="format">The <see cref="ScratchImageFileFormat"/>/Type of the Image</param>
				ScratchImage(String^ filePath, ScratchImageFileFormat format);

				/// <summary>
				/// Initializes the <see cref="ScratchImage"/> with the given params
				/// </summary>
				/// <param name="metaData">Metadata to initialize the image with</param>
				void InitializeImage(ScratchImageMetadata^ metaData);

				/// <summary>
				/// Initializes the <see cref="ScratchImage"/> with the params and given raw image buffer
				/// </summary>
				/// <param name="metaData">Metadata to initialize the image with</param>
				/// <param name="buffer">Pixel buffer to apply to the image</param>
				void InitializeImage(ScratchImageMetadata^ metaData, array<Byte>^ buffer);

				/// <summary>
				/// Loads the buffer into the <see cref="ScratchImage"/> from the given image buffer and type
				/// </summary>
				/// <param name="buffer">Image file buffer</param>
				/// <param name="format">The <see cref="ScratchImageFileFormat"/>/Type of the Image</param>
				void Load(array<Byte>^ buffer, ScratchImageFileFormat format);

				/// <summary>
				/// Loads the file into the <see cref="ScratchImage"/> from the given image path
				/// </summary>
				/// <param name="filePath">Image file path</param>
				void Load(String^ filePath);

				/// <summary>
				/// Loads the file into the <see cref="ScratchImage"/> from the given image path and type
				/// </summary>
				/// <param name="filePath">Image file path</param>
				/// <param name="format">The <see cref="ScratchImageFileFormat"/>/Type of the Image</param>
				void Load(String^ filePath, ScratchImageFileFormat format);

				/// <summary>
				/// Saves the <see cref="ScratchImage"/> to the given image path
				/// </summary>
				/// <param name="filePath">Image file path</param>
				void Save(String^ filePath);

				/// <summary>
				/// Saves the <see cref="ScratchImage"/> to the given image path and type
				/// </summary>
				/// <param name="filePath">Image file path</param>
				/// <param name="format">The <see cref="ScratchImageFileFormat"/>/Type of the Image</param>
				void Save(String^ filePath, ScratchImageFileFormat format);

				/// <summary>
				/// Saves the <see cref="ScratchImage"/> to the give stream and type
				/// </summary>
				/// <param name="stream">Stream we are saving to</param>
				/// <param name="format">The <see cref="ScratchImageFileFormat"/>/Type of the Image</param>
				void Save(System::IO::Stream^ stream, ScratchImageFileFormat format);

				/// <summary>
				/// Converts the <see cref="ScratchImage"/> to the given format
				/// </summary>
				/// <param name="format"><see cref="ScratchImageFormat"/> to convert the image to. If set to unkown, the output format will be determined based off the current format.</param>
				void ConvertImage(ScratchImageFormat format);

				/// <summary>
				/// Resizes the <see cref="ScratchImage"/> to the given width and height
				/// </summary>
				/// <param name="width">Image Width</param>
				/// <param name="width">Image Height</param>
				void Resize(int width, int height);

				/// <summary>
				/// Generates Mip Maps for the <see cref="ScratchImage"/>
				/// </summary>
				/// <param name="mipMapCount">Number of Mip Maps to generate</param>
				void GenerateMipMaps(int mipMapCount);

				/// <summary>
				/// Gets the <see cref="ScratchImage"/>'s Pixels of the first mip/slice
				/// </summary>
				IntPtr GetPixelsPointer(Int64% size);

				/// <summary>
				/// Gets the <see cref="ScratchImage"/>'s Pixels at the given mip/slice
				/// </summary>
				/// <param name="mip">Mip Map to convert</param>
				/// <param name="item">Item to convert</param>
				/// <param name="slice">Slice to convert</param>
				IntPtr GetPixelsPointer(int mip, int item, int slice, Int64% size);

				/// <summary>
				/// Gets the <see cref="ScratchImage"/>'s Pixels of the first mip/slice
				/// </summary>
				array<Byte>^ GetPixels();

				/// <summary>
				/// Gets the <see cref="ScratchImage"/>'s Pixels at the given mip/slice
				/// </summary>
				/// <param name="mip">Mip Map to convert</param>
				/// <param name="item">Item to convert</param>
				/// <param name="slice">Slice to convert</param>
				array<Byte>^ GetPixels(int mip, int item, int slice);

				/// <summary>
				/// Gets the <see cref="ScratchImage"/>'s Pixel at the given mip/slice at the given x and y
				/// </summary>
				/// <param name="mip">Mip Map to convert</param>
				/// <param name="item">Item to convert</param>
				/// <param name="slice">Slice to convert</param>
				/// <param name="x">X coordinate</param>
				/// <param name="y">Y coordinate</param>
				Vector4 GetPixelValue(int mip, int item, int slice, int x, int y);

				/// <summary>
				/// Gets the <see cref="ScratchImage"/>'s Pixel at the given mip/slice at the given x and y
				/// </summary>
				/// <param name="mip">Mip Map to convert</param>
				/// <param name="item">Item to convert</param>
				/// <param name="slice">Slice to convert</param>
				/// <param name="x">X coordinate</param>
				/// <param name="y">Y coordinate</param>
				/// <param name="value">Output value</param>
				void GetPixelValue(int mip, int item, int slice, int x, int y, Vector4% value);

				/// <summary>
				/// Sets the <see cref="ScratchImage"/>'s Pixel at the given mip/slice at the given x and y
				/// </summary>
				/// <param name="mip">Mip Map to convert</param>
				/// <param name="item">Item to convert</param>
				/// <param name="slice">Slice to convert</param>
				/// <param name="x">X coordinate</param>
				/// <param name="y">Y coordinate</param>
				/// <param name="value">Value to set</param>
				void SetPixelValue(int mip, int item, int slice, int x, int y, Vector4 value);

				/// <summary>
				/// 
				/// </summary>
				/// <param name="device"></param>
				/// <returns></returns>
				IntPtr CreateShaderResourceView(IntPtr device);

				/// <summary>
				/// Destructs the ScratchImage and deletes the <see cref="ScratchImage"/>
				/// </summary>
				~ScratchImage()
				{
					ClearLoadedImage();
				}
			};
		}
	}
}


