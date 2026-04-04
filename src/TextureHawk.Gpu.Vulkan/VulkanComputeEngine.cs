using System.Runtime.InteropServices;
using RedFox.Graphics2D;
using Silk.NET.Vulkan;

using Image = RedFox.Graphics2D.Image;

namespace TextureHawk.Gpu.Vulkan;

/// <summary>
/// Vulkan-based GPU compute engine for accelerating image processing.
/// </summary>
/// <remarks>
/// Creates a Vulkan instance and logical device with a compute queue family.
/// Supports runtime GPU enumeration and selection, compute shader dispatching
/// via SPIR-V modules, and buffer-based image upload/download.
/// Falls back gracefully when Vulkan is not available on the system.
/// </remarks>
public sealed class VulkanComputeEngine : IComputeEngine, IDisposable
{
    private readonly object _deviceSync = new();
    private readonly Vk _vk;
    private RenderDocCapture? _renderDocCapture;
    private Instance _instance;
    private PhysicalDevice _physicalDevice;
    private Device _device;
    private Queue _computeQueue;
    private CommandPool _commandPool;
    private uint _computeQueueFamilyIndex;
    private bool _deviceInitialized;
    private bool _supportsFormatlessStorageImageWrites;
    private bool _disposed;
    private List<GpuDevice>? _cachedDevices;

    /// <inheritdoc />
    public bool IsAvailable { get; private set; }

    /// <inheritdoc />
    public string Name => "Vulkan";

    /// <inheritdoc />
    public GpuDevice? SelectedDevice { get; private set; }

    public VulkanComputeEngine()
    {
        _vk = Vk.GetApi();
        try
        {
            CreateInstance();
            IsAvailable = true;

            // Auto-select the best discrete GPU by default
            var devices = EnumerateDevices();
            var preferred = devices.FirstOrDefault(d => d.DeviceType == GpuDeviceType.DiscreteGpu);
            if (preferred != default)
                SelectDevice(preferred);
            else if (devices.Count > 0)
                SelectDevice(devices[0]);
        }
        catch
        {
            IsAvailable = false;
        }
    }

    /// <summary>
    /// Enumerates all Vulkan-capable physical devices.
    /// </summary>
    public IReadOnlyList<GpuDevice> EnumerateDevices()
    {
        if (_cachedDevices is not null)
            return _cachedDevices;

        _cachedDevices = [];

        if (!IsAvailable)
            return _cachedDevices;

        unsafe
        {
            uint deviceCount = 0;
            _vk.EnumeratePhysicalDevices(_instance, ref deviceCount, null);
            if (deviceCount == 0)
                return _cachedDevices;

            var physicalDevices = new PhysicalDevice[deviceCount];
            fixed (PhysicalDevice* pDevices = physicalDevices)
                _vk.EnumeratePhysicalDevices(_instance, ref deviceCount, pDevices);

            foreach (var physDevice in physicalDevices)
            {
                _vk.GetPhysicalDeviceProperties(physDevice, out var props);
                _vk.GetPhysicalDeviceMemoryProperties(physDevice, out var memProps);

                ulong totalMemory = 0;
                for (var i = 0; i < memProps.MemoryHeapCount; i++)
                {
                    var heap = memProps.MemoryHeaps[i];
                    if ((heap.Flags & MemoryHeapFlags.DeviceLocalBit) != 0)
                        totalMemory += heap.Size;
                }

                var deviceType = props.DeviceType switch
                {
                    PhysicalDeviceType.IntegratedGpu => GpuDeviceType.IntegratedGpu,
                    PhysicalDeviceType.DiscreteGpu => GpuDeviceType.DiscreteGpu,
                    PhysicalDeviceType.VirtualGpu => GpuDeviceType.VirtualGpu,
                    PhysicalDeviceType.Cpu => GpuDeviceType.Cpu,
                    _ => GpuDeviceType.Other
                };

                var nameSpan = new ReadOnlySpan<byte>(props.DeviceName, 256);
                var nullIdx = nameSpan.IndexOf((byte)0);
                var name = System.Text.Encoding.UTF8.GetString(nameSpan[..(nullIdx >= 0 ? nullIdx : 256)]);

                _cachedDevices.Add(new GpuDevice(name, props.DeviceID, props.VendorID, deviceType, totalMemory));
            }
        }

        return _cachedDevices;
    }

    /// <summary>
    /// Selects a specific GPU device by matching <see cref="GpuDevice.DeviceId"/> and <see cref="GpuDevice.VendorId"/>.
    /// </summary>
    public bool SelectDevice(GpuDevice device)
    {
        lock (_deviceSync)
        {
            if (!IsAvailable)
                return false;

            // Ensure in-flight work has retired before replacing the logical device.
            if (_deviceInitialized)
            {
                unsafe
                {
                    _vk.QueueWaitIdle(_computeQueue);
                }

                DestroyDevice();
            }

            unsafe
            {
                uint deviceCount = 0;
                _vk.EnumeratePhysicalDevices(_instance, ref deviceCount, null);
                var physicalDevices = new PhysicalDevice[deviceCount];
                fixed (PhysicalDevice* pDevices = physicalDevices)
                    _vk.EnumeratePhysicalDevices(_instance, ref deviceCount, pDevices);

                foreach (var physDevice in physicalDevices)
                {
                    _vk.GetPhysicalDeviceProperties(physDevice, out var props);
                    if (props.DeviceID == device.DeviceId && props.VendorID == device.VendorId)
                    {
                        _physicalDevice = physDevice;

                        if (!FindComputeQueueFamily(out _computeQueueFamilyIndex))
                            return false;

                        CreateLogicalDevice();
                        CreateCommandPool();
                        SelectedDevice = device;
                        _deviceInitialized = true;
                        return true;
                    }
                }
            }

            return false;
        }
    }

    /// <inheritdoc />
    public bool CaptureStart(string captureName)
    {
        lock (_deviceSync)
        {
            _renderDocCapture ??= RenderDocCapture.TryCreate();
            return _renderDocCapture?.TryStart(captureName) == true;
        }
    }

    /// <inheritdoc />
    public void CaptureEnd()
    {
        lock (_deviceSync)
        {
            _renderDocCapture?.End();
        }
    }

