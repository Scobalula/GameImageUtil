using System.Globalization;
using RedFox.Graphics2D;
using Spectre.Console;
using TextureHawk.Gpu;
using TextureHawk.Processing;

namespace TextureHawk.CLI;

internal sealed class InteractiveWizard(CliServices services, IAnsiConsole console)
{
    public async Task<int> RunAsync()
    {
        var choice = PromptSelection(
            "What do you want to do?",
            ["convert", "info", "list", "pipe", "exit"],
            "convert");

        return choice switch
        {
            "convert" => await RunConvertAsync(),
            "info" => RunInfo(),
            "list" => RunList(),
            "pipe" => await RunPipeAsync(),
            _ => 0
        };
    }

    private async Task<int> RunConvertAsync()
    {
        var inputPath = PromptExistingFile("Input file path");
        var suggestedProcessorId = services.DetectProcessorId(inputPath);
        var processorId = PromptProcessor(suggestedProcessorId);
        var processor = services.GetProcessor(processorId)!;
        var outputPath = ReadWithDefault("Output file path", services.GetDefaultOutputPath(inputPath));
        var additionalInputs = ParseAdditionalInputs(ReadWithDefault("Additional input files (comma-separated, optional)", string.Empty, allowEmpty: true));
        var gpuColorSpaceMode = PromptGpuColorSpaceMode();
        var parameterValues = PromptParameterValues(processor);

        var summary = new Table().HideHeaders();
        summary.AddColumn("key");
        summary.AddColumn("value");
        summary.AddRow("Input", Markup.Escape(inputPath));
        summary.AddRow("Processor", $"{Markup.Escape(processor.Name)} [grey]({Markup.Escape(processor.Id)})[/]");
        summary.AddRow("Output", Markup.Escape(outputPath));
        summary.AddRow("GPU Color Space", Markup.Escape(GpuColorSpaceModeNames.ToDisplayName(gpuColorSpaceMode ?? services.GetDefaultGpuColorSpaceMode())));
        summary.AddRow("Parameters", parameterValues.Count.ToString(CultureInfo.InvariantCulture));
        console.Write(summary);
        console.WriteLine();

        var shouldRun = PromptSelection("Run this conversion now?", ["yes", "no"], "yes") == "yes";
        if (!shouldRun)
            return 0;

        var result = await services.ExecuteConvertAsync(new ConvertRequest
        {
            InputPath = inputPath,
            OutputPath = outputPath,
            ProcessorId = processor.Id,
            ParameterValues = parameterValues,
            AdditionalInputPaths = additionalInputs,
            GpuColorSpaceMode = gpuColorSpaceMode
        }, CancellationToken.None);

        if (!result.Success)
        {
            console.MarkupLine($"[red]Error:[/] {Markup.Escape(result.ErrorMessage ?? "Processing failed.")}");
            return 1;
        }

        console.MarkupLine("[green]Done.[/]");
        foreach (var path in result.WrittenPaths)
            console.MarkupLine($"  [grey]{Markup.Escape(path)}[/]");

        return 0;
    }

