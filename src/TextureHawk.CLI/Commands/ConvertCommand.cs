using Spectre.Console;
using Spectre.Console.Cli;
using TextureHawk.Gpu;

namespace TextureHawk.CLI.Commands;

/// <summary>
/// Executes a conversion request from the command line.
/// </summary>
internal sealed class ConvertCommand(CliServices services, IAnsiConsole console) : AsyncCommand<ConvertCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ConvertCommandSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.InputPath))
        {
            console.MarkupLine("[red]Error:[/] An input path is required. Use [grey]--input <path>[/].");
            return 1;
        }

        var processorId = settings.ProcessorId ?? CliServices.DefaultProcessorId;
        var processor = services.GetProcessor(processorId);
        if (processor is null)
        {
            console.MarkupLine($"[red]Error:[/] Unknown processor [grey]{Markup.Escape(processorId)}[/].");
            return 1;
        }

        if (!services.TryParseGpuColorSpaceMode(settings.GpuColorSpaceMode, out var gpuColorSpaceMode, out var gpuError))
        {
            console.MarkupLine($"[red]Error:[/] {Markup.Escape(gpuError ?? "Unsupported GPU color-space mode.")}");
            return 1;
        }

        var rawParameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var assignment in settings.Parameters)
        {
            if (!ProcessorParameterBinder.TryParseAssignment(assignment, out var key, out var value))
            {
                console.MarkupLine($"[red]Error:[/] Invalid parameter assignment [grey]{Markup.Escape(assignment)}[/]. Use [grey]--param key=value[/].");
                return 1;
            }

            rawParameters[key] = value;
        }

        if (!string.IsNullOrWhiteSpace(settings.TargetFormat))
            rawParameters["targetFormat"] = settings.TargetFormat;

        if (!ProcessorParameterBinder.TryBind(processor, rawParameters, out var parameters, out var parameterError))
        {
            console.MarkupLine($"[red]Error:[/] {Markup.Escape(parameterError ?? "Invalid parameter values.")}");
            return 1;
        }

        var request = new ConvertRequest
        {
            InputPath = settings.InputPath,
            OutputPath = settings.OutputPath,
            ProcessorId = processor.Id,
            ParameterValues = parameters,
            AdditionalInputPaths = settings.AdditionalInputs,
            GpuColorSpaceMode = gpuColorSpaceMode
        };

        var summary = new Table().HideHeaders();
        summary.AddColumn("key");
        summary.AddColumn("value");
        summary.AddRow("Input", Markup.Escape(Path.GetFullPath(settings.InputPath)));
        summary.AddRow("Processor", $"{Markup.Escape(processor.Name)} [grey]({Markup.Escape(processor.Id)})[/]");
        summary.AddRow("Output", Markup.Escape(string.IsNullOrWhiteSpace(settings.OutputPath)
            ? services.GetDefaultOutputPath(Path.GetFullPath(settings.InputPath))
            : Path.GetFullPath(settings.OutputPath)));

        if (gpuColorSpaceMode is not null)
            summary.AddRow("GPU Color Space", Markup.Escape(GpuColorSpaceModeNames.ToDisplayName(gpuColorSpaceMode.Value)));

        console.Write(summary);
        console.WriteLine();

        var result = await services.ExecuteConvertAsync(request, CancellationToken.None);

        if (result.Success)
        {
            if (result.WrittenPaths.Count == 0)
            {
                console.MarkupLine("[green]Done.[/]");
            }
            else if (result.WrittenPaths.Count == 1)
            {
                console.MarkupLine($"[green]Done.[/] Wrote [grey]{Markup.Escape(result.WrittenPaths[0])}[/].");
            }
            else
            {
                console.MarkupLine("[green]Done.[/] Wrote output files:");
                foreach (var path in result.WrittenPaths)
                    console.MarkupLine($"  [grey]{Markup.Escape(path)}[/]");
            }

            return 0;
        }

        console.MarkupLine($"[red]Error:[/] {Markup.Escape(result.ErrorMessage ?? "Processing failed.")}");
        return 1;
    }
}
