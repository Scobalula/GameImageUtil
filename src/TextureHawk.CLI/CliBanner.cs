using Spectre.Console;

namespace TextureHawk.CLI;

internal static class CliBanner
{
    public static void Render(IAnsiConsole console, string versionText)
    {
        const string hawk = @"
         ______________
      .-'  __  /\  __  `-.
    .'   .'  \/  \/  `.   `.
   /   .'   _/ /\ \_   `.   \
   \   \_.-' _/  \_ `-._/   /
    `-._   _/ /\  \_   _.-'
         `-._/  \_.-'
              \/";

        console.WriteLine();
        console.MarkupLine($"[bold #ff4f8f]{Markup.Escape(hawk)}[/]");
        console.MarkupLine("[bold #ff6aa6]TextureHawk[/]");
        console.MarkupLine("[grey]High-performance texture tooling[/]");
        console.MarkupLine($"[grey]v{Markup.Escape(versionText)}[/]");
        console.WriteLine();
    }
}