using CentralServer.Application.DependencyInjection;
using CentralServer.Application.PluginDistribution;
using CentralServer.Infrastructure.DependencyInjection;
using CentralServer.Presentation.DependencyInjection;
using CentralServer.Presentation.GraphQL;
using CentralServer.Presentation.GraphQL.Security;
using CentralServer.Presentation.Monitoring;
using CentralServer.Presentation.Plugins;
using CentralServer.Presentation.Security;

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
var bundleDirectory = PluginBundleDirectoryResolver.Resolve(
    builder.Configuration["Plugins:BundleDirectory"],
    builder.Environment.ContentRootPath);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("Beacon.CentralServer"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter();
    });

builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddMetricsServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddPresentationMonitoring(builder.Configuration);

builder.Services.Configure<GraphQLSecurityOptions>(
    builder.Configuration.GetSection(GraphQLSecurityOptions.SectionName));
builder.Services.AddPresentationSecurity();
builder.Services.AddPresentationGraphQL()
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
    config.AddJsonConsole(options => 
    {
        options.IncludeScopes = true;
        options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
        options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
        {
            Indented = false
        };
    });

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
await app.InitializePersistenceAsync();

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
app.MapPluginBundleEndpoints(bundleDirectory);

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("Health")
    .WithOpenApi();

app.Run();

public partial class Program;
