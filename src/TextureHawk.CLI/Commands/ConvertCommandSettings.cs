using Spectre.Console.Cli;

namespace TextureHawk.CLI.Commands;

internal sealed class ConvertCommandSettings : CommandSettings
{
    [CommandOption("-i|--input <INPUT>")]
    public string? InputPath { get; init; }

    [CommandOption("-o|--output <OUTPUT>")]
    public string? OutputPath { get; init; }

    [CommandOption("-p|--processor <PROCESSOR>")]
    public string? ProcessorId { get; init; }

    [CommandOption("-f|--format <FORMAT>")]
    public string? TargetFormat { get; init; }

    [CommandOption("-a|--additional <INPUT>")]
    public string[] AdditionalInputs { get; init; } = [];

    [CommandOption("--gpu-color-space <MODE>")]
    public string? GpuColorSpaceMode { get; init; }

    [CommandOption("--param <PARAMETER>")]
    public string[] Parameters { get; init; } = [];
}