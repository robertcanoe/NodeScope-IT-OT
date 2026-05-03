using Microsoft.EntityFrameworkCore;
using NodeScope.Application;
using NodeScope.Application.Abstractions.Data;
using NodeScope.Infrastructure;
using NodeScope.Infrastructure.Data;
using NodeScope.Infrastructure.Processing;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddApplication();

var connectionString =
    builder.Configuration.GetConnectionString("Database")
        ?? throw new InvalidOperationException("Connection string 'Database' is not configured.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddScoped<INodeScopeDbContext>(static provider => provider.GetRequiredService<AppDbContext>());
builder.Services.AddNodeScopeInfrastructureServices(includeProcessingHostedService: false);
builder.Services.AddHostedService<PendingImportOrchestrationHostedService>();

var host = builder.Build();
host.Run();
