using System.CommandLine;

namespace GymFlow.Cli.Commands;

public static class RestoreCommand
{
    public static Command Build()
    {
        var command = new Command("restore", "Restore from a backup file");

        var backupFileArg = new Argument<string>("backup-file") { Description = "Path to the backup file" };
        var confirmOption = new Option<bool>("--confirm") { Description = "Skip confirmation prompt" };

        command.AddArgument(backupFileArg);
        command.AddOption(confirmOption);

        command.SetHandler(async (string backupFile, bool confirm) =>
        {
            if (!File.Exists(backupFile))
            {
                Console.WriteLine($"✗ Backup file not found: {backupFile}");
                return;
            }

            if (!confirm)
            {
                Console.WriteLine($"⚠ This will restore the database from: {backupFile}");
                Console.Write("Type 'yes' to confirm: ");
                var response = Console.ReadLine();
                if (response?.ToLower() != "yes")
                {
                    Console.WriteLine("Restore cancelled.");
                    return;
                }
            }

            Console.WriteLine("Restoring database...");

            try
            {
                var backupContent = await File.ReadAllTextAsync(backupFile);

                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = "exec -i postgres psql -U gymflow -d gymflow_dev",
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    }
                };
                process.Start();
                await process.StandardInput.WriteAsync(backupContent);
                process.StandardInput.Close();
                
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                    Console.WriteLine("✓ Database restored successfully");
                else
                    Console.WriteLine($"✗ Restore failed: {error}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
            }
        }, backupFileArg, confirmOption);

        return command;
    }
}