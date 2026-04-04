using System.Text.Json;
using System.Text.Json.Serialization;
using TextureHawk.Gpu;

namespace TextureHawk.CLI;

internal sealed class PipeServer(CliServices services, TextReader reader, TextWriter writer)
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync();
            if (line is null)
                return 0;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            PipeResponse response;
            var shouldExit = false;

            try
            {
                var request = JsonSerializer.Deserialize<PipeRequest>(line, s_jsonOptions);
                if (request is null || string.IsNullOrWhiteSpace(request.Command))
                {
                    response = CreateError(null, "invalid", "A JSON request with a command is required.");
                }
                else
                {
                    (response, shouldExit) = await HandleAsync(request, cancellationToken);
                }
            }
            catch (JsonException ex)
            {
                response = CreateError(null, "invalid", $"Invalid JSON request: {ex.Message}");
            }

            await WriteResponseAsync(response);
            if (shouldExit)
                return response.ExitCode;
        }
    }

    private async Task<(PipeResponse Response, bool ShouldExit)> HandleAsync(PipeRequest request, CancellationToken cancellationToken)
    {
        var command = request.Command!.Trim().ToLowerInvariant();
        switch (command)
        {
            case "convert":
                return (await HandleConvertAsync(request, cancellationToken), false);

            case "info":
                return (HandleInfo(request), false);

            case "list":
                return (HandleList(request), false);

            case "exit":
            case "quit":
            case "shutdown":
                return (new PipeResponse
                {
                    RequestId = request.RequestId,
                    Command = "exit",
                    Success = true,
                    ExitCode = 0,
                    Data = new { message = "Pipe server shutting down." }
                }, true);

            default:
                return (CreateError(request.RequestId, command, $"Unknown command '{request.Command}'."), false);
        }
    }

    private async Task<PipeResponse> HandleConvertAsync(PipeRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.InputPath))
            return CreateError(request.RequestId, "convert", "The convert command requires inputPath.");

        var processorId = request.ProcessorId ?? CliServices.DefaultProcessorId;
        var processor = services.GetProcessor(processorId);
        if (processor is null)
            return CreateError(request.RequestId, "convert", $"Processor '{processorId}' not found.");

        if (!services.TryParseGpuColorSpaceMode(request.GpuColorSpaceMode, out var gpuColorSpaceMode, out var gpuError))
            return CreateError(request.RequestId, "convert", gpuError ?? "Unsupported GPU color-space mode.");

        var rawParameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (request.Parameters is not null)
        {
            foreach (var (key, value) in request.Parameters)
                rawParameters[key] = value;
        }

        if (!string.IsNullOrWhiteSpace(request.TargetFormat))
            rawParameters["targetFormat"] = request.TargetFormat;

        if (!ProcessorParameterBinder.TryBind(processor, rawParameters, out var parameterValues, out var parameterError))
            return CreateError(request.RequestId, "convert", parameterError ?? "Invalid processor parameter values.");

        var result = await services.ExecuteConvertAsync(new ConvertRequest
        {
            InputPath = request.InputPath,
            OutputPath = request.OutputPath,
            ProcessorId = processor.Id,
            ParameterValues = parameterValues,
            AdditionalInputPaths = request.AdditionalInputPaths ?? [],
            GpuColorSpaceMode = gpuColorSpaceMode
        }, cancellationToken);

        if (!result.Success)
            return CreateError(request.RequestId, "convert", result.ErrorMessage ?? "Processing failed.");

        return new PipeResponse
        {
            RequestId = request.RequestId,
            Command = "convert",
            Success = true,
            ExitCode = 0,
            Data = new
            {
                inputPath = result.InputPath,
                outputPath = result.OutputPath,
                writtenPaths = result.WrittenPaths,
                processorId = result.ProcessorId,
                gpuColorSpaceMode = gpuColorSpaceMode is null ? null : GpuColorSpaceModeNames.ToDisplayName(gpuColorSpaceMode.Value)
            }
        };
    }

    private PipeResponse HandleInfo(PipeRequest request)
    {
        var filePath = request.FilePath ?? request.InputPath;
        if (string.IsNullOrWhiteSpace(filePath))
            return CreateError(request.RequestId, "info", "The info command requires filePath.");

        try
        {
            var info = services.ReadImageInfo(filePath);
            return new PipeResponse
            {
                RequestId = request.RequestId,
                Command = "info",
                Success = true,
                ExitCode = 0,
                Data = info
            };
        }
        catch (Exception ex)
        {
            return CreateError(request.RequestId, "info", ex.Message);
        }
    }

    private PipeResponse HandleList(PipeRequest request)
    {
        var data = services.GetAllProcessors()
            .OrderBy(processor => processor.Category)
            .ThenBy(processor => processor.Name)
            .Select(processor => new
            {
                id = processor.Id,
                name = processor.Name,
                category = processor.Category,
                description = processor.Description,
                parameters = processor.Parameters.Select(parameter => new
                {
                    id = parameter.Id,
                    name = parameter.Name,
                    description = parameter.Description,
                    type = parameter.Type.ToString(),
                    defaultValue = parameter.DefaultValue?.ToString(),
                    minValue = parameter.MinValue?.ToString(),
                    maxValue = parameter.MaxValue?.ToString(),
                    options = parameter.Options
                })
            })
            .ToList();

        return new PipeResponse
        {
            RequestId = request.RequestId,
            Command = "list",
            Success = true,
            ExitCode = 0,
            Data = data
        };
    }

    private async Task WriteResponseAsync(PipeResponse response)
    {
        var json = JsonSerializer.Serialize(response, s_jsonOptions);
        await writer.WriteLineAsync(json);
        await writer.FlushAsync();
    }

    private static PipeResponse CreateError(string? requestId, string command, string error)
    {
        return new PipeResponse
        {
            RequestId = requestId,
            Command = command,
            Success = false,
            ExitCode = 1,
            Error = error
        };
    }
}