# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Install Aspire workload (required by solution)
RUN dotnet workload install aspire --skip-manifest-update

# Copy solution and project files
COPY ["Agents.sln", "./"]
COPY ["src/Presentation/Agents.API.TestPlanning/Agents.API.TestPlanning.csproj", "src/Presentation/Agents.API.TestPlanning/"]
COPY ["src/Application/Agents.Application.TestPlanning/Agents.Application.TestPlanning.csproj", "src/Application/Agents.Application.TestPlanning/"]
COPY ["src/Application/Agents.Application.Core/Agents.Application.Core.csproj", "src/Application/Agents.Application.Core/"]
COPY ["src/Domain/Agents.Domain.TestPlanning/Agents.Domain.TestPlanning.csproj", "src/Domain/Agents.Domain.TestPlanning/"]
COPY ["src/Domain/Agents.Domain.Core/Agents.Domain.Core.csproj", "src/Domain/Agents.Domain.Core/"]
COPY ["src/Infrastructure/Agents.Infrastructure.LLM/Agents.Infrastructure.LLM.csproj", "src/Infrastructure/Agents.Infrastructure.LLM/"]
COPY ["src/Infrastructure/Agents.Infrastructure.Prompts/Agents.Infrastructure.Prompts.csproj", "src/Infrastructure/Agents.Infrastructure.Prompts/"]
COPY ["src/Infrastructure/Agents.Infrastructure.Dapr/Agents.Infrastructure.Dapr.csproj", "src/Infrastructure/Agents.Infrastructure.Dapr/"]
COPY ["src/Shared/Agents.Shared.Common/Agents.Shared.Common.csproj", "src/Shared/Agents.Shared.Common/"]
COPY ["src/Shared/Agents.ServiceDefaults/Agents.ServiceDefaults.csproj", "src/Shared/Agents.ServiceDefaults/"]

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build application
WORKDIR "/src/src/Presentation/Agents.API.TestPlanning"
RUN dotnet build "Agents.API.TestPlanning.csproj" -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish "Agents.API.TestPlanning.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app

# Install curl for health checks
RUN apk add --no-cache curl

# Create non-root user
RUN addgroup -S appuser && adduser -S appuser -G appuser

# Copy published application
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Environment variables
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0

# Labels
LABEL maintainer="AI Agents Team" \
      version="1.0.0" \
      description="AI Test Planning Agent API"

# Entry point
ENTRYPOINT ["dotnet", "Agents.API.TestPlanning.dll"]
