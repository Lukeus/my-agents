using Agents.Application.Core;
using Agents.Application.DevOps;
using Agents.Domain.Core.Interfaces;
using Agents.Infrastructure.LLM;
using Agents.Infrastructure.Prompts.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title = "DevOps Agent API", Version = "v1" }));
builder.Services.AddLLMProvider(builder.Configuration);
builder.Services.AddSingleton<IPromptLoader, PromptLoader>();
builder.Services.AddSingleton<IEventPublisher, MockEventPublisher>();
builder.Services.AddScoped<DevOpsAgent>();
builder.Services.AddHealthChecks();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();

public class MockEventPublisher : IEventPublisher
{
    public Task PublishAsync(Agents.Domain.Core.Events.IDomainEvent domainEvent, CancellationToken ct = default) => Task.CompletedTask;
    public Task PublishAsync(IEnumerable<Agents.Domain.Core.Events.IDomainEvent> events, CancellationToken ct = default) => Task.CompletedTask;
}
