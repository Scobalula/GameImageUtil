#include "pch.h"
#pragma warning(disable : 4561) // __fastcall' incompatible with the '/clr' option: converting to '__stdcall
#include "DirectXTex.h"
#include "ScratchImage.h"


using namespace GameImageUtil::Imaging::DirectXTex;

/// <summary>
/// Sets custom properties for TIFF Images (for use with LoadFromWICMemory(...))
/// </summary>
/// <param name="props">Property Bag to apply the custom properties to</param>
static void SetCustomPropertiesTIFF(IPropertyBag2* props)
{
	PROPBAG2 options = {};
	VARIANT varValues = {};
	options.pstrName = const_cast<wchar_t*>(L"TiffCompressionMethod");
	varValues.vt = VT_UI1;
	varValues.bVal = 0x00000001; // WICTiffCompressionNone
	(void)props->Write(1, &options, &varValues);
}

/// <summary>
/// Sets custom properties for JPG Images (for use with LoadFromWICMemory(...))
/// </summary>
/// <param name="props">Property Bag to apply the custom properties to</param>
static void SetCustomPropertiesJPG(IPropertyBag2* props)
{
	PROPBAG2 options = {};
	VARIANT varValues = {};
	options.pstrName = const_cast<wchar_t*>(L"ImageQuality");
	varValues.vt = VT_R4;
	varValues.fltVal = 1.f;
	(void)props->Write(1, &options, &varValues);
}

void ScratchImage::ClearLoadedImage()
{
	if (ScratchImagePointer)
		delete ScratchImagePointer;
}

GameImageUtil::Imaging::DirectXTex::ScratchImage::ScratchImage(IntPtr device, IntPtr context, IntPtr resource)
{
	std::unique_ptr<DirectX::ScratchImage> scratchImage(new (std::nothrow) DirectX::ScratchImage);
	if (!scratchImage)
		throw gcnew Exception("Failed to create scratch image");

	ID3D11ShaderResourceView* view = (ID3D11ShaderResourceView*)(void*)resource;
	ID3D11Resource* pResource = nullptr;

	view->GetResource(&pResource);

	HRESULT result = DirectX::CaptureTexture(
		(ID3D11Device*)(void*)device,
		(ID3D11DeviceContext*)(void*)context,
		pResource,
		*scratchImage);

	if (FAILED(result))
	{
		throw gcnew ScratchImageException(String::Format("Failed to generated mip maps, return code: 0x{0:X}", result));
	}

	ScratchImagePointer = scratchImage.release();
}

ScratchImage::ScratchImage(ScratchImageMetadata^ metaData)
{
	InitializeImage(metaData);
}

GameImageUtil::Imaging::DirectXTex::ScratchImage::ScratchImage(ScratchImageMetadata^ metaData, array<Byte>^ buffer)
{
	InitializeImage(metaData, buffer);
}

ScratchImage::ScratchImage(String^ filePath)
{
	Load(filePath);
}

GameImageUtil::Imaging::DirectXTex::ScratchImage::ScratchImage(String^ filePath, ScratchImageFileFormat format)
{
	Load(filePath, format);
}

void ScratchImage::InitializeImage(ScratchImageMetadata^ metaData)
{
	std::unique_ptr<DirectX::ScratchImage> scratchImage(new (std::nothrow) DirectX::ScratchImage);
	if (!scratchImage)
		throw gcnew Exception("Failed to create scratch image");

	DirectX::TexMetadata nativeMetadata =
	{
		(size_t)metaData->Width,
		(size_t)metaData->Height,
		(size_t)metaData->Depth,
		(size_t)metaData->ArraySize,
		(size_t)metaData->MipLevels,
		(uint32_t)metaData->MiscFlags,
		(uint32_t)metaData->MiscFlags2,
		(DXGI_FORMAT)metaData->Format,
		(DirectX::TEX_DIMENSION)metaData->Dimension
	};

	HRESULT result = scratchImage.get()->Initialize(nativeMetadata);
	if (FAILED(result))
		throw gcnew ScratchImageException(String::Format("Failed to initialize DirectX::ScratchImage with the metadata, return code: 0x{0:X}", result));

	delete ScratchImagePointer;
	ScratchImagePointer = scratchImage.release();
}

