using System.CommandLine;
using System.CommandLine.Parsing;
using GymFlow.Cli.Commands;

var rootCommand = new RootCommand("GymFlow CLI - Self-hosted gym management");

// Global options
var verboseOption = new Option<bool>("--verbose", "Enable verbose output");
var configOption = new Option<string>("--config", "Path to configuration file");
var dryRunOption = new Option<bool>("--dry-run", "Show what would be done without doing it");

rootCommand.AddGlobalOption(verboseOption);
rootCommand.AddGlobalOption(configOption);
rootCommand.AddGlobalOption(dryRunOption);

// Register subcommands
rootCommand.AddCommand(InstallCommand.Build());
rootCommand.AddCommand(UpgradeCommand.Build());
rootCommand.AddCommand(StatusCommand.Build());
rootCommand.AddCommand(DoctorCommand.Build());
rootCommand.AddCommand(ModuleCommand.Build());
rootCommand.AddCommand(ServeCommand.Build());
rootCommand.AddCommand(BackupCommand.Build());
rootCommand.AddCommand(RestoreCommand.Build());

await rootCommand.InvokeAsync(args);