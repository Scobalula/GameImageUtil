using System.Reflection;
using Spectre.Console;
using Spectre.Console.Cli;
using TextureHawk.CLI;
using TextureHawk.CLI.Commands;

var version = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 1, 0);
var console = AnsiConsole.Console;
using var services = CliServices.Create(version);

if (args.Length == 0)
{
    if (Console.IsInputRedirected)
        return await new PipeServer(services, Console.In, Console.Out).RunAsync(CancellationToken.None);

    CliBanner.Render(console, services.VersionText);
    await services.TryWriteUpdateNotificationAsync(console);
    return await new InteractiveWizard(services, console).RunAsync();
}

if (args.Length == 1 && args[0] is "-v" or "--version" or "version")
{
    CliBanner.Render(console, services.VersionText);
    return 0;
}

var registrar = new CliTypeRegistrar();
registrar.RegisterInstance(typeof(CliServices), services);
registrar.RegisterInstance(typeof(IAnsiConsole), console);

var app = new CommandApp(registrar);
app.Configure(config =>
{
    config.SetApplicationName("texturehawk");
    config.ValidateExamples();

    config.AddCommand<ConvertCommand>("convert")
        .WithDescription("Convert an input image with a selected processor.")
        .WithExample("convert", "--input", "albedo.png", "--output", "albedo_converted.dds")
        .WithExample("convert", "--input", "normal.png", "--processor", "builtin.normalmap-flipy");

    config.AddCommand<InfoCommand>("info")
        .WithDescription("Display metadata for an image file.")
        .WithExample("info", "texture.dds");

    config.AddCommand<ListCommand>("list")
        .WithDescription("List available processors.")
        .WithExample("list");

    config.AddCommand<PipeCommand>("pipe")
        .WithDescription("Keep the CLI open and accept newline-delimited JSON commands on stdin.")
        .WithExample("pipe");
});

if (args.Length == 1 && args[0] is "-h" or "--help" or "help")
{
    CliBanner.Render(console, services.VersionText);
    var exitCode = await app.RunAsync(["--help"]);
    await services.TryWriteUpdateNotificationAsync(console);
    return exitCode;
}

return await app.RunAsync(args);
