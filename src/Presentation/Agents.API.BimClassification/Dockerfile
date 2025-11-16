# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Agents.sln", "./"]
COPY ["src/Presentation/Agents.API.BimClassification/Agents.API.BimClassification.csproj", "src/Presentation/Agents.API.BimClassification/"]
COPY ["src/Application/Agents.Application.BimClassification/Agents.Application.BimClassification.csproj", "src/Application/Agents.Application.BimClassification/"]
COPY ["src/Application/Agents.Application.Core/Agents.Application.Core.csproj", "src/Application/Agents.Application.Core/"]
COPY ["src/Domain/Agents.Domain.BimClassification/Agents.Domain.BimClassification.csproj", "src/Domain/Agents.Domain.BimClassification/"]
COPY ["src/Domain/Agents.Domain.Core/Agents.Domain.Core.csproj", "src/Domain/Agents.Domain.Core/"]
COPY ["src/Infrastructure/Agents.Infrastructure.LLM/Agents.Infrastructure.LLM.csproj", "src/Infrastructure/Agents.Infrastructure.LLM/"]
COPY ["src/Infrastructure/Agents.Infrastructure.Prompts/Agents.Infrastructure.Prompts.csproj", "src/Infrastructure/Agents.Infrastructure.Prompts/"]
COPY ["src/Infrastructure/Agents.Infrastructure.Persistence.SqlServer/Agents.Infrastructure.Persistence.SqlServer.csproj", "src/Infrastructure/Agents.Infrastructure.Persistence.SqlServer/"]
COPY ["src/Shared/Agents.Shared.Common/Agents.Shared.Common.csproj", "src/Shared/Agents.Shared.Common/"]
COPY ["src/Infrastructure/Agents.Infrastructure.Dapr/Agents.Infrastructure.Dapr.csproj", "src/Infrastructure/Agents.Infrastructure.Dapr/"]
COPY ["src/Shared/Agents.ServiceDefaults/Agents.ServiceDefaults.csproj", "src/Shared/Agents.ServiceDefaults/"]

# Restore dependencies
RUN dotnet restore

# Copy source code
COPY . .

# Build application
WORKDIR "/src/src/Presentation/Agents.API.BimClassification"
RUN dotnet build "Agents.API.BimClassification.csproj" -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish "Agents.API.BimClassification.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published application
COPY --from=publish /app/publish .

# Set ownership
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose port
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "Agents.API.BimClassification.dll"]