void ScratchImage::InitializeImage(ScratchImageMetadata^ metaData, array<Byte>^ buffer)
{
	InitializeImage(metaData);

	if (!ScratchImagePointer)
		throw gcnew ScratchImageException(String::Format("Failed to aquire DirectX::ScratchImage, result returned nullptr"));
	if (buffer->Length < (int)ScratchImagePointer->GetPixelsSize())
		throw gcnew ScratchImageException(String::Format("Input buffer size is less than DirectX::ScratchImage->GetPixelsSize()"));

	Marshal::Copy(buffer, 0, IntPtr(ScratchImagePointer->GetPixels()), (int)ScratchImagePointer->GetPixelsSize());
}

void GameImageUtil::Imaging::DirectXTex::ScratchImage::Load(array<Byte>^ buffer, ScratchImageFileFormat format)
{
	HRESULT result;

	std::unique_ptr<uint8_t[]> bufferPtr(new uint8_t[buffer->Length]);
	Marshal::Copy(buffer, 0, IntPtr(bufferPtr.get()), buffer->Length);

	std::unique_ptr<DirectX::ScratchImage> scratchImage(new (std::nothrow) DirectX::ScratchImage);
	if (!scratchImage)
		throw gcnew Exception("Failed to create scratch image");

	switch (format)
	{
	case ScratchImageFileFormat::DDS:
		result = DirectX::LoadFromDDSMemory(
			bufferPtr.get(),
			(size_t)buffer->Length,
			DirectX::DDS_FLAGS::DDS_FLAGS_NONE,
			nullptr,
			*scratchImage);
		break;
	case ScratchImageFileFormat::TIF:
	case ScratchImageFileFormat::JPG:
	case ScratchImageFileFormat::PNG:
	case ScratchImageFileFormat::BMP:
		result = DirectX::LoadFromWICMemory(
			bufferPtr.get(),
			(size_t)buffer->Length,
			DirectX::WIC_FLAGS_NONE,
			nullptr,
			*scratchImage);
		break;
	case ScratchImageFileFormat::TGA:
		result = DirectX::LoadFromTGAMemory(
			bufferPtr.get(),
			(size_t)buffer->Length,
			nullptr,
			*scratchImage);
		break;
	}

	if (FAILED(result))
		throw gcnew Exception(String::Format("Failed to save image, return code: 0x{0:X}", result));

	delete ScratchImagePointer;
	ScratchImagePointer = scratchImage.release();
}

void ScratchImage::Load(String^ filePath)
{
	Load(filePath, InteropUtility::GetImageFormatForExtension(System::IO::Path::GetExtension(filePath)));
}

void ScratchImage::Load(String^ filePath, ScratchImageFileFormat format)
{
	HRESULT result;

	std::wstring filePathStd;
	InteropUtility::ToStdWString(filePath, filePathStd);

	std::unique_ptr<DirectX::ScratchImage> scratchImage(new (std::nothrow) DirectX::ScratchImage);
	if (!scratchImage)
		throw gcnew Exception("Failed to create scratch image");

	switch (format)
	{
	case ScratchImageFileFormat::DDS:
	{
		result = DirectX::LoadFromDDSFile(filePathStd.data(), DirectX::DDS_FLAGS::DDS_FLAGS_NONE, nullptr, *scratchImage);
		break;
	}
	case ScratchImageFileFormat::TIF:
	case ScratchImageFileFormat::JPG:
	case ScratchImageFileFormat::PNG:
	case ScratchImageFileFormat::BMP:
	{
		result = DirectX::LoadFromWICFile(filePathStd.data(), DirectX::WIC_FLAGS_IGNORE_SRGB, nullptr, *scratchImage);
		break;
	}
	case ScratchImageFileFormat::TGA:
	{
		result = DirectX::LoadFromTGAFile(filePathStd.data(), nullptr, *scratchImage);
		break;
	}
	}

	if (FAILED(result))
		throw gcnew ScratchImageException(String::Format("Failed to save image, return code: 0x{0:X}", result));

	delete ScratchImagePointer;
	ScratchImagePointer = scratchImage.release();
}

void ScratchImage::Save(String^ filePath)
{
	Save(filePath, InteropUtility::GetImageFormatForExtension(System::IO::Path::GetExtension(filePath)));
}

