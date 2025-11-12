# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["Agents.sln", "./"]
COPY ["src/Presentation/Agents.API.Notification/Agents.API.Notification.csproj", "src/Presentation/Agents.API.Notification/"]
COPY ["src/Application/Agents.Application.Notification/Agents.Application.Notification.csproj", "src/Application/Agents.Application.Notification/"]
COPY ["src/Application/Agents.Application.Core/Agents.Application.Core.csproj", "src/Application/Agents.Application.Core/"]
COPY ["src/Domain/Agents.Domain.Notification/Agents.Domain.Notification.csproj", "src/Domain/Agents.Domain.Notification/"]
COPY ["src/Domain/Agents.Domain.Core/Agents.Domain.Core.csproj", "src/Domain/Agents.Domain.Core/"]
COPY ["src/Infrastructure/Agents.Infrastructure.LLM/Agents.Infrastructure.LLM.csproj", "src/Infrastructure/Agents.Infrastructure.LLM/"]
COPY ["src/Infrastructure/Agents.Infrastructure.Prompts/Agents.Infrastructure.Prompts.csproj", "src/Infrastructure/Agents.Infrastructure.Prompts/"]
COPY ["src/Infrastructure/Agents.Infrastructure.Persistence.SqlServer/Agents.Infrastructure.Persistence.SqlServer.csproj", "src/Infrastructure/Agents.Infrastructure.Persistence.SqlServer/"]
COPY ["src/Shared/Agents.Shared.Common/Agents.Shared.Common.csproj", "src/Shared/Agents.Shared.Common/"]

# Restore dependencies
RUN dotnet restore "src/Presentation/Agents.API.Notification/Agents.API.Notification.csproj"

# Copy source code
COPY . .

# Build application
WORKDIR "/src/src/Presentation/Agents.API.Notification"
RUN dotnet build "Agents.API.Notification.csproj" -c Release -o /app/build --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish "Agents.API.Notification.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

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
ENTRYPOINT ["dotnet", "Agents.API.Notification.dll"]
