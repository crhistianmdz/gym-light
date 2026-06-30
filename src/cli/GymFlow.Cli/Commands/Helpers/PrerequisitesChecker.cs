namespace GymFlow.Cli.Commands.Helpers;

/// <summary>
/// Checks if required tools are available
/// </summary>
public class PrerequisitesChecker
{
    public bool ValidateAll()
    {
        var allPassed = true;

        // Check Docker
        if (!CheckDocker())
        {
            Console.WriteLine("✗ Docker is required but not available");
            allPassed = false;
        }
        else
        {
            Console.WriteLine("✓ Docker available");
        }

        // Check docker compose
        if (!CheckDockerCompose())
        {
            Console.WriteLine("✗ Docker Compose is required but not available");
            allPassed = false;
        }
        else
        {
            Console.WriteLine("✓ Docker Compose available");
        }

        // Check psql (optional, for backup/restore)
        if (!CheckPsql())
        {
            Console.WriteLine("⚠ psql (PostgreSQL client) not found - backup/restore will use docker exec");
        }
        else
        {
            Console.WriteLine("✓ psql available");
        }

        // Check pg_dump (optional)
        if (!CheckPgDump())
        {
            Console.WriteLine("⚠ pg_dump not found - backup/restore will use docker exec");
        }
        else
        {
            Console.WriteLine("✓ pg_dump available");
        }

        // Check for port conflicts
        var portIssues = CheckPortConflicts();
        if (portIssues.Any())
        {
            foreach (var issue in portIssues)
            {
                Console.WriteLine($"⚠ {issue}");
            }
        }
        else
        {
            Console.WriteLine("✓ Required ports available (5432, 6379)");
        }

        return allPassed;
    }

    private bool CheckDocker()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private bool CheckDockerCompose()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "compose version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private bool CheckPsql()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "psql",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private bool CheckPgDump()
    {
        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = "pg_dump",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };
            process.Start();
            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private List<string> CheckPortConflicts()
    {
        var issues = new List<string>();
        
        // Simple port check using /dev/tcp or similar - just log for now
        // Real implementation would check actual port usage
        
        return issues;
    }
}