    /// <summary>
    /// Dispatches a compute shader on the selected GPU.
    /// </summary>
    public unsafe ValueTask<Image> DispatchAsync(ShaderDefinition shader, IReadOnlyList<Image> inputs, IReadOnlyDictionary<string, object> parameters, int outputWidth, int outputHeight, ImageFormat outputFormat, CancellationToken cancellationToken)
    {
        lock (_deviceSync)
        {
            if (!_deviceInitialized)
                throw new InvalidOperationException("No GPU device selected. Call SelectDevice first.");

            if (!_supportsFormatlessStorageImageWrites)
                throw new InvalidOperationException("The selected Vulkan device does not support formatless storage-image writes.");

            if (!File.Exists(shader.ShaderPath))
                throw new FileNotFoundException($"SPIR-V shader not found: {shader.ShaderPath}");

            var shaderBytecode = File.ReadAllBytes(shader.ShaderPath);
            var pushConstants = BuildPushConstants(shader, parameters, outputWidth, outputHeight, inputs);
            return DispatchAsync(shaderBytecode, inputs, pushConstants, outputWidth, outputHeight, outputFormat, shader.WorkGroupSize, cancellationToken);
        }
    }

    private static byte[] BuildPushConstants(ShaderDefinition shader, IReadOnlyDictionary<string, object> parameters, int outputWidth, int outputHeight, IReadOnlyList<Image> inputs)
    {
        if (shader.Parameters.Count == 0)
            return [];

        var size = shader.Parameters.Max(parameter => parameter.Offset + parameter.Size);
        var data = new byte[size];

        foreach (var parameter in shader.Parameters)
        {
            if (!TryResolveBuiltInPushConstant(parameter.Name, outputWidth, outputHeight, inputs, out var builtInValue)
                && !parameters.TryGetValue(parameter.Name, out builtInValue))
            {
                continue;
            }

            WritePushConstantValue(data.AsSpan(parameter.Offset), parameter.Type, builtInValue);
        }

        return data;
    }

    private static bool TryResolveBuiltInPushConstant(string name, int outputWidth, int outputHeight, IReadOnlyList<Image> inputs, out object value)
    {
        switch (name)
        {
            case "outWidth":
                value = outputWidth;
                return true;
            case "outHeight":
                value = outputHeight;
                return true;
            case "srcWidth" when inputs.Count > 0:
                value = inputs[0].Width;
                return true;
            case "srcHeight" when inputs.Count > 0:
                value = inputs[0].Height;
                return true;
            default:
                value = null!;
                return false;
        }
    }

    private static void WritePushConstantValue(Span<byte> destination, ShaderParameterType type, object value)
    {
        switch (type)
        {
            case ShaderParameterType.Float:
                var floatValue = Convert.ToSingle(value);
                MemoryMarshal.Write(destination, in floatValue);
                break;
            case ShaderParameterType.UInt:
                var uintValue = Convert.ToUInt32(value);
                MemoryMarshal.Write(destination, in uintValue);
                break;
            default:
                var intValue = Convert.ToInt32(value);
                MemoryMarshal.Write(destination, in intValue);
                break;
        }
    }

    /// <summary>
    /// Dispatches a compute shader from raw shader bytecode with explicit push constants.
    /// </summary>
    public unsafe ValueTask<Image> DispatchAsync(byte[] shaderBytecode, IReadOnlyList<Image> inputs, byte[] pushConstants, int outputWidth, int outputHeight, ImageFormat outputFormat, (int X, int Y, int Z) workGroupSize, CancellationToken cancellationToken)
    {
        lock (_deviceSync)
        {
            if (!_deviceInitialized)
                throw new InvalidOperationException("No GPU device selected. Call SelectDevice first.");

            if (!_supportsFormatlessStorageImageWrites)
                throw new InvalidOperationException("The selected Vulkan device does not support formatless storage-image writes.");

            if (workGroupSize == default)
                workGroupSize = (16, 16, 1);

        // Create output image
        var output = new Image(outputWidth, outputHeight, 1, 1, 1, outputFormat, false);

        // Create VkImage inputs with correct format for hardware sampling
        var inputImages = CreateInputImages(inputs);

        var outputSlice = output.Slices[0];
        var outputImage = CreateOutputImage(outputWidth, outputHeight, outputFormat, (ulong)outputSlice.Pixels.Span.Length);

        // Create shader module, pipeline, descriptors
        var shaderModule = CreateShaderModule(shaderBytecode);
        var (descriptorSetLayout, pipelineLayout, pipeline) = CreateComputePipeline(shaderModule, inputImages.Count, 1, pushConstants.Length);
        var descriptorPool = CreateDescriptorPool(inputImages.Count, 1);
        var descriptorSet = AllocateDescriptorSet(descriptorPool, descriptorSetLayout);

        WriteDescriptors(descriptorSet, inputImages, outputImage);

        // Record and submit command buffer
        var cmdBuffer = AllocateCommandBuffer();
        BeginCommandBuffer(cmdBuffer);

        // Transfer input images from staging buffers
        foreach (var img in inputImages)
        {
            TransitionImageLayout(cmdBuffer, img.Image, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, img.LayerCount);
            RecordBufferToImageCopy(cmdBuffer, img.StagingBuffer, img.Image, img.Width, img.Height, img.LayerCount, img.BytesPerLayer);
            TransitionImageLayout(cmdBuffer, img.Image, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, img.LayerCount);
        }

        TransitionImageLayout(cmdBuffer, outputImage.Image, ImageLayout.Undefined, ImageLayout.General);

        _vk.CmdBindPipeline(cmdBuffer, PipelineBindPoint.Compute, pipeline);
        _vk.CmdBindDescriptorSets(cmdBuffer, PipelineBindPoint.Compute, pipelineLayout, 0, 1, &descriptorSet, 0, null);

        if (pushConstants.Length > 0)
        {
            fixed (byte* pPushData = pushConstants)
                _vk.CmdPushConstants(cmdBuffer, pipelineLayout, ShaderStageFlags.ComputeBit, 0, (uint)pushConstants.Length, pPushData);
        }

        var groupCountX = (uint)((outputWidth + workGroupSize.X - 1) / workGroupSize.X);
        var groupCountY = (uint)((outputHeight + workGroupSize.Y - 1) / workGroupSize.Y);
        _vk.CmdDispatch(cmdBuffer, groupCountX, groupCountY, 1);

        TransitionImageLayout(cmdBuffer, outputImage.Image, ImageLayout.General, ImageLayout.TransferSrcOptimal);
        RecordImageToBufferCopy(cmdBuffer, outputImage.Image, outputImage.StagingBuffer.Handle, outputWidth, outputHeight);

        EndAndSubmitCommandBuffer(cmdBuffer);

        // Read back output
        DownloadFromBuffer(outputImage.StagingMemory, outputSlice.Pixels.Span, outputImage.StagingSize);

        // Cleanup
        _vk.DestroyDescriptorPool(_device, descriptorPool, null);
        _vk.DestroyPipeline(_device, pipeline, null);
        _vk.DestroyPipelineLayout(_device, pipelineLayout, null);
        _vk.DestroyDescriptorSetLayout(_device, descriptorSetLayout, null);
        _vk.DestroyShaderModule(_device, shaderModule, null);
        CleanupOutputImage(outputImage);
        CleanupInputImages(inputImages);

            return ValueTask.FromResult(output);
        }
    }

