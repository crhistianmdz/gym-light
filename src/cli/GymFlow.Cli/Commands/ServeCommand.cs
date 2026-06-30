using System.CommandLine;

namespace GymFlow.Cli.Commands;

public static class ServeCommand
{
    public static Command Build()
    {
        var command = new Command("serve", "Start GymFlow services locally");

        var portOption = new Option<int>("--port", () => 3000) { Description = "Port for the frontend" };
        var verboseOption = new Option<bool>("--verbose") { Description = "Enable verbose output" };

        command.AddOption(portOption);
        command.AddOption(verboseOption);

        command.SetHandler(async (int port, bool verbose) =>
        {
            Console.WriteLine("Starting GymFlow services...");

            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = "compose up -d",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        WorkingDirectory = ".."
                    }
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (verbose || process.ExitCode != 0)
                {
                    if (!string.IsNullOrEmpty(output))
                        Console.WriteLine(output);
                    if (!string.IsNullOrEmpty(error))
                        Console.WriteLine($"Error: {error}");
                }

                if (process.ExitCode == 0)
                {
                    Console.WriteLine("✓ Services started");
                    Console.WriteLine($"  Frontend:  http://localhost:{port}");
                    Console.WriteLine($"  Backend:  http://localhost:5000");
                    Console.WriteLine($"  API:      http://localhost:5000/api");
                }
                else
                {
                    Console.WriteLine("✗ Failed to start services");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
            }
        }, portOption, verboseOption);

        return command;
    }
}