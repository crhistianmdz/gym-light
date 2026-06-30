using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GymFlow.Cli.Commands.Helpers;

/// <summary>
/// Generates docker-compose.yml based on the template
/// </summary>
public class DockerComposeGenerator
{
    private readonly string _templatePath;

    public DockerComposeGenerator()
    {
        _templatePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "docker",
            "docker-compose.yml"
        );
    }

    public void Generate(string instanceName, string publicUrl)
    {
        var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "docker-compose.yml");

        if (!File.Exists(_templatePath))
        {
            throw new FileNotFoundException($"Template not found: {_templatePath}");
        }

        // Read template
        var template = File.ReadAllText(_templatePath);

        // For now, copy the template as-is (future: customize for instance)
        File.Copy(_templatePath, outputPath, overwrite: true);

        Console.WriteLine($"Generated: {outputPath}");
    }

    public string GenerateYaml(string instanceName, string publicUrl)
    {
        // Generate customized docker-compose.yml
        return $@"services:
  postgres:
    image: postgres:16-alpine
    volumes:
      - postgres-data:/var/lib/postgresql/data
    ports:
      - ""5432:5432""
    environment:
      POSTGRES_DB: gymflow_{SanitizeName(instanceName)}
      POSTGRES_USER: gymflow
      POSTGRES_PASSWORD: {{GENERATED_PASSWORD}}
    networks:
      - gymflow-network
    healthcheck:
      test: [""CMD-SHELL"", ""pg_isready -U gymflow -d gymflow_{SanitizeName(instanceName)}""]
      interval: 10s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    ports:
      - ""6379:6379""
    networks:
      - gymflow-network
    healthcheck:
      test: [""CMD"", ""redis-cli"", ""ping""]
      interval: 10s
      timeout: 5s
      retries: 5

  backend:
    build:
      context: .
      dockerfile: docker/backend/Dockerfile
    ports:
      - ""5000:8080""
    depends_on:
      postgres:
        condition: service_healthy
      redis:
        condition: service_healthy
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Database=gymflow_{SanitizeName(instanceName)};Username=gymflow;Password={{GENERATED_PASSWORD}}
      - Redis__Connection=redis:6379
      - Jwt__Secret={{GENERATED_JWT_SECRET}}
      - Jwt__Issuer={publicUrl}
      - Jwt__Audience={publicUrl}
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:8080
    networks:
      - gymflow-network

  frontend:
    build:
      context: .
      dockerfile: docker/frontend/Dockerfile
    ports:
      - ""3000:3000""
    depends_on:
      - backend
    networks:
      - gymflow-network

volumes:
  postgres-data:

networks:
  gymflow-network:
    driver: bridge
";
    }

    private static string SanitizeName(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");
    }
}