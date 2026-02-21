using Microsoft.EntityFrameworkCore;
using RAG_Doc.Application.DTOs;
using RAG_Doc.Application.Handlers;
using RAG_Doc.Domain.Interfaces;
using RAG_Doc.Infrastructure.Persistence;
using RAG_Doc.Infrastructure.Repositories;
using RAG_Doc.Infrastructure.Services;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.FluentValidation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DocHubConn")));
builder.Services.AddMemoryCache();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IRagService, RagService>();

builder.Services.Configure<LlmSettings>(builder.Configuration.GetSection("LLMS"));

// Register HttpClient for EmbeddingService 
builder.Services.AddHttpClient<EmbeddingService>(client =>
{
    var endpointUrl = builder.Configuration["LLMS:EndPointUrl"] ?? "http://localhost:1234";
    client.BaseAddress = new Uri(endpointUrl);

    var timeoutSeconds = builder.Configuration.GetValue<int>("LLMS:TimeoutSeconds", 600);
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});

builder.Services.AddHttpClient<LMStudioService>(client =>
{
    var endpointUrl = builder.Configuration["LLMS:EndPointUrl"] ?? "http://localhost:1234";
    client.BaseAddress = new Uri(endpointUrl);

    var timeoutSeconds = builder.Configuration.GetValue<int>("LLMS:TimeoutSeconds", 600);
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});

builder.Services.AddScoped<ILMStudioService, LMStudioService>();
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
// Wolverine configuration
builder.Host.UseWolverine(opts =>
{
    opts.UseEntityFrameworkCoreTransactions();
    opts.Discovery.IncludeAssembly(typeof(AppDbContext).Assembly);
    opts.Discovery.IncludeAssembly(typeof(DocumentHandler).Assembly);
    opts.UseFluentValidation();
    //opts.OptimizeArtifactWorkflow();
});

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
