# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the .csproj manifest first (for layer caching of restore)
COPY docker/backend/GymFlow.WebAPI.csproj ./GymFlow.WebAPI.csproj
RUN dotnet restore GymFlow.WebAPI.csproj

# Copy the rest of the source code
COPY src/backend/ ./

# Publish
RUN dotnet publish GymFlow.WebAPI.csproj -c Release --no-restore -o /out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "GymFlow.WebAPI.dll"]