void ScratchImage::Save(String^ filePath, ScratchImageFileFormat format)
{
	if (!ScratchImagePointer)
		throw gcnew ScratchImageException(String::Format("Failed to aquire DirectX::ScratchImage, result returned nullptr"));

	HRESULT result;

	std::wstring filePathStd;
	InteropUtility::ToStdWString(filePath, filePathStd);

	
	const DirectX::Image* images = ScratchImagePointer->GetImages();
	auto imageCount = ScratchImagePointer->GetImageCount();

	if (!images || imageCount <= 0)
		throw gcnew ScratchImageException(String::Format("Failed to aquire DirectX::ScratchImage, result returned nullptr or image count was 0"));

	switch (format)
	{
	case ScratchImageFileFormat::DDS:
		result = DirectX::SaveToDDSFile(
			images,
			imageCount,
			ScratchImagePointer->GetMetadata(),
			DirectX::DDS_FLAGS::DDS_FLAGS_NONE,
			filePathStd.data());
		break;
	case ScratchImageFileFormat::TIF:
		result = DirectX::SaveToWICFile(
			*images,
			DirectX::WIC_FLAGS::WIC_FLAGS_FORCE_RGB,
			DirectX::GetWICCodec(DirectX::WIC_CODEC_TIFF),
			filePathStd.data(),
			nullptr,
			SetCustomPropertiesTIFF);
		break;
	case ScratchImageFileFormat::JPG:
		result = DirectX::SaveToWICFile(
			*images,
			DirectX::WIC_FLAGS::WIC_FLAGS_NONE,
			DirectX::GetWICCodec(DirectX::WIC_CODEC_JPEG),
			filePathStd.data(),
			nullptr,
			SetCustomPropertiesJPG);
		break;
	case ScratchImageFileFormat::PNG:
		result = DirectX::SaveToWICFile(
			*images,
			DirectX::WIC_FLAGS::WIC_FLAGS_IGNORE_SRGB,
			DirectX::GetWICCodec(DirectX::WIC_CODEC_PNG),
			filePathStd.data());
		break;
	case ScratchImageFileFormat::BMP:
		result = DirectX::SaveToWICFile(
			*images,
			DirectX::WIC_FLAGS::WIC_FLAGS_NONE,
			DirectX::GetWICCodec(DirectX::WIC_CODEC_BMP), filePathStd.data());
		break;
	case ScratchImageFileFormat::TGA:
		result = DirectX::SaveToTGAFile(*images, filePathStd.data());
		break;
	}

	// Check result
	if (FAILED(result))
		throw gcnew Exception(String::Format("Failed to save image, return code: 0x{0:X}", result));
}

void GameImageUtil::Imaging::DirectXTex::ScratchImage::Save(System::IO::Stream^ stream, ScratchImageFileFormat format)
{
	if (!ScratchImagePointer)
		throw gcnew ScratchImageException(String::Format("Failed to aquire DirectX::ScratchImage, result returned nullptr"));

	HRESULT result;

	const DirectX::Image* images = ScratchImagePointer->GetImages();
	auto imageCount = ScratchImagePointer->GetImageCount();

	if (!images || imageCount <= 0)
		throw gcnew ScratchImageException(String::Format("Failed to aquire DirectX::ScratchImage, result returned nullptr or image count was 0"));

	auto blob = DirectX::Blob();

	switch (format)
	{
	case ScratchImageFileFormat::DDS:
		result = DirectX::SaveToDDSMemory(
			images,
			imageCount,
			ScratchImagePointer->GetMetadata(),
			DirectX::DDS_FLAGS::DDS_FLAGS_NONE,
			blob);
		break;
	case ScratchImageFileFormat::TIF:
		result = DirectX::SaveToWICMemory(
			*images,
			DirectX::WIC_FLAGS::WIC_FLAGS_NONE,
			DirectX::GetWICCodec(DirectX::WIC_CODEC_TIFF),
			blob,
			nullptr,
			SetCustomPropertiesTIFF);
		break;
	case ScratchImageFileFormat::JPG:
		result = DirectX::SaveToWICMemory(
			*images,
			DirectX::WIC_FLAGS::WIC_FLAGS_NONE,
			DirectX::GetWICCodec(DirectX::WIC_CODEC_JPEG),
			blob,
			nullptr,
			SetCustomPropertiesJPG);
		break;
	case ScratchImageFileFormat::PNG:
		result = DirectX::SaveToWICMemory(
			*images,
			DirectX::WIC_FLAGS::WIC_FLAGS_NONE,
			DirectX::GetWICCodec(DirectX::WIC_CODEC_PNG),
			blob);
		break;
	case ScratchImageFileFormat::BMP:
		result = DirectX::SaveToWICMemory(
			*images,
			DirectX::WIC_FLAGS::WIC_FLAGS_NONE,
			DirectX::GetWICCodec(DirectX::WIC_CODEC_BMP),
			blob);
		break;
	case ScratchImageFileFormat::TGA:
		result = DirectX::SaveToTGAMemory(*images, blob);
		break;
	}

	if (FAILED(result))
		throw gcnew Exception(String::Format("Failed to save image, return code: 0x{0:X}", result));

	auto temp = gcnew System::IO::UnmanagedMemoryStream((unsigned char*)blob.GetBufferPointer(), blob.GetBufferSize());
	temp->CopyTo(stream);
}

