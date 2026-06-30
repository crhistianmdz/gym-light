using System.CommandLine;

namespace GymFlow.Cli.Commands;

public static class BackupCommand
{
    public static Command Build()
    {
        var command = new Command("backup", "Create a database backup");

        var outputOption = new Option<string>("--output") { Description = "Output path for the backup file" };
        var dryRunOption = new Option<bool>("--dry-run") { Description = "Show what would be done without doing it" };
        var verboseOption = new Option<bool>("--verbose") { Description = "Enable verbose output" };

        command.AddOption(outputOption);
        command.AddOption(dryRunOption);
        command.AddOption(verboseOption);

        command.SetHandler(async (string outputPath, bool dryRun, bool verbose) =>
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var defaultPath = $"backups/gymflow-pre-v1.0.0-{timestamp}.sql";
            var finalPath = outputPath ?? defaultPath;

            if (dryRun)
            {
                Console.WriteLine($"[DRY RUN] Would create backup: {finalPath}");
                return;
            }

            Console.WriteLine($"Creating backup: {finalPath}");

            try
            {
                Directory.CreateDirectory("backups");

                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = "exec postgres pg_dump -U gymflow -d gymflow_dev -F p",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    await File.WriteAllTextAsync(finalPath, output);
                    Console.WriteLine($"✓ Backup created: {finalPath}");
                }
                else
                {
                    Console.WriteLine($"✗ Backup failed: {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
            }
        }, outputOption, dryRunOption, verboseOption);

        return command;
    }
}