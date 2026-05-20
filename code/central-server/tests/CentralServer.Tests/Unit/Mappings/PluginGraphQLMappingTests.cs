namespace CentralServer.Tests.Unit.Mappings;

using CentralServer.Application.DTOs;
using CentralServer.Application.Monitoring;
using CentralServer.Domain.Models;
using CentralServer.Presentation.GraphQL.Mappings;

public class PluginGraphQLMappingTests
{
    [Fact]
    public void ToGraphQLType_WithDashboardJson_ExposesDashboardMetadata()
    {
        var plugin = new PluginDTO
        {
            Id = "PING",
            Name = "PING",
            Version = "1.0.0",
            Checksum = "checksum",
            DashboardJson = """{"panels":[]}""",
            ExecutionMode = PluginExecutionMode.Scheduled
        };

        var result = plugin.ToGraphQLType();

        Assert.True(result.HasDashboard);
        Assert.Equal(GrafanaDashboardConventions.BuildPluginDashboardUid(plugin.Id), result.DashboardUid);
    }

    [Fact]
    public void ToGraphQLType_WithoutDashboardJson_HidesDashboardMetadata()
    {
        var plugin = new PluginDTO
        {
            Id = "PING",
            Name = "PING",
            Version = "1.0.0",
            Checksum = "checksum",
            ExecutionMode = PluginExecutionMode.Scheduled
        };

        var result = plugin.ToGraphQLType();

        Assert.False(result.HasDashboard);
        Assert.Null(result.DashboardUid);
    }
}