void ScratchImage::ConvertImage(ScratchImageFormat format)
{
	// Validate it
	if (!ScratchImagePointer)
		throw gcnew ScratchImageException(String::Format("Failed to aquire DirectX::ScratchImage, result returned nullptr"));

	// Results
	HRESULT result;

	// Get Metadata
	auto metaData = ScratchImagePointer->GetMetadata();
	// Check is the image compressed, if not, decompress it
	if (DirectX::IsCompressed(metaData.format) && !DirectX::IsCompressed((DXGI_FORMAT)format))
	{
		// Create new Image
		std::unique_ptr<DirectX::ScratchImage> newImage(new (std::nothrow) DirectX::ScratchImage);
		// Validate it
		if (!newImage)
			throw gcnew Exception("Failed to create new scratch image for image decompression");
		// Decompress the image
		result = DirectX::Decompress(ScratchImagePointer->GetImages(), ScratchImagePointer->GetImageCount(), metaData, (DXGI_FORMAT)format, *newImage);
		// Check result
		if (FAILED(result))
			throw gcnew Exception(String::Format("Failed to decompress image, return code: 0x{0:X}", result));

		delete ScratchImagePointer;
		ScratchImagePointer = newImage.release();

	}
	// Check is the image decompress, if not, compress it if necessary
	else if (!DirectX::IsCompressed(metaData.format) && DirectX::IsCompressed((DXGI_FORMAT)format))
	{
		std::unique_ptr<DirectX::ScratchImage> newImage(new (std::nothrow) DirectX::ScratchImage);
		if (!newImage)
			throw gcnew ScratchImageException("Failed to create new scratch image for image compression");

		result = DirectX::Compress(
			ScratchImagePointer->GetImages(),
			ScratchImagePointer->GetImageCount(),
			metaData,
			(DXGI_FORMAT)format,
			DirectX::TEX_COMPRESS_FLAGS::TEX_COMPRESS_DEFAULT,
			DirectX::TEX_THRESHOLD_DEFAULT,
			*newImage);

		if (FAILED(result))
			throw gcnew Exception(String::Format("Failed to compress image, return code: 0x{0:X}", result));

		delete ScratchImagePointer;
		ScratchImagePointer = newImage.release();
	}
	// Convert it
	else if (metaData.format != (DXGI_FORMAT)format)
	{
		// Create new Image
		std::unique_ptr<DirectX::ScratchImage> newImage(new (std::nothrow) DirectX::ScratchImage);

		if (!newImage)
			throw gcnew ScratchImageException("Failed to create new scratch image for image conversion");

		result = DirectX::Convert(ScratchImagePointer->GetImages(), ScratchImagePointer->GetImageCount(), metaData, (DXGI_FORMAT)format, DirectX::TEX_FILTER_FLAGS::TEX_FILTER_DEFAULT, DirectX::TEX_THRESHOLD_DEFAULT, *newImage);

		if (FAILED(result))
			throw gcnew ScratchImageException(String::Format("Failed to convert image, return code: 0x{0:X}", result));

		delete ScratchImagePointer;
		ScratchImagePointer = newImage.release();
	}
}