    /// <summary>
    /// Dispatches a compute shader that produces multiple output images.
    /// Each output gets its own storage buffer at consecutive descriptor bindings.
    /// </summary>
    public unsafe ValueTask<IReadOnlyList<Image>> DispatchMultiAsync(byte[] shaderBytecode, IReadOnlyList<Image> inputs, byte[] pushConstants, IReadOnlyList<(int Width, int Height, ImageFormat Format)> outputs, (int X, int Y, int Z) workGroupSize, CancellationToken cancellationToken)
    {
        lock (_deviceSync)
        {
            if (!_deviceInitialized)
                throw new InvalidOperationException("No GPU device selected. Call SelectDevice first.");

            if (!_supportsFormatlessStorageImageWrites)
                throw new InvalidOperationException("The selected Vulkan device does not support formatless storage-image writes.");

            if (outputs.Count == 0)
                throw new ArgumentException("At least one output specification is required.", nameof(outputs));

            if (workGroupSize == default)
                workGroupSize = (16, 16, 1);

        // Create output images and their staging resources
        var outputImages = new Image[outputs.Count];
        var outputResources = new VkOutputImage[outputs.Count];

        for (var i = 0; i < outputs.Count; i++)
        {
            var (w, h, fmt) = outputs[i];
            outputImages[i] = new Image(w, h, 1, 1, 1, fmt, false);
            outputResources[i] = CreateOutputImage(w, h, fmt, (ulong)outputImages[i].Slices[0].Pixels.Span.Length);
        }

        // Create VkImage inputs
        var inputImages = CreateInputImages(inputs);

        // Create shader module, pipeline, descriptors
        var shaderModule = CreateShaderModule(shaderBytecode);
        var (descriptorSetLayout, pipelineLayout, pipeline) = CreateComputePipeline(shaderModule, inputImages.Count, outputs.Count, pushConstants.Length);
        var descriptorPool = CreateDescriptorPool(inputImages.Count, outputs.Count);
        var descriptorSet = AllocateDescriptorSet(descriptorPool, descriptorSetLayout);

        WriteMultiOutputDescriptors(descriptorSet, inputImages, outputResources);

        // Record and submit command buffer
        var cmdBuffer = AllocateCommandBuffer();
        BeginCommandBuffer(cmdBuffer);

        foreach (var img in inputImages)
        {
            TransitionImageLayout(cmdBuffer, img.Image, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, img.LayerCount);
            RecordBufferToImageCopy(cmdBuffer, img.StagingBuffer, img.Image, img.Width, img.Height, img.LayerCount, img.BytesPerLayer);
            TransitionImageLayout(cmdBuffer, img.Image, ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, img.LayerCount);
        }

        for (var i = 0; i < outputResources.Length; i++)
            TransitionImageLayout(cmdBuffer, outputResources[i].Image, ImageLayout.Undefined, ImageLayout.General);

        _vk.CmdBindPipeline(cmdBuffer, PipelineBindPoint.Compute, pipeline);
        _vk.CmdBindDescriptorSets(cmdBuffer, PipelineBindPoint.Compute, pipelineLayout, 0, 1, &descriptorSet, 0, null);

        if (pushConstants.Length > 0)
        {
            fixed (byte* pPushData = pushConstants)
                _vk.CmdPushConstants(cmdBuffer, pipelineLayout, ShaderStageFlags.ComputeBit, 0, (uint)pushConstants.Length, pPushData);
        }

        // Dispatch based on the largest output dimensions
        var maxW = outputs.Max(o => o.Width);
        var maxH = outputs.Max(o => o.Height);
        var groupCountX = (uint)((maxW + workGroupSize.X - 1) / workGroupSize.X);
        var groupCountY = (uint)((maxH + workGroupSize.Y - 1) / workGroupSize.Y);
        _vk.CmdDispatch(cmdBuffer, groupCountX, groupCountY, 1);

        for (var i = 0; i < outputResources.Length; i++)
        {
            TransitionImageLayout(cmdBuffer, outputResources[i].Image, ImageLayout.General, ImageLayout.TransferSrcOptimal);
            RecordImageToBufferCopy(cmdBuffer, outputResources[i].Image, outputResources[i].StagingBuffer.Handle, outputResources[i].Width, outputResources[i].Height);
        }

        EndAndSubmitCommandBuffer(cmdBuffer);

        // Read back all outputs
        for (var i = 0; i < outputs.Count; i++)
            DownloadFromBuffer(outputResources[i].StagingMemory, outputImages[i].Slices[0].Pixels.Span, outputResources[i].StagingSize);

        // Cleanup
        _vk.DestroyDescriptorPool(_device, descriptorPool, null);
        _vk.DestroyPipeline(_device, pipeline, null);
        _vk.DestroyPipelineLayout(_device, pipelineLayout, null);
        _vk.DestroyDescriptorSetLayout(_device, descriptorSetLayout, null);
        _vk.DestroyShaderModule(_device, shaderModule, null);
        for (var i = 0; i < outputs.Count; i++)
            CleanupOutputImage(outputResources[i]);
        CleanupInputImages(inputImages);

            return ValueTask.FromResult<IReadOnlyList<Image>>(outputImages);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_deviceSync)
        {
            if (_disposed) return;
            _disposed = true;

            if (_deviceInitialized)
                DestroyDevice();

            unsafe
            {
                if (_instance.Handle != 0)
                    _vk.DestroyInstance(_instance, null);
            }

            _vk.Dispose();
        }
    }

