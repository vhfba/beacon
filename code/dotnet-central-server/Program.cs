using CentralServer.Application.UseCases;
using CentralServer.Application.PluginDistribution;
using CentralServer.Domain.Repositories;
using CentralServer.Infrastructure.Persistence;
using CentralServer.Infrastructure.Persistence.Repositories;
using CentralServer.Presentation.GraphQL;
using CentralServer.Presentation.GraphQL.Security;
using CentralServer.Presentation.GraphQL.Responses;
using CentralServer.Presentation.GraphQL.Types;
using CentralServer.Presentation.Monitoring;
using CentralServer.Presentation.Plugins;
using CentralServer.Presentation.Probes;
using CentralServer.Presentation.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using IOPath = System.IO.Path;

var builder = WebApplication.CreateBuilder(args);
var configuredBundleDirectory = builder.Configuration["Plugins:BundleDirectory"];
var bundleDirectory = string.IsNullOrWhiteSpace(configuredBundleDirectory)
    ? IOPath.Combine(builder.Environment.ContentRootPath, PluginBundleConventions.DefaultBundleDirectory)
    : configuredBundleDirectory;

if (!IOPath.IsPathRooted(bundleDirectory))
{
    bundleDirectory = IOPath.GetFullPath(IOPath.Combine(builder.Environment.ContentRootPath, bundleDirectory));
}

Directory.CreateDirectory(bundleDirectory);

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

builder.Services.Configure<GraphQLSecurityOptions>(
    builder.Configuration.GetSection(GraphQLSecurityOptions.SectionName));
builder.Services.Configure<MonitoringOptions>(
    builder.Configuration.GetSection(MonitoringOptions.SectionName));
builder.Services.AddSingleton<ThresholdProfileStore>();
builder.Services.AddHttpClient<GrafanaDashboardSyncService>();

builder.Services
    .AddAuthentication(ApiKeyAuthenticationDefaults.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
        ApiKeyAuthenticationDefaults.SchemeName,
        _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
        policy.RequireAuthenticatedUser().RequireRole(AuthorizationPolicies.AdminRole));

    options.AddPolicy(AuthorizationPolicies.ProbeOrAdmin, policy =>
        policy.RequireAuthenticatedUser().RequireRole(AuthorizationPolicies.ProbeRole, AuthorizationPolicies.AdminRole));
});

builder.Services
    .AddGraphQLServer()
    .AddAuthorization()
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
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<GraphQLRequestHardeningMiddleware>();

app.MapGraphQL("/graphql")
    .RequireAuthorization(AuthorizationPolicies.ProbeOrAdmin);

app.MapGet("/", () => Results.Redirect("/beacon-simulator.html"));

app.MapMonitoringEndpoints();
app.MapProbeRuntimeEndpoints();
app.MapPluginBundleEndpoints(bundleDirectory);

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("Health")
    .WithOpenApi();

app.Run();
async Task SeedData(CentralServerDbContext db)
{
    await Task.CompletedTask;
}