void ScratchImage::Resize(int width, int height)
{
	if (!ScratchImagePointer)
		throw gcnew ScratchImageException(String::Format("Failed to aquire DirectX::ScratchImage, result returned nullptr"));

	std::unique_ptr<DirectX::ScratchImage> newImage(new (std::nothrow) DirectX::ScratchImage);
	if (!newImage)
		throw gcnew ScratchImageException("Failed to create new scratch image for the resulting resized image");

	auto result = DirectX::Resize(
		ScratchImagePointer->GetImages(),
		ScratchImagePointer->GetImageCount(),
		ScratchImagePointer->GetMetadata(),
		(size_t)width,
		(size_t)height,
		DirectX::TEX_FILTER_FLAGS::TEX_FILTER_SEPARATE_ALPHA,
		*newImage);
	if (FAILED(result))
		throw gcnew ScratchImageException(String::Format("Failed to resize image, return code: 0x{0:X}", result));

	delete ScratchImagePointer;
	ScratchImagePointer = newImage.release();
}

void GameImageUtil::Imaging::DirectXTex::ScratchImage::GenerateMipMaps(int mipMapCount)
{
	if (!ScratchImagePointer)
		throw gcnew ScratchImageException(String::Format("Failed to aquire DirectX::ScratchImage, result returned nullptr"));

	std::unique_ptr<DirectX::ScratchImage> newImage(new (std::nothrow) DirectX::ScratchImage);
	if (!newImage)
		throw gcnew ScratchImageException("Failed to create new scratch image for mip maps");

	auto result = DirectX::GenerateMipMaps(ScratchImagePointer->GetImages(), ScratchImagePointer->GetImageCount(), ScratchImagePointer->GetMetadata(), DirectX::TEX_FILTER_FLAGS::TEX_FILTER_SEPARATE_ALPHA, (size_t)mipMapCount, *newImage);
	if (FAILED(result))
		throw gcnew ScratchImageException(String::Format("Failed to generated mip maps, return code: 0x{0:X}", result));

	delete ScratchImagePointer;
	ScratchImagePointer = newImage.release();
}

IntPtr GameImageUtil::Imaging::DirectXTex::ScratchImage::GetPixelsPointer(Int64% size)
{
	return GetPixelsPointer(0, 0, 0, size);
}

IntPtr GameImageUtil::Imaging::DirectXTex::ScratchImage::GetPixelsPointer(int mip, int item, int slice, Int64% size)
{
	auto pixels = ScratchImagePointer->GetPixels();
	auto pixelSize = ScratchImagePointer->GetPixelsSize();

	size = (Int64)pixelSize;
	return IntPtr(pixels);
}

array<Byte>^ ScratchImage::GetPixels()
{
	return GetPixels(0, 0, 0);
}

array<Byte>^ ScratchImage::GetPixels(int mip, int item, int slice)
{
	auto pixels = ScratchImagePointer->GetPixels();
	auto pixelSize = ScratchImagePointer->GetPixelsSize();

	auto buffer = gcnew array<Byte>((int)pixelSize);

	Marshal::Copy(IntPtr(pixels), buffer, 0, (int)pixelSize);

	return buffer;
}

Vector4 GameImageUtil::Imaging::DirectXTex::ScratchImage::GetPixelValue(int mip, int item, int slice, int x, int y)
{
	auto result = Vector4::One;
	GetPixelValue(mip, item, slice, x, y, result);
	return result;
}

void GameImageUtil::Imaging::DirectXTex::ScratchImage::GetPixelValue(int mip, int item, int slice, int x, int y, Vector4% value)
{
	if (x > Width)
		throw gcnew ArgumentOutOfRangeException("x", "X must be less than the Width of the Image");
	if (y > Height)
		throw gcnew ArgumentOutOfRangeException("y", "Y must be less than the Height of the Image");
	if (x < 0)
		throw gcnew ArgumentOutOfRangeException("x", "X must be greater than 0");
	if (y < 0)
		throw gcnew ArgumentOutOfRangeException("y", "Y must be greater than 0");

	// Get the first image
	const DirectX::Image* img = ScratchImagePointer->GetImage((size_t)mip, (size_t)item, (size_t)slice);
	uint32_t pixelIndex = 0;

	switch (Format)
	{
	case ScratchImageFormat::R8G8B8A8UNorm:
	{
		pixelIndex = ((y * Width) + x) * 4;
		const uint8_t* pixels = img->pixels;
		value.X = pixels[pixelIndex + 0] / 255.0f;
		value.Y = pixels[pixelIndex + 1] / 255.0f;
		value.Z = pixels[pixelIndex + 2] / 255.0f;
		value.W = pixels[pixelIndex + 3] / 255.0f;
		break;
	}
	case ScratchImageFormat::R16G16B16A16UNorm:
	{
		pixelIndex = ((y * Width) + x) * 4;
		const uint16_t* pixels = (uint16_t*)img->pixels;
		value.X = pixels[pixelIndex + 0] / 65535.0f;
		value.Y = pixels[pixelIndex + 1] / 65535.0f;
		value.Z = pixels[pixelIndex + 2] / 65535.0f;
		value.W = pixels[pixelIndex + 3] / 65535.0f;
		break;
	}
	case ScratchImageFormat::R32G32B32A32Float:
	{
		pixelIndex = ((y * Width) + x) * 4;
		const float* pixels = (float*)img->pixels;
		value.X = pixels[pixelIndex + 0];
		value.Y = pixels[pixelIndex + 1];
		value.Z = pixels[pixelIndex + 2];
		value.W = pixels[pixelIndex + 3];
		break;
	}
	default:
		throw gcnew Exception("Unsupported Image Format for GetPixel(...)");
	}
}