    private int RunInfo()
    {
        var filePath = PromptExistingFile("File to inspect");

        try
        {
            var info = services.ReadImageInfo(filePath);
            var table = new Table().HideHeaders();
            table.AddColumn("key");
            table.AddColumn("value");
            table.AddRow("File", Markup.Escape(info.FilePath));
            table.AddRow("Dimensions", Markup.Escape($"{info.Width} x {info.Height}"));
            table.AddRow("Depth", info.Depth.ToString(CultureInfo.InvariantCulture));
            table.AddRow("Array Size", info.ArraySize.ToString(CultureInfo.InvariantCulture));
            table.AddRow("Mip Levels", info.MipLevels.ToString(CultureInfo.InvariantCulture));
            table.AddRow("Format", Markup.Escape(info.Format));
            table.AddRow("Cubemap", info.IsCubemap ? "true" : "false");
            table.AddRow("Slices", info.SliceCount.ToString(CultureInfo.InvariantCulture));
            console.Write(table);
            return 0;
        }
        catch (Exception ex)
        {
            console.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    private int RunList()
    {
        var tree = new Tree("[bold #ff6aa6]Available Processors[/]");
        foreach (var (category, processors) in services.ProcessorRegistry.GetByCategory().OrderBy(group => group.Key))
        {
            var categoryNode = tree.AddNode($"[bold]{Markup.Escape(category)}[/]");
            foreach (var processor in processors.OrderBy(processor => processor.Name))
            {
                var processorNode = categoryNode.AddNode($"{Markup.Escape(processor.Name)} [grey]({Markup.Escape(processor.Id)})[/]");
                if (!string.IsNullOrWhiteSpace(processor.Description))
                    processorNode.AddNode($"[grey]{Markup.Escape(processor.Description)}[/]");
            }
        }

        console.Write(tree);
        return 0;
    }

    private async Task<int> RunPipeAsync()
    {
        console.MarkupLine("[grey]Enter newline-delimited JSON requests. Use {\"command\":\"exit\"} or Ctrl+Z to stop.[/]");
        return await new PipeServer(services, Console.In, Console.Out).RunAsync(CancellationToken.None);
    }

    private Dictionary<string, object> PromptParameterValues(IProcessor processor)
    {
        var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (processor.Parameters.Count == 0)
            return values;

        console.MarkupLine($"[bold]Processor Parameters[/] [grey]for {Markup.Escape(processor.Name)}[/]");
        foreach (var parameter in processor.Parameters)
        {
            console.MarkupLine($"[grey]{Markup.Escape(parameter.Description)}[/]");
            values[parameter.Id] = PromptParameterValue(parameter);
        }

        console.WriteLine();
        return values;
    }

    private object PromptParameterValue(ProcessorParameter parameter)
    {
        return parameter.Type switch
        {
            ProcessorParameterType.Boolean => PromptBoolean(parameter),
            ProcessorParameterType.Integer => PromptInteger(parameter),
            ProcessorParameterType.Float => PromptFloat(parameter),
            ProcessorParameterType.Choice => PromptChoice(parameter),
            ProcessorParameterType.ImageFormat => PromptImageFormat(parameter),
            ProcessorParameterType.FilePath => ReadWithDefault(parameter.Name, parameter.DefaultValue?.ToString() ?? string.Empty, allowEmpty: true),
            _ => ReadWithDefault(parameter.Name, parameter.DefaultValue?.ToString() ?? string.Empty, allowEmpty: true)
        };
    }

    private bool PromptBoolean(ProcessorParameter parameter)
    {
        var defaultValue = parameter.DefaultValue is bool booleanValue && booleanValue;
        var selection = PromptSelection(
            $"{parameter.Name} [grey](default: {(defaultValue ? "true" : "false")})[/]",
            defaultValue ? ["true", "false"] : ["false", "true"],
            defaultValue ? "true" : "false");
        return string.Equals(selection, "true", StringComparison.OrdinalIgnoreCase);
    }

    private int PromptInteger(ProcessorParameter parameter)
    {
        var defaultValue = parameter.DefaultValue?.ToString() ?? "0";
        while (true)
        {
            var raw = ReadWithDefault(BuildNumericPrompt(parameter), defaultValue);
            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) && IsInRange(parameter, value))
                return value;

            console.MarkupLine($"[red]Enter a valid integer for {Markup.Escape(parameter.Name)}.[/]");
        }
    }

