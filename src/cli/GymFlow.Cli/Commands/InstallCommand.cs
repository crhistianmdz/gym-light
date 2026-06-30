using System.CommandLine;

namespace GymFlow.Cli.Commands;

public static class InstallCommand
{
    public static Command Build()
    {
        var command = new Command("install", "Install a new GymFlow Lite instance");

        var nameOption = new Option<string>("--name") { Description = "Name of the gym instance", IsRequired = true };
        var urlOption = new Option<string>("--url") { Description = "Public URL for the instance" };
        var modeOption = new Option<string>("--mode", () => "docker") { Description = "Installation mode: docker or native" };
        var dryRunOption = new Option<bool>("--dry-run") { Description = "Show what would be done without doing it" };
        var verboseOption = new Option<bool>("--verbose") { Description = "Enable verbose output" };

        command.AddOption(nameOption);
        command.AddOption(urlOption);
        command.AddOption(modeOption);
        command.AddOption(dryRunOption);
        command.AddOption(verboseOption);

        command.SetHandler((string name, string url, string mode, bool dryRun, bool verbose) =>
        {
            name = name?.Trim('"') ?? "gymflow";
            url = url?.Trim('"');
            mode = mode?.Trim('"') ?? "docker";

            if (verbose)
                Console.WriteLine($"Installing {name} in {mode} mode...");

            try
            {
                var checker = new Helpers.PrerequisitesChecker();
                if (!checker.ValidateAll())
                    return;

                if (dryRun)
                {
                    Console.WriteLine("[DRY RUN] Would:");
                    Console.WriteLine("  1. Generate docker-compose.yml");
                    Console.WriteLine("  2. Generate .env file");
                    Console.WriteLine("  3. Create backup/ directory");
                    Console.WriteLine("  4. Initialize database");
                    Console.WriteLine("  5. Register base modules");
                    return;
                }

                var generator = new Helpers.DockerComposeGenerator();
                var envGen = new Helpers.EnvironmentGenerator();
                generator.Generate(name, url ?? $"https://{name}.example.com");
                envGen.Generate(name);

                Console.WriteLine($"✓ Installed {name} successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Installation failed: {ex.Message}");
            }
        }, nameOption, urlOption, modeOption, dryRunOption, verboseOption);

        return command;
    }
}