using Spectre.Console.Cli;

namespace TextureHawk.CLI.Commands;

internal sealed class InfoCommandSettings : CommandSettings
{
    [CommandArgument(0, "<FILE>")]
    public string? FilePath { get; init; }
}