    private float PromptFloat(ProcessorParameter parameter)
    {
        var defaultValue = parameter.DefaultValue is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : parameter.DefaultValue?.ToString() ?? "0";
        while (true)
        {
            var raw = ReadWithDefault(BuildNumericPrompt(parameter), defaultValue);
            if (float.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value) && IsInRange(parameter, value))
                return value;

            console.MarkupLine($"[red]Enter a valid floating-point value for {Markup.Escape(parameter.Name)}.[/]");
        }
    }

    private string PromptChoice(ProcessorParameter parameter)
    {
        var options = parameter.Options ?? [];
        var defaultValue = parameter.DefaultValue?.ToString();
        return PromptSelection($"{parameter.Name} [grey](default: {Markup.Escape(defaultValue ?? options.FirstOrDefault() ?? string.Empty)})[/]", options, defaultValue);
    }

    private ImageFormat PromptImageFormat(ProcessorParameter parameter)
    {
        var defaultValue = parameter.DefaultValue is ImageFormat format ? format.ToString() : ImageFormat.R8G8B8A8Unorm.ToString();
        while (true)
        {
            var raw = ReadWithDefault($"{parameter.Name} [grey](default: {Markup.Escape(defaultValue)})[/]", defaultValue);
            if (Enum.TryParse<ImageFormat>(raw, true, out var value))
                return value;

            console.MarkupLine($"[red]Enter a valid image format such as {Markup.Escape(defaultValue)}.[/]");
        }
    }

    private GpuColorSpaceMode? PromptGpuColorSpaceMode()
    {
        var defaultValue = GpuColorSpaceModeNames.ToDisplayName(services.GetDefaultGpuColorSpaceMode());
        var selection = PromptSelection(
            $"GPU color space [grey](default: {Markup.Escape(defaultValue)})[/]",
            GpuColorSpaceModeNames.Options,
            defaultValue);
        return GpuColorSpaceModeNames.TryParse(selection, out var mode) ? mode : services.GetDefaultGpuColorSpaceMode();
    }

    private string PromptProcessor(string suggestedProcessorId)
    {
        var processors = services.GetAllProcessors()
            .OrderBy(processor => !string.Equals(processor.Id, suggestedProcessorId, StringComparison.OrdinalIgnoreCase))
            .ThenBy(processor => processor.Category)
            .ThenBy(processor => processor.Name)
            .Select(processor => new ProcessorDisplay(
                processor.Id,
                $"{processor.Category} :: {processor.Name} ({processor.Id})"))
            .ToList();
        var defaultDisplay = processors.First(processor => string.Equals(processor.Id, suggestedProcessorId, StringComparison.OrdinalIgnoreCase)).Display;
        var selected = PromptSelection(
            $"Processor [grey](suggested: {Markup.Escape(suggestedProcessorId)})[/]",
            processors.Select(processor => processor.Display).ToList(),
            defaultDisplay);
        return processors.First(processor => processor.Display == selected).Id;
    }

    private string PromptExistingFile(string title)
    {
        while (true)
        {
            var raw = console.Prompt(new TextPrompt<string>($"{title}:").AllowEmpty());
            if (!string.IsNullOrWhiteSpace(raw))
            {
                var fullPath = Path.GetFullPath(raw);
                if (File.Exists(fullPath))
                    return fullPath;
            }

            console.MarkupLine("[red]Enter an existing file path.[/]");
        }
    }

    private string PromptSelection(string title, IReadOnlyCollection<string> options, string? defaultValue)
    {
        var orderedOptions = OrderWithDefault(options, defaultValue);
        var prompt = new SelectionPrompt<string>().Title(title);
        prompt.AddChoices(orderedOptions);
        return console.Prompt(prompt);
    }

    private string ReadWithDefault(string title, string defaultValue, bool allowEmpty = false)
    {
        while (true)
        {
            var raw = console.Prompt(new TextPrompt<string>($"{title} [grey](default: {Markup.Escape(defaultValue)})[/]:").AllowEmpty());
            if (string.IsNullOrWhiteSpace(raw))
                return defaultValue;

            if (allowEmpty || !string.IsNullOrWhiteSpace(raw))
                return raw.Trim();
        }
    }

    private static List<string> OrderWithDefault(IReadOnlyCollection<string> options, string? defaultValue)
    {
        return options
            .OrderBy(option => !string.Equals(option, defaultValue, StringComparison.OrdinalIgnoreCase))
            .ThenBy(option => option, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<string> ParseAdditionalInputs(string raw)
    {
        return raw.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static string BuildNumericPrompt(ProcessorParameter parameter)
    {
        var range = string.Empty;
        if (parameter.MinValue is not null || parameter.MaxValue is not null)
        {
            var min = parameter.MinValue?.ToString() ?? "-inf";
            var max = parameter.MaxValue?.ToString() ?? "+inf";
            range = $" [grey](range: {min} to {max})[/]";
        }

        return $"{parameter.Name}{range}";
    }

    private static bool IsInRange(ProcessorParameter parameter, int value)
    {
        return IsInRange(parameter, Convert.ToDecimal(value, CultureInfo.InvariantCulture));
    }

    private static bool IsInRange(ProcessorParameter parameter, float value)
    {
        return IsInRange(parameter, Convert.ToDecimal(value, CultureInfo.InvariantCulture));
    }

    private static bool IsInRange(ProcessorParameter parameter, decimal value)
    {
        if (parameter.MinValue is not null)
        {
            var min = Convert.ToDecimal(parameter.MinValue, CultureInfo.InvariantCulture);
            if (value < min)
                return false;
        }

        if (parameter.MaxValue is not null)
        {
            var max = Convert.ToDecimal(parameter.MaxValue, CultureInfo.InvariantCulture);
            if (value > max)
                return false;
        }

        return true;
    }

    private readonly record struct ProcessorDisplay(string Id, string Display);
}
