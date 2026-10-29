# Multi-stage Dockerfile for Keyloop Unified Service Scheduler (.NET 10 Web API)

# Stage 1: Build & Test
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for efficient caching
COPY Keyloop.Scheduler.sln ./
COPY src/Keyloop.Scheduler.Domain/Keyloop.Scheduler.Domain.csproj src/Keyloop.Scheduler.Domain/
COPY src/Keyloop.Scheduler.Core/Keyloop.Scheduler.Core.csproj src/Keyloop.Scheduler.Core/
COPY src/Keyloop.Scheduler.Infrastructure/Keyloop.Scheduler.Infrastructure.csproj src/Keyloop.Scheduler.Infrastructure/
COPY src/Keyloop.Scheduler.Api/Keyloop.Scheduler.Api.csproj src/Keyloop.Scheduler.Api/
COPY tests/Keyloop.Scheduler.Tests/Keyloop.Scheduler.Tests.csproj tests/Keyloop.Scheduler.Tests/

# Restore dependencies
RUN dotnet restore Keyloop.Scheduler.sln

# Copy the rest of the source code
COPY . .

# Build the entire solution
RUN dotnet build Keyloop.Scheduler.sln -c Release --no-restore

# Run automated tests during Docker build to enforce quality gate
RUN dotnet test tests/Keyloop.Scheduler.Tests/Keyloop.Scheduler.Tests.csproj -c Release --no-build --verbosity normal

# Publish the API
RUN dotnet publish src/Keyloop.Scheduler.Api/Keyloop.Scheduler.Api.csproj -c Release -o /app/publish --no-build

# Stage 2: Production Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create directory for persistent SQLite storage
RUN mkdir -p /app/data

# Copy published application from build stage
COPY --from=build /app/publish .

# Environment configuration
ENV ASPNETCORE_URLS=http://+:5000
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/scheduler.db;Cache=Shared"
ENV ASPNETCORE_ENVIRONMENT=Development

EXPOSE 5000

ENTRYPOINT ["dotnet", "Keyloop.Scheduler.Api.dll"]
