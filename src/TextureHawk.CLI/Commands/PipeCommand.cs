using Spectre.Console.Cli;

namespace TextureHawk.CLI.Commands;

internal sealed class PipeCommand(CliServices services) : AsyncCommand<PipeCommandSettings>
{
    public override Task<int> ExecuteAsync(CommandContext context, PipeCommandSettings settings)
    {
        return new PipeServer(services, Console.In, Console.Out).RunAsync(CancellationToken.None);
    }
}
