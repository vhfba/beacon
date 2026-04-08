using CentralServer.Application.UseCases;
using CentralServer.Domain.Repositories;
using CentralServer.Infrastructure.Persistence;
using CentralServer.Infrastructure.Persistence.Repositories;
using CentralServer.Presentation.GraphQL;
using CentralServer.Presentation.GraphQL.Responses;
using CentralServer.Presentation.GraphQL.Types;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var databaseProvider = builder.Configuration["Database:Provider"]?.Trim().ToLowerInvariant();
builder.Services.AddDbContext<CentralServerDbContext>(options =>
{
    if (databaseProvider == "inmemory" || databaseProvider == "h2")
    {
        var databaseName = builder.Configuration["Database:InMemoryName"] ?? "beacon_central_dev";
        options.UseInMemoryDatabase(databaseName);
        return;
    }

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    options.UseNpgsql(connectionString);
});
builder.Services.AddScoped<IProbeRepository, ProbeRepositoryAdapter>();
builder.Services.AddScoped<ITestTypeRepository, TestTypeRepositoryAdapter>();
builder.Services.AddScoped<IProbeTestConfigurationRepository, ProbeTestConfigurationRepositoryAdapter>();
builder.Services.AddScoped<IPluginRepository, PluginRepositoryAdapter>();
builder.Services.AddScoped<RegisterProbeUseCase>();
builder.Services.AddScoped<GetFleetStatusUseCase>();
builder.Services.AddScoped<GetProbeConfigUseCase>();
builder.Services.AddScoped<UpdateProbeTestConfigUseCase>();
builder.Services.AddScoped<UpdateProbeStatusUseCase>();
builder.Services.AddScoped<ListPluginsUseCase>();
builder.Services.AddScoped<RegisterPluginUseCase>();
builder.Services.AddScoped<GetPluginByIdUseCase>();
builder.Services.AddScoped<SetProbeTestEnabledUseCase>();
builder.Services.AddScoped<SetPluginAvailabilityUseCase>();
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddType<ProbeType>()
    .AddType<ProbeStatusType>()
    .AddType<ProbeTestConfigType>()
    .AddType<ProbeConfigType>()
    .AddType<PluginType>()
    .AddType<RegisterProbeInputType>()
    .AddType<RegisterPluginInputType>()
    .AddType<UpdateProbeTestConfigInputType>()
    .AddType<SetProbeTestEnabledInputType>()
    .AddType<SetPluginAvailabilityInputType>()
    .AddType<FleetStatusResponse>()
    .AddType<RegisterProbeResponse>()
    .AddType<RegisterPluginResponse>()
    .AddType<UpdateProbeTestConfigResponse>()
    .AddType<UpdateProbeStatusResponse>()
    .AddType<SetProbeTestEnabledResponse>()
    .AddType<SetPluginAvailabilityResponse>()
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = builder.Environment.IsDevelopment());
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});
builder.Services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConsole();
    if (builder.Environment.IsDevelopment())
    {
        config.AddDebug();
    }
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CentralServerDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }
    await SeedData(db);
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseCors();
app.MapGraphQL("/graphql");
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("Health")
    .WithOpenApi();

app.Run();
async Task SeedData(CentralServerDbContext db)
{
    await Task.CompletedTask;
}