void GameImageUtil::Imaging::DirectXTex::ScratchImage::SetPixelValue(int mip, int item, int slice, int x, int y, Vector4 value)
{
	if (x > Width)
		throw gcnew ArgumentOutOfRangeException("x", "X must be less than the Width of the Image");
	if (y > Height)
		throw gcnew ArgumentOutOfRangeException("y", "Y must be less than the Height of the Image");
	if (x < 0)
		throw gcnew ArgumentOutOfRangeException("x", "X must be greater than 0");
	if (y < 0)
		throw gcnew ArgumentOutOfRangeException("y", "Y must be greater than 0");

	// Get the first image
	const DirectX::Image* img = ScratchImagePointer->GetImage((size_t)mip, (size_t)item, (size_t)slice);
	uint32_t pixelIndex = 0;

	switch (Format)
	{
	case ScratchImageFormat::R8G8B8A8UNorm:
	{
		pixelIndex = ((y * Width) + x) * 4;
		uint8_t* pixels = img->pixels;
		pixels[pixelIndex + 0] = (uint8_t)(Math::Clamp(value.X, 0.0f, 1.0f) * 255.0f);
		pixels[pixelIndex + 1] = (uint8_t)(Math::Clamp(value.Y, 0.0f, 1.0f) * 255.0f);
		pixels[pixelIndex + 2] = (uint8_t)(Math::Clamp(value.Z, 0.0f, 1.0f) * 255.0f);
		pixels[pixelIndex + 3] = (uint8_t)(Math::Clamp(value.W, 0.0f, 1.0f) * 255.0f);
		break;
	}
	case ScratchImageFormat::R16G16B16A16UNorm:
	{
		pixelIndex = ((y * Width) + x) * 4;
		uint16_t* pixels = (uint16_t*)img->pixels;
		pixels[pixelIndex + 0] = (uint16_t)(Math::Clamp(value.X, 0.0f, 1.0f) * 65535.0f);
		pixels[pixelIndex + 1] = (uint16_t)(Math::Clamp(value.Y, 0.0f, 1.0f) * 65535.0f);
		pixels[pixelIndex + 2] = (uint16_t)(Math::Clamp(value.Z, 0.0f, 1.0f) * 65535.0f);
		pixels[pixelIndex + 3] = (uint16_t)(Math::Clamp(value.W, 0.0f, 1.0f) * 65535.0f);
		break;
	}
	case ScratchImageFormat::R32G32B32A32Float:
	{
		pixelIndex = ((y * Width) + x) * 4;
		float* pixels = (float*)img->pixels;
		pixels[pixelIndex + 0] = value.X;
		pixels[pixelIndex + 1] = value.Z;
		pixels[pixelIndex + 2] = value.Y;
		pixels[pixelIndex + 3] = value.W;
		break;
	}
	default:
		throw gcnew Exception("Unsupported Image Format for SetPixel(...)");
	}
}

IntPtr GameImageUtil::Imaging::DirectXTex::ScratchImage::CreateShaderResourceView(IntPtr device)
{
	ID3D11ShaderResourceView* view{};

	HRESULT result = DirectX::CreateShaderResourceView(
		(ID3D11Device*)(void*)device,
		ScratchImagePointer->GetImages(),
		ScratchImagePointer->GetImageCount(),
		ScratchImagePointer->GetMetadata(),
		&view);

	if (FAILED(result))
	{
		throw gcnew ScratchImageException(String::Format("Failed to generated mip maps, return code: 0x{0:X}", result));
	}

	return (IntPtr)view;
}
