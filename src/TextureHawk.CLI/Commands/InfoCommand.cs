using Spectre.Console;
using Spectre.Console.Cli;

namespace TextureHawk.CLI.Commands;

/// <summary>
/// Displays information about an image file.
/// </summary>
internal sealed class InfoCommand(CliServices services, IAnsiConsole console) : AsyncCommand<InfoCommandSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, InfoCommandSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.FilePath))
        {
            console.MarkupLine("[red]Error:[/] A file path is required.");
            return Task.FromResult(1);
        }

        var filePath = Path.GetFullPath(settings.FilePath);
        if (!File.Exists(filePath))
        {
            console.MarkupLine($"[red]Error:[/] File not found: [grey]{Markup.Escape(filePath)}[/]");
            return Task.FromResult(1);
        }

        try
        {
            var info = services.ReadImageInfo(filePath);
            var table = new Table().HideHeaders();
            table.AddColumn("key");
            table.AddColumn("value");
            table.AddRow("File", Markup.Escape(info.FilePath));
            table.AddRow("Dimensions", Markup.Escape($"{info.Width} x {info.Height}"));
            table.AddRow("Depth", info.Depth.ToString());
            table.AddRow("Array Size", info.ArraySize.ToString());
            table.AddRow("Mip Levels", info.MipLevels.ToString());
            table.AddRow("Format", Markup.Escape(info.Format));
            table.AddRow("Cubemap", info.IsCubemap ? "true" : "false");
            table.AddRow("Slices", info.SliceCount.ToString());
            console.Write(table);
            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            console.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return Task.FromResult(1);
        }
    }
}
