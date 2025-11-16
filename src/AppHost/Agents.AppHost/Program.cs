var builder = DistributedApplication.CreateBuilder(args);

// Get absolute path to dapr-components directory
var componentsPath = Path.Combine(AppContext.BaseDirectory, "dapr-components");

// Add infrastructure resources
var sqlServer = builder.AddSqlServer("sqlserver")
    .WithDataVolume("agents-sqlserver-data")
    .AddDatabase("agentsdb");

var redis = builder.AddRedis("redis")
    .WithDataVolume("agents-redis-data");

// Add Ollama for local LLM
var ollama = builder.AddContainer("ollama", "ollama/ollama", "latest")
    .WithHttpEndpoint(11434, 11434, "http")
    .WithBindMount("ollama-data", "/root/.ollama");

// Add Notification Agent API with Dapr sidecar
var notificationApi = builder.AddProject("notification-api", "../../Presentation/Agents.API.Notification/Agents.API.Notification.csproj")
    .WithReference(sqlServer)
    .WithReference(redis)
    .WithEnvironment("Dapr__Enabled", "true")
    .WithEnvironment("ConnectionStrings__SqlServer", sqlServer)
    .WithEnvironment("LLMProvider__ProviderType", "Ollama")
    .WithEnvironment("LLMProvider__Ollama__Endpoint", "http://ollama:11434")
    .WithDaprSidecar("notification-api");

// Add DevOps Agent API with Dapr sidecar
var devopsApi = builder.AddProject("devops-api", "../../Presentation/Agents.API.DevOps/Agents.API.DevOps.csproj")
    .WithReference(redis)
    .WithEnvironment("Dapr__Enabled", "true")
    .WithEnvironment("LLMProvider__ProviderType", "Ollama")
    .WithEnvironment("LLMProvider__Ollama__Endpoint", "http://ollama:11434")
    .WithDaprSidecar("devops-api");

// Add TestPlanning Agent API with Dapr sidecar
var testplanningApi = builder.AddProject("testplanning-api", "../../Presentation/Agents.API.TestPlanning/Agents.API.TestPlanning.csproj")
    .WithReference(redis)
    .WithEnvironment("Dapr__Enabled", "true")
    .WithEnvironment("LLMProvider__ProviderType", "Ollama")
    .WithEnvironment("LLMProvider__Ollama__Endpoint", "http://ollama:11434")
    .WithDaprSidecar("testplanning-api");

// Add Implementation Agent API with Dapr sidecar
var implementationApi = builder.AddProject("implementation-api", "../../Presentation/Agents.API.Implementation/Agents.API.Implementation.csproj")
    .WithReference(redis)
    .WithEnvironment("Dapr__Enabled", "true")
    .WithEnvironment("LLMProvider__ProviderType", "Ollama")
    .WithEnvironment("LLMProvider__Ollama__Endpoint", "http://ollama:11434")
    .WithDaprSidecar("implementation-api");

// Add ServiceDesk Agent API with Dapr sidecar
var servicedeskApi = builder.AddProject("servicedesk-api", "../../Presentation/Agents.API.ServiceDesk/Agents.API.ServiceDesk.csproj")
    .WithReference(redis)
    .WithEnvironment("Dapr__Enabled", "true")
    .WithEnvironment("LLMProvider__ProviderType", "Ollama")
    .WithEnvironment("LLMProvider__Ollama__Endpoint", "http://ollama:11434")
    .WithDaprSidecar("servicedesk-api");

// Add BIM Classification Agent API with Dapr sidecar
var bimclassificationApi = builder.AddProject("bimclassification-api", "../../Presentation/Agents.API.BimClassification/Agents.API.BimClassification.csproj")
    .WithReference(sqlServer)
    .WithReference(redis)
    .WithEnvironment("Dapr__Enabled", "true")
    .WithEnvironment("ConnectionStrings__SqlServer", sqlServer)
    .WithEnvironment("LLMProvider__ProviderType", "Ollama")
    .WithEnvironment("LLMProvider__Ollama__Endpoint", "http://ollama:11434")
    .WithDaprSidecar("bimclassification-api");

builder.Build().Run();
