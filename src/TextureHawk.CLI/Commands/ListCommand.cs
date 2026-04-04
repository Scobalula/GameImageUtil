using Spectre.Console;
using Spectre.Console.Cli;

namespace TextureHawk.CLI.Commands;

/// <summary>
/// Lists all available processors.
/// </summary>
internal sealed class ListCommand(CliServices services, IAnsiConsole console) : AsyncCommand<ListCommandSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, ListCommandSettings settings)
    {
        var grouped = services.ProcessorRegistry.GetByCategory();

        if (grouped.Count == 0)
        {
            console.MarkupLine("[yellow]No processors registered.[/]");
            return Task.FromResult(0);
        }

        var tree = new Tree("[bold #ff6aa6]Available Processors[/]");

        foreach (var (category, processors) in grouped.OrderBy(g => g.Key))
        {
            var categoryNode = tree.AddNode($"[bold]{Markup.Escape(category)}[/]");
            foreach (var processor in processors.OrderBy(p => p.Name))
            {
                var processorNode = categoryNode.AddNode($"{Markup.Escape(processor.Name)} [grey]({Markup.Escape(processor.Id)})[/]");
                if (!string.IsNullOrEmpty(processor.Description))
                    processorNode.AddNode($"[grey]{Markup.Escape(processor.Description)}[/]");

                if (processor.Parameters.Count > 0)
                {
                    var parametersNode = processorNode.AddNode("[grey]parameters[/]");
                    foreach (var param in processor.Parameters)
                    {
                        var detail = $"--param {param.Id}=<{param.Type}>";
                        if (param.DefaultValue is not null)
                            detail += $" (default: {param.DefaultValue})";
                        parametersNode.AddNode($"[grey]{Markup.Escape(detail)}[/]");
                    }
                }
            }
        }

        console.Write(tree);
        return Task.FromResult(0);
    }
}
