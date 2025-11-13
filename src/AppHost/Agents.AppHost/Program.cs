var builder = DistributedApplication.CreateBuilder(args);

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
var notificationApi = builder.AddProject<Projects.Agents_API_Notification>("notification-api")
    .WithReference(sqlServer)
    .WithReference(redis)
    .WithEnvironment("Dapr__Enabled", "true")
    .WithEnvironment("ConnectionStrings__SqlServer", sqlServer)
    .WithEnvironment("LLMProvider__ProviderType", "Ollama")
    .WithEnvironment("LLMProvider__Ollama__Endpoint", "http://ollama:11434")
    .WithDaprSidecar("notification-api");

// Add DevOps Agent API with Dapr sidecar
var devopsApi = builder.AddProject<Projects.Agents_API_DevOps>("devops-api")
    .WithReference(redis)
    .WithEnvironment("Dapr__Enabled", "true")
    .WithEnvironment("LLMProvider__ProviderType", "Ollama")
    .WithEnvironment("LLMProvider__Ollama__Endpoint", "http://ollama:11434")
    .WithDaprSidecar("devops-api");

// Add TestPlanning Agent API with Dapr sidecar
var testplanningApi = builder.AddProject<Projects.Agents_API_TestPlanning>("testplanning-api")
    .WithReference(redis)
    .WithEnvironment("Dapr__Enabled", "true")
    .WithEnvironment("LLMProvider__ProviderType", "Ollama")
    .WithEnvironment("LLMProvider__Ollama__Endpoint", "http://ollama:11434")
    .WithDaprSidecar("testplanning-api");

// Add Implementation Agent API with Dapr sidecar
var implementationApi = builder.AddProject<Projects.Agents_API_Implementation>("implementation-api")
    .WithReference(redis)
    .WithEnvironment("Dapr__Enabled", "true")
    .WithEnvironment("LLMProvider__ProviderType", "Ollama")
    .WithEnvironment("LLMProvider__Ollama__Endpoint", "http://ollama:11434")
    .WithDaprSidecar("implementation-api");

// Add ServiceDesk Agent API with Dapr sidecar
var servicedeskApi = builder.AddProject<Projects.Agents_API_ServiceDesk>("servicedesk-api")
    .WithReference(redis)
    .WithEnvironment("Dapr__Enabled", "true")
    .WithEnvironment("LLMProvider__ProviderType", "Ollama")
    .WithEnvironment("LLMProvider__Ollama__Endpoint", "http://ollama:11434")
    .WithDaprSidecar("servicedesk-api");

builder.Build().Run();