    private unsafe void CreateInstance()
    {
        var appInfo = new ApplicationInfo
        {
            SType = StructureType.ApplicationInfo,
            ApiVersion = Vk.Version12
        };

        var createInfo = new InstanceCreateInfo
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo
        };

        var result = _vk.CreateInstance(in createInfo, null, out _instance);
        if (result != Result.Success)
            throw new InvalidOperationException($"Failed to create Vulkan instance: {result}");
    }

    private unsafe bool FindComputeQueueFamily(out uint familyIndex)
    {
        familyIndex = 0;
        uint count = 0;
        _vk.GetPhysicalDeviceQueueFamilyProperties(_physicalDevice, ref count, null);
        var families = new QueueFamilyProperties[count];
        fixed (QueueFamilyProperties* pFamilies = families)
            _vk.GetPhysicalDeviceQueueFamilyProperties(_physicalDevice, ref count, pFamilies);

        for (uint i = 0; i < count; i++)
        {
            if ((families[i].QueueFlags & QueueFlags.ComputeBit) != 0)
            {
                familyIndex = i;
                return true;
            }
        }
        return false;
    }

    private unsafe void CreateLogicalDevice()
    {
        _vk.GetPhysicalDeviceFeatures(_physicalDevice, out var supportedFeatures);
        _supportsFormatlessStorageImageWrites = supportedFeatures.ShaderStorageImageWriteWithoutFormat;

        var enabledFeatures = new PhysicalDeviceFeatures();
        if (_supportsFormatlessStorageImageWrites)
            enabledFeatures.ShaderStorageImageWriteWithoutFormat = true;

        var queuePriority = 1.0f;
        var queueCreateInfo = new DeviceQueueCreateInfo
        {
            SType = StructureType.DeviceQueueCreateInfo,
            QueueFamilyIndex = _computeQueueFamilyIndex,
            QueueCount = 1,
            PQueuePriorities = &queuePriority
        };

        var deviceCreateInfo = new DeviceCreateInfo
        {
            SType = StructureType.DeviceCreateInfo,
            QueueCreateInfoCount = 1,
            PQueueCreateInfos = &queueCreateInfo,
            PEnabledFeatures = &enabledFeatures
        };

        var result = _vk.CreateDevice(_physicalDevice, in deviceCreateInfo, null, out _device);
        if (result != Result.Success)
            throw new InvalidOperationException($"Failed to create Vulkan device: {result}");

        _vk.GetDeviceQueue(_device, _computeQueueFamilyIndex, 0, out _computeQueue);
    }

    private unsafe void CreateCommandPool()
    {
        var poolInfo = new CommandPoolCreateInfo
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = _computeQueueFamilyIndex,
            Flags = CommandPoolCreateFlags.ResetCommandBufferBit
        };

        var result = _vk.CreateCommandPool(_device, in poolInfo, null, out _commandPool);
        if (result != Result.Success)
            throw new InvalidOperationException($"Failed to create command pool: {result}");
    }

    private unsafe (VkBuffer Buffer, DeviceMemory Memory) CreateBuffer(ulong size, BufferUsageFlags usage, MemoryPropertyFlags memProperties)
    {
        var bufferInfo = new BufferCreateInfo
        {
            SType = StructureType.BufferCreateInfo,
            Size = size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive
        };

        _vk.CreateBuffer(_device, in bufferInfo, null, out var buffer);

        _vk.GetBufferMemoryRequirements(_device, buffer, out var memReqs);
        var memTypeIndex = FindMemoryType(memReqs.MemoryTypeBits, memProperties);

        var allocInfo = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memReqs.Size,
            MemoryTypeIndex = memTypeIndex
        };

        _vk.AllocateMemory(_device, in allocInfo, null, out var memory);
        _vk.BindBufferMemory(_device, buffer, memory, 0);

        return (new VkBuffer(buffer), memory);
    }

    private unsafe uint FindMemoryType(uint typeFilter, MemoryPropertyFlags properties)
    {
        _vk.GetPhysicalDeviceMemoryProperties(_physicalDevice, out var memProperties);
        for (uint i = 0; i < memProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1u << (int)i)) != 0 && (memProperties.MemoryTypes[(int)i].PropertyFlags & properties) == properties)
                return i;
        }
        throw new InvalidOperationException("Failed to find suitable memory type.");
    }

    private unsafe void UploadToBuffer(DeviceMemory memory, ReadOnlySpan<byte> data, ulong size)
    {
        void* mapped;
        _vk.MapMemory(_device, memory, 0, size, 0, &mapped);
        data.CopyTo(new Span<byte>(mapped, data.Length));
        _vk.UnmapMemory(_device, memory);
    }

    private unsafe void DownloadFromBuffer(DeviceMemory memory, Span<byte> destination, ulong size)
    {
        void* mapped;
        _vk.MapMemory(_device, memory, 0, size, 0, &mapped);
        new ReadOnlySpan<byte>(mapped, destination.Length).CopyTo(destination);
        _vk.UnmapMemory(_device, memory);
    }

    private unsafe ShaderModule CreateShaderModule(byte[] shaderBytecode)
    {
        fixed (byte* pCode = shaderBytecode)
        {
            var createInfo = new ShaderModuleCreateInfo
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)shaderBytecode.Length,
                PCode = (uint*)pCode
            };

            _vk.CreateShaderModule(_device, in createInfo, null, out var module);
            return module;
        }
    }

    private unsafe (DescriptorSetLayout Layout, PipelineLayout PipelineLayout, Pipeline Pipeline) CreateComputePipeline(ShaderModule shaderModule, int samplerCount, int storageImageCount, int pushConstantSize)
    {
        var totalBindings = samplerCount + storageImageCount;
        var bindings = new DescriptorSetLayoutBinding[totalBindings];

        // Combined image sampler bindings (inputs)
        for (var i = 0; i < samplerCount; i++)
        {
            bindings[i] = new DescriptorSetLayoutBinding
            {
                Binding = (uint)i,
                DescriptorType = DescriptorType.CombinedImageSampler,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.ComputeBit
            };
        }

        // Storage image bindings (output)
        for (var i = 0; i < storageImageCount; i++)
        {
            bindings[samplerCount + i] = new DescriptorSetLayoutBinding
            {
                Binding = (uint)(samplerCount + i),
                DescriptorType = DescriptorType.StorageImage,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.ComputeBit
            };
        }

        DescriptorSetLayout descriptorSetLayout;
        fixed (DescriptorSetLayoutBinding* pBindings = bindings)
        {
            var layoutInfo = new DescriptorSetLayoutCreateInfo
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = (uint)totalBindings,
                PBindings = pBindings
            };
            _vk.CreateDescriptorSetLayout(_device, in layoutInfo, null, out descriptorSetLayout);
        }

        // Pipeline layout with push constants
        var pushConstantRange = new PushConstantRange
        {
            StageFlags = ShaderStageFlags.ComputeBit,
            Offset = 0,
            Size = (uint)pushConstantSize
        };

        PipelineLayout pipelineLayout;
        {
            var pipelineLayoutInfo = new PipelineLayoutCreateInfo
            {
                SType = StructureType.PipelineLayoutCreateInfo,
                SetLayoutCount = 1,
                PSetLayouts = &descriptorSetLayout,
                PushConstantRangeCount = pushConstantSize > 0 ? 1u : 0u,
                PPushConstantRanges = pushConstantSize > 0 ? &pushConstantRange : null
            };
            _vk.CreatePipelineLayout(_device, in pipelineLayoutInfo, null, out pipelineLayout);
        }

        // Compute pipeline
        var entryPoint = "main"u8;
        Pipeline pipeline;
        fixed (byte* pName = entryPoint)
        {
            var stageInfo = new PipelineShaderStageCreateInfo
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                Stage = ShaderStageFlags.ComputeBit,
                Module = shaderModule,
                PName = pName
            };

            var pipelineInfo = new ComputePipelineCreateInfo
            {
                SType = StructureType.ComputePipelineCreateInfo,
                Stage = stageInfo,
                Layout = pipelineLayout
            };

            _vk.CreateComputePipelines(_device, default, 1, in pipelineInfo, null, out pipeline);
        }

        return (descriptorSetLayout, pipelineLayout, pipeline);
    }

    private unsafe DescriptorPool CreateDescriptorPool(int samplerCount, int storageImageCount)
    {
        var poolSizes = new DescriptorPoolSize[2];
        uint poolSizeCount = 0;

        if (samplerCount > 0)
            poolSizes[poolSizeCount++] = new DescriptorPoolSize { Type = DescriptorType.CombinedImageSampler, DescriptorCount = (uint)samplerCount };
        if (storageImageCount > 0)
            poolSizes[poolSizeCount++] = new DescriptorPoolSize { Type = DescriptorType.StorageImage, DescriptorCount = (uint)storageImageCount };

        fixed (DescriptorPoolSize* pPoolSizes = poolSizes)
        {
            var poolInfo = new DescriptorPoolCreateInfo
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                MaxSets = 1,
                PoolSizeCount = poolSizeCount,
                PPoolSizes = pPoolSizes
            };

            _vk.CreateDescriptorPool(_device, in poolInfo, null, out var pool);
            return pool;
        }
    }

    private unsafe DescriptorSet AllocateDescriptorSet(DescriptorPool pool, DescriptorSetLayout layout)
    {
        var allocInfo = new DescriptorSetAllocateInfo
        {
            SType = StructureType.DescriptorSetAllocateInfo,
            DescriptorPool = pool,
            DescriptorSetCount = 1,
            PSetLayouts = &layout
        };

        _vk.AllocateDescriptorSets(_device, in allocInfo, out var descriptorSet);
        return descriptorSet;
    }

    private unsafe void WriteDescriptors(DescriptorSet descriptorSet, List<VkInputImage> inputImages, VkOutputImage outputImage)
    {
        var totalWrites = inputImages.Count + 1;
        var writes = new WriteDescriptorSet[totalWrites];
        var imageInfos = new DescriptorImageInfo[Math.Max(inputImages.Count, 1)];
        var outputInfos = new DescriptorImageInfo[1];
        outputInfos[0] = new DescriptorImageInfo { ImageView = outputImage.View, ImageLayout = ImageLayout.General };

        for (var i = 0; i < inputImages.Count; i++)
        {
            imageInfos[i] = new DescriptorImageInfo
            {
                Sampler = inputImages[i].Sampler,
                ImageView = inputImages[i].View,
                ImageLayout = ImageLayout.ShaderReadOnlyOptimal
            };
        }

        fixed (DescriptorImageInfo* pImageInfos = imageInfos)
        fixed (DescriptorImageInfo* pOutputInfos = outputInfos)
        {
            for (var i = 0; i < inputImages.Count; i++)
            {
                writes[i] = new WriteDescriptorSet
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = descriptorSet,
                    DstBinding = (uint)i,
                    DescriptorCount = 1,
                    DescriptorType = DescriptorType.CombinedImageSampler,
                    PImageInfo = &pImageInfos[i]
                };
            }

            writes[inputImages.Count] = new WriteDescriptorSet
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = descriptorSet,
                DstBinding = (uint)inputImages.Count,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.StorageImage,
                PImageInfo = pOutputInfos
            };

            fixed (WriteDescriptorSet* pWrites = writes)
                _vk.UpdateDescriptorSets(_device, (uint)totalWrites, pWrites, 0, null);
        }
    }

    private unsafe void WriteMultiOutputDescriptors(DescriptorSet descriptorSet, List<VkInputImage> inputImages, VkOutputImage[] outputImages)
    {
        var totalWrites = inputImages.Count + outputImages.Length;
        var writes = new WriteDescriptorSet[totalWrites];
        var imageInfos = new DescriptorImageInfo[Math.Max(inputImages.Count, 1)];
        var outputInfos = new DescriptorImageInfo[outputImages.Length];

        for (var i = 0; i < outputImages.Length; i++)
            outputInfos[i] = new DescriptorImageInfo { ImageView = outputImages[i].View, ImageLayout = ImageLayout.General };

        for (var i = 0; i < inputImages.Count; i++)
        {
            imageInfos[i] = new DescriptorImageInfo
            {
                Sampler = inputImages[i].Sampler,
                ImageView = inputImages[i].View,
                ImageLayout = ImageLayout.ShaderReadOnlyOptimal
            };
        }

        fixed (DescriptorImageInfo* pImageInfos = imageInfos)
        fixed (DescriptorImageInfo* pOutputInfos = outputInfos)
        {
            for (var i = 0; i < inputImages.Count; i++)
            {
                writes[i] = new WriteDescriptorSet
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = descriptorSet,
                    DstBinding = (uint)i,
                    DescriptorCount = 1,
                    DescriptorType = DescriptorType.CombinedImageSampler,
                    PImageInfo = &pImageInfos[i]
                };
            }

            for (var i = 0; i < outputImages.Length; i++)
            {
                writes[inputImages.Count + i] = new WriteDescriptorSet
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = descriptorSet,
                    DstBinding = (uint)(inputImages.Count + i),
                    DescriptorCount = 1,
                    DescriptorType = DescriptorType.StorageImage,
                    PImageInfo = &pOutputInfos[i]
                };
            }

            fixed (WriteDescriptorSet* pWrites = writes)
                _vk.UpdateDescriptorSets(_device, (uint)totalWrites, pWrites, 0, null);
        }
    }

    private List<VkInputImage> CreateInputImages(IReadOnlyList<Image> inputs)
    {
        var inputImages = new List<VkInputImage>(inputs.Count);
        foreach (var input in inputs)
            inputImages.Add(CreateInputImage(input));

        return inputImages;
    }

    private VkInputImage CreateInputImage(Image input)
    {
        if (input.IsCubemap)
            return CreateCubemapInputImage(input);

        var slice = input.Slices[0];
        return Create2DInputImage(slice.Width, slice.Height, MapImageFormat(input.Format), slice.Pixels.Span, Filter.Linear);
    }

    private unsafe VkInputImage Create2DInputImage(int width, int height, Format vkFormat, ReadOnlySpan<byte> pixelData, Filter filter)
    {
        var dataSize = (ulong)pixelData.Length;
        var (stagingBuf, stagingMem) = CreateBuffer(dataSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
        UploadToBuffer(stagingMem, pixelData, dataSize);

        var imageInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Format = vkFormat,
            Extent = new Extent3D((uint)width, (uint)height, 1),
            MipLevels = 1,
            ArrayLayers = 1,
            Samples = SampleCountFlags.Count1Bit,
            Tiling = ImageTiling.Optimal,
            Usage = ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit,
            SharingMode = SharingMode.Exclusive,
            InitialLayout = ImageLayout.Undefined
        };

        _vk.CreateImage(_device, in imageInfo, null, out var image);

        _vk.GetImageMemoryRequirements(_device, image, out var memReqs);
        var memAlloc = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memReqs.Size,
            MemoryTypeIndex = FindMemoryType(memReqs.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit)
        };
        _vk.AllocateMemory(_device, in memAlloc, null, out var imageMem);
        _vk.BindImageMemory(_device, image, imageMem, 0);

        var viewInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = image,
            ViewType = ImageViewType.Type2D,
            Format = vkFormat,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };
        _vk.CreateImageView(_device, in viewInfo, null, out var imageView);

        var samplerInfo = new SamplerCreateInfo
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = filter,
            MinFilter = filter,
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
            MipmapMode = SamplerMipmapMode.Nearest,
            MaxLod = 0
        };
        _vk.CreateSampler(_device, in samplerInfo, null, out var sampler);

        return new VkInputImage(image, imageMem, imageView, sampler, stagingBuf.Handle, stagingMem, width, height, 1, pixelData.Length);
    }

    private unsafe VkInputImage CreateCubemapInputImage(Image input)
    {
        const int layerCount = 6;

        if (input.Slices.Length < layerCount)
            throw new InvalidOperationException("Cubemap input must provide six face slices.");

        var width = input.Width;
        var height = input.Height;
        var vkFormat = MapImageFormat(input.Format);
        var bytesPerLayer = input.Slices[0].Pixels.Length;
        ulong dataSize = 0;
        for (var i = 0; i < layerCount; i++)
            dataSize += (ulong)input.Slices[i].Pixels.Length;

        var (stagingBuf, stagingMem) = CreateBuffer(dataSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
        UploadSlicesToBuffer(stagingMem, input, layerCount, dataSize);

        var imageInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            Flags = ImageCreateFlags.CreateCubeCompatibleBit,
            ImageType = ImageType.Type2D,
            Format = vkFormat,
            Extent = new Extent3D((uint)width, (uint)height, 1),
            MipLevels = 1,
            ArrayLayers = layerCount,
            Samples = SampleCountFlags.Count1Bit,
            Tiling = ImageTiling.Optimal,
            Usage = ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit,
            SharingMode = SharingMode.Exclusive,
            InitialLayout = ImageLayout.Undefined
        };

        _vk.CreateImage(_device, in imageInfo, null, out var image);

        _vk.GetImageMemoryRequirements(_device, image, out var memReqs);
        var memAlloc = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memReqs.Size,
            MemoryTypeIndex = FindMemoryType(memReqs.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit)
        };
        _vk.AllocateMemory(_device, in memAlloc, null, out var imageMem);
        _vk.BindImageMemory(_device, image, imageMem, 0);

        var viewInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = image,
            ViewType = ImageViewType.TypeCube,
            Format = vkFormat,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = layerCount
            }
        };
        _vk.CreateImageView(_device, in viewInfo, null, out var imageView);

        var samplerInfo = new SamplerCreateInfo
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
            MipmapMode = SamplerMipmapMode.Nearest,
            MaxLod = 0
        };
        _vk.CreateSampler(_device, in samplerInfo, null, out var sampler);

        return new VkInputImage(image, imageMem, imageView, sampler, stagingBuf.Handle, stagingMem, width, height, layerCount, bytesPerLayer);
    }

    private unsafe VkOutputImage CreateOutputImage(int width, int height, ImageFormat outputFormat, ulong stagingSize)
    {
        var vkFormat = MapImageFormat(outputFormat);
        var imageInfo = new ImageCreateInfo
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Format = vkFormat,
            Extent = new Extent3D((uint)width, (uint)height, 1),
            MipLevels = 1,
            ArrayLayers = 1,
            Samples = SampleCountFlags.Count1Bit,
            Tiling = ImageTiling.Optimal,
            Usage = ImageUsageFlags.StorageBit | ImageUsageFlags.TransferSrcBit,
            SharingMode = SharingMode.Exclusive,
            InitialLayout = ImageLayout.Undefined
        };

        _vk.CreateImage(_device, in imageInfo, null, out var image);

        _vk.GetImageMemoryRequirements(_device, image, out var memReqs);
        var memAlloc = new MemoryAllocateInfo
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memReqs.Size,
            MemoryTypeIndex = FindMemoryType(memReqs.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit)
        };
        _vk.AllocateMemory(_device, in memAlloc, null, out var imageMem);
        _vk.BindImageMemory(_device, image, imageMem, 0);

        var viewInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = image,
            ViewType = ImageViewType.Type2D,
            Format = vkFormat,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = 1
            }
        };
        _vk.CreateImageView(_device, in viewInfo, null, out var imageView);

        var (stagingBuffer, stagingMemory) = CreateBuffer(stagingSize, BufferUsageFlags.TransferDstBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
        return new VkOutputImage(image, imageMem, imageView, stagingBuffer, stagingMemory, stagingSize, width, height);
    }

    private unsafe void TransitionImageLayout(CommandBuffer cmdBuffer, Silk.NET.Vulkan.Image image, ImageLayout oldLayout, ImageLayout newLayout, int layerCount = 1)
    {
        AccessFlags srcAccess, dstAccess;
        PipelineStageFlags srcStage, dstStage;

        if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
        {
            srcAccess = 0;
            dstAccess = AccessFlags.TransferWriteBit;
            srcStage = PipelineStageFlags.TopOfPipeBit;
            dstStage = PipelineStageFlags.TransferBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            srcAccess = AccessFlags.TransferWriteBit;
            dstAccess = AccessFlags.ShaderReadBit;
            srcStage = PipelineStageFlags.TransferBit;
            dstStage = PipelineStageFlags.ComputeShaderBit;
        }
        else if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.General)
        {
            srcAccess = 0;
            dstAccess = AccessFlags.ShaderWriteBit;
            srcStage = PipelineStageFlags.TopOfPipeBit;
            dstStage = PipelineStageFlags.ComputeShaderBit;
        }
        else if (oldLayout == ImageLayout.General && newLayout == ImageLayout.TransferSrcOptimal)
        {
            srcAccess = AccessFlags.ShaderWriteBit;
            dstAccess = AccessFlags.TransferReadBit;
            srcStage = PipelineStageFlags.ComputeShaderBit;
            dstStage = PipelineStageFlags.TransferBit;
        }
        else
        {
            throw new ArgumentException($"Unsupported layout transition: {oldLayout} → {newLayout}");
        }

        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            SrcAccessMask = srcAccess,
            DstAccessMask = dstAccess,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = image,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                BaseMipLevel = 0,
                LevelCount = 1,
                BaseArrayLayer = 0,
                LayerCount = (uint)layerCount
            }
        };

        _vk.CmdPipelineBarrier(cmdBuffer, srcStage, dstStage, 0, 0, null, 0, null, 1, &barrier);
    }

    private unsafe void RecordBufferToImageCopy(CommandBuffer cmdBuffer, Silk.NET.Vulkan.Buffer stagingBuffer, Silk.NET.Vulkan.Image image, int width, int height, int layerCount, int bytesPerLayer)
    {
        if (layerCount == 1)
        {
            var region = new BufferImageCopy
            {
                BufferOffset = 0,
                BufferRowLength = 0,
                BufferImageHeight = 0,
                ImageSubresource = new ImageSubresourceLayers
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    MipLevel = 0,
                    BaseArrayLayer = 0,
                    LayerCount = 1
                },
                ImageOffset = default,
                ImageExtent = new Extent3D((uint)width, (uint)height, 1)
            };

            _vk.CmdCopyBufferToImage(cmdBuffer, stagingBuffer, image, ImageLayout.TransferDstOptimal, 1, &region);
            return;
        }

        var regions = new BufferImageCopy[layerCount];
        ulong bufferOffset = 0;
        for (var i = 0; i < layerCount; i++)
        {
            regions[i] = new BufferImageCopy
            {
                BufferOffset = bufferOffset,
                BufferRowLength = 0,
                BufferImageHeight = 0,
                ImageSubresource = new ImageSubresourceLayers
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    MipLevel = 0,
                    BaseArrayLayer = (uint)i,
                    LayerCount = 1
                },
                ImageOffset = default,
                ImageExtent = new Extent3D((uint)width, (uint)height, 1)
            };
            bufferOffset += (ulong)bytesPerLayer;
        }

        fixed (BufferImageCopy* pRegions = regions)
            _vk.CmdCopyBufferToImage(cmdBuffer, stagingBuffer, image, ImageLayout.TransferDstOptimal, (uint)regions.Length, pRegions);
    }

    private unsafe void UploadSlicesToBuffer(DeviceMemory memory, Image input, int layerCount, ulong size)
    {
        void* mapped;
        _vk.MapMemory(_device, memory, 0, size, 0, &mapped);

        var destination = new Span<byte>(mapped, checked((int)size));
        var offset = 0;
        for (var i = 0; i < layerCount; i++)
        {
            var slicePixels = input.Slices[i].Pixels.Span;
            slicePixels.CopyTo(destination[offset..]);
            offset += slicePixels.Length;
        }

        _vk.UnmapMemory(_device, memory);
    }

    private unsafe void RecordImageToBufferCopy(CommandBuffer cmdBuffer, Silk.NET.Vulkan.Image image, Silk.NET.Vulkan.Buffer stagingBuffer, int width, int height)
    {
        var region = new BufferImageCopy
        {
            BufferOffset = 0,
            BufferRowLength = 0,
            BufferImageHeight = 0,
            ImageSubresource = new ImageSubresourceLayers
            {
                AspectMask = ImageAspectFlags.ColorBit,
                MipLevel = 0,
                BaseArrayLayer = 0,
                LayerCount = 1
            },
            ImageOffset = default,
            ImageExtent = new Extent3D((uint)width, (uint)height, 1)
        };

        _vk.CmdCopyImageToBuffer(cmdBuffer, image, ImageLayout.TransferSrcOptimal, stagingBuffer, 1, &region);
    }

    private unsafe void CleanupInputImages(List<VkInputImage> inputImages)
    {
        foreach (var img in inputImages)
        {
            _vk.DestroySampler(_device, img.Sampler, null);
            _vk.DestroyImageView(_device, img.View, null);
            _vk.DestroyImage(_device, img.Image, null);
            _vk.FreeMemory(_device, img.ImageMemory, null);
            _vk.DestroyBuffer(_device, img.StagingBuffer, null);
            _vk.FreeMemory(_device, img.StagingMemory, null);
        }
    }

    private unsafe void CleanupOutputImage(VkOutputImage outputImage)
    {
        _vk.DestroyImageView(_device, outputImage.View, null);
        _vk.DestroyImage(_device, outputImage.Image, null);
        _vk.FreeMemory(_device, outputImage.ImageMemory, null);
        _vk.DestroyBuffer(_device, outputImage.StagingBuffer.Handle, null);
        _vk.FreeMemory(_device, outputImage.StagingMemory, null);
    }

    private static Format MapImageFormat(ImageFormat format) => format switch
    {
        ImageFormat.R32G32B32A32Float => Format.R32G32B32A32Sfloat,
        ImageFormat.R32G32B32A32Uint => Format.R32G32B32A32Uint,
        ImageFormat.R32G32B32A32Sint => Format.R32G32B32A32Sint,
        ImageFormat.R32G32B32Float => Format.R32G32B32Sfloat,
        ImageFormat.R16G16B16A16Float => Format.R16G16B16A16Sfloat,
        ImageFormat.R16G16B16A16Unorm => Format.R16G16B16A16Unorm,
        ImageFormat.R16G16B16A16Snorm => Format.R16G16B16A16SNorm,
        ImageFormat.R16G16B16A16Uint => Format.R16G16B16A16Uint,
        ImageFormat.R16G16B16A16Sint => Format.R16G16B16A16Sint,
        ImageFormat.R32G32Float => Format.R32G32Sfloat,
        ImageFormat.R32G32Uint => Format.R32G32Uint,
        ImageFormat.R32G32Sint => Format.R32G32Sint,
        ImageFormat.R10G10B10A2Unorm => Format.A2B10G10R10UnormPack32,
        ImageFormat.R10G10B10A2Uint => Format.A2B10G10R10UintPack32,
        ImageFormat.R11G11B10Float => Format.B10G11R11UfloatPack32,
        ImageFormat.R8G8B8A8Unorm => Format.R8G8B8A8Unorm,
        ImageFormat.R8G8B8A8UnormSrgb => Format.R8G8B8A8Srgb,
        ImageFormat.R8G8B8A8Uint => Format.R8G8B8A8Uint,
        ImageFormat.R8G8B8A8Snorm => Format.R8G8B8A8SNorm,
        ImageFormat.R8G8B8A8Sint => Format.R8G8B8A8Sint,
        ImageFormat.B8G8R8A8Unorm => Format.B8G8R8A8Unorm,
        ImageFormat.B8G8R8A8UnormSrgb => Format.B8G8R8A8Srgb,
        ImageFormat.R16G16Float => Format.R16G16Sfloat,
        ImageFormat.R16G16Unorm => Format.R16G16Unorm,
        ImageFormat.R16G16Snorm => Format.R16G16SNorm,
        ImageFormat.R32Float => Format.R32Sfloat,
        ImageFormat.R32Uint => Format.R32Uint,
        ImageFormat.R32Sint => Format.R32Sint,
        ImageFormat.R8G8Unorm => Format.R8G8Unorm,
        ImageFormat.R8G8Snorm => Format.R8G8SNorm,
        ImageFormat.R16Float => Format.R16Sfloat,
        ImageFormat.R16Unorm => Format.R16Unorm,
        ImageFormat.R8Unorm => Format.R8Unorm,
        ImageFormat.R8Snorm => Format.R8SNorm,
        ImageFormat.BC1Unorm => Format.BC1RgbaUnormBlock,
        ImageFormat.BC1UnormSrgb => Format.BC1RgbaSrgbBlock,
        ImageFormat.BC2Unorm => Format.BC2UnormBlock,
        ImageFormat.BC2UnormSrgb => Format.BC2SrgbBlock,
        ImageFormat.BC3Unorm => Format.BC3UnormBlock,
        ImageFormat.BC3UnormSrgb => Format.BC3SrgbBlock,
        ImageFormat.BC4Unorm => Format.BC4UnormBlock,
        ImageFormat.BC4Snorm => Format.BC4SNormBlock,
        ImageFormat.BC5Unorm => Format.BC5UnormBlock,
        ImageFormat.BC5Snorm => Format.BC5SNormBlock,
        ImageFormat.BC6HUF16 => Format.BC6HUfloatBlock,
        ImageFormat.BC6HSF16 => Format.BC6HSfloatBlock,
        ImageFormat.BC7Unorm => Format.BC7UnormBlock,
        ImageFormat.BC7UnormSrgb => Format.BC7SrgbBlock,
        ImageFormat.R9G9B9E5SharedExp => Format.E5B9G9R9UfloatPack32,
        ImageFormat.B5G6R5Unorm => Format.B5G6R5UnormPack16,
        ImageFormat.B5G5R5A1Unorm => Format.B5G5R5A1UnormPack16,
        ImageFormat.B4G4R4A4Unorm => Format.B4G4R4A4UnormPack16,
        _ => throw new NotSupportedException($"Image format '{format}' has no Vulkan equivalent.")
    };

    private unsafe CommandBuffer AllocateCommandBuffer()
    {
        var allocInfo = new CommandBufferAllocateInfo
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = _commandPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1
        };

        _vk.AllocateCommandBuffers(_device, in allocInfo, out var cmdBuffer);
        return cmdBuffer;
    }

    private unsafe void BeginCommandBuffer(CommandBuffer cmdBuffer)
    {
        var beginInfo = new CommandBufferBeginInfo
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };
        _vk.BeginCommandBuffer(cmdBuffer, in beginInfo);
    }

    private unsafe void EndAndSubmitCommandBuffer(CommandBuffer cmdBuffer)
    {
        _vk.EndCommandBuffer(cmdBuffer);

        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &cmdBuffer
        };

        _vk.QueueSubmit(_computeQueue, 1, in submitInfo, default);
        _vk.QueueWaitIdle(_computeQueue);

        _vk.FreeCommandBuffers(_device, _commandPool, 1, in cmdBuffer);
    }

    private unsafe void DestroyDevice()
    {
        _vk.DeviceWaitIdle(_device);
        _vk.DestroyCommandPool(_device, _commandPool, null);
        _vk.DestroyDevice(_device, null);
        _deviceInitialized = false;
    }
}
