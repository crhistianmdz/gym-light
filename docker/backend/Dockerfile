# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy source code
COPY src/backend/ ./

# Create dynamic .csproj for building
RUN echo "<Project Sdk=\"Microsoft.NET.Sdk.Web\">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>GymFlow.WebAPI</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=\"Microsoft.EntityFrameworkCore\" Version=\"8.0.0\" />
    <PackageReference Include=\"Npgsql.EntityFrameworkCore.PostgreSQL\" Version=\"8.0.0\" />
    <PackageReference Include=\"Microsoft.AspNetCore.Authentication.JwtBearer\" Version=\"8.0.0\" />
    <PackageReference Include=\"StackExchange.Redis\" Version=\"2.7.33\" />
    <PackageReference Include=\"FluentValidation.AspNetCore\" Version=\"11.3.0\" />
    <PackageReference Include=\"Microsoft.EntityFrameworkCore.Design\" Version=\"8.0.0\" />
  </ItemGroup>
</Project>" > GymFlow.WebAPI.csproj

# Restore and publish
RUN dotnet restore GymFlow.WebAPI.csproj
RUN dotnet publish GymFlow.WebAPI.csproj -c Release -o /out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "GymFlow.WebAPI.dll"]