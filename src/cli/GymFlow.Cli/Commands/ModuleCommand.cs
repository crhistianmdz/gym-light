using System.CommandLine;

namespace GymFlow.Cli.Commands;

public static class ModuleCommand
{
    public static Command Build()
    {
        var moduleCommand = new Command("module", "Manage GymFlow modules");

        var listCommand = new Command("list", "List all available modules");
        listCommand.SetHandler(() =>
        {
            Console.WriteLine("Available Modules");
            Console.WriteLine("================");
            Console.WriteLine("Name:           Version:   Status:");
            Console.WriteLine("Core            1.0.0      active");
            Console.WriteLine("Members        1.0.0      active");
            Console.WriteLine("Sales          1.0.0      active");
            Console.WriteLine("Freeze         1.0.0      active");
            Console.WriteLine("Cancellation   1.0.0      active");
            Console.WriteLine("Anthropometry   1.0.0      active");
            Console.WriteLine("Routines      1.0.0      active");
            Console.WriteLine("Planning       1.0.0      available");
            Console.WriteLine("Metrics        1.0.0      available");
        });

        var enableCommand = new Command("enable", "Enable a module");
        var nameArg = new Argument<string>("name", "Module name to enable");
        enableCommand.AddArgument(nameArg);
        enableCommand.SetHandler((string name) =>
        {
            Console.WriteLine($"⚠ Module enable requires HU-015 (Plugin System) to be fully integrated.");
            Console.WriteLine($"Use HTTP API to enable modules until CLI integration is complete.");
        }, nameArg);

        var disableCommand = new Command("disable", "Disable a module");
        var disableNameArg = new Argument<string>("name", "Module name to disable");
        disableCommand.AddArgument(disableNameArg);
        disableCommand.SetHandler((string name) =>
        {
            Console.WriteLine($"⚠ Module disable requires HU-015 (Plugin System) to be fully integrated.");
            Console.WriteLine($"Use HTTP API to disable modules until CLI integration is complete.");
        }, disableNameArg);

        moduleCommand.Add(listCommand);
        moduleCommand.Add(enableCommand);
        moduleCommand.Add(disableCommand);

        return moduleCommand;
    }
}