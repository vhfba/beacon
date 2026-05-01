namespace CentralServer.Presentation.GraphQL.Mappings;

using CentralServer.Application.DTOs;
using CentralServer.Domain.Models;
using CentralServer.Presentation.GraphQL.Types;

public static class GraphQLTypeMappings
{
    public static ProbeType ToGraphQLType(this ProbeDTO dto)
    {
        var status = Enum.TryParse<ProbeStatusType>(dto.Status, true, out var parsedStatus)
            ? parsedStatus
            : ProbeStatusType.Registered;

        return new ProbeType
        {
            Id = dto.Id,
            Name = dto.Name,
            Location = dto.Location,
            IpAddress = dto.IpAddress,
            Ssid = dto.Ssid,
            AgentVersion = dto.AgentVersion,
            Status = status,
            CreatedAt = dto.CreatedAt,
            LastHeartbeat = dto.LastHeartbeat,
            LastConfigFetch = dto.LastConfigFetch,
            LastMetricsPush = dto.LastMetricsPush,
            LastSeenAt = dto.LastSeenAt
        };
    }

    public static PluginType ToGraphQLType(this PluginDTO dto)
    {
        return new PluginType
        {
            Id = dto.Id,
            Name = dto.Name,
            Version = dto.Version,
            Checksum = dto.Checksum,
            Description = dto.Description,
            ReleasedAt = dto.ReleasedAt,
            Available = dto.Available,
            ExecutionMode = dto.ExecutionMode == PluginExecutionMode.Action
                ? PluginExecutionModeType.Action
                : PluginExecutionModeType.Scheduled,
            BundleUrl = dto.BundleUrl,
            BundleDownloadUrl = dto.BundleDownloadUrl,
            DashboardJson = dto.DashboardJson
        };
    }

    public static ProbeTestConfigType ToGraphQLType(this ProbeTestConfigurationDTO dto)
    {
        return new ProbeTestConfigType
        {
            ProbeId = dto.ProbeId,
            TestType = dto.TestType,
            IntervalSeconds = dto.IntervalSeconds,
            Enabled = dto.Enabled
        };
    }

    public static ProbeConfigType ToGraphQLType(this ProbeConfigDTO dto)
    {
        return new ProbeConfigType
        {
            ProbeId = dto.ProbeId,
            EnabledTests = dto.EnabledTests.Select(ToGraphQLType).ToList(),
            AvailablePlugins = dto.AvailablePlugins.Select(ToGraphQLType).ToList()
        };
    }

    public static ProbeRuntimeType ToGraphQLType(this ProbeRuntimeDTO dto)
    {
        return new ProbeRuntimeType
        {
            ProbeId = dto.ProbeId,
            Status = dto.Status,
            CanEmitMetrics = dto.CanEmitMetrics,
            EnabledTests = dto.EnabledTests,
            Site = dto.Site,
            IpAddress = dto.IpAddress,
            PolledAtUtc = dto.PolledAtUtc
        };
    }

    public static ProbePluginAssignmentType ToGraphQLType(this ProbePluginAssignmentDTO dto)
    {
        return new ProbePluginAssignmentType
        {
            ProbeId = dto.ProbeId,
            PluginId = dto.PluginId,
            PluginName = dto.PluginName,
            PluginVersion = dto.PluginVersion,
            PluginAvailable = dto.PluginAvailable,
            AssignedAt = dto.AssignedAt
        };
    }

    public static ProbeActionExecutionType ToGraphQLType(this ProbeActionExecutionDTO dto)
    {
        return new ProbeActionExecutionType
        {
            ExecutionId = dto.ExecutionId,
            ProbeId = dto.ProbeId,
            PluginId = dto.PluginId,
            TriggeredBy = dto.TriggeredBy,
            Status = dto.Status.ToGraphQLType(),
            RequestedAtUtc = dto.RequestedAtUtc,
            DeliveredAtUtc = dto.DeliveredAtUtc,
            StartedAtUtc = dto.StartedAtUtc,
            CompletedAtUtc = dto.CompletedAtUtc,
            ErrorMessage = dto.ErrorMessage
        };
    }

    public static ProbeActionExecutionStatusType ToGraphQLType(this ProbeActionExecutionStatus status)
    {
        return status switch
        {
            ProbeActionExecutionStatus.Queued => ProbeActionExecutionStatusType.Queued,
            ProbeActionExecutionStatus.Delivered => ProbeActionExecutionStatusType.Delivered,
            ProbeActionExecutionStatus.Running => ProbeActionExecutionStatusType.Running,
            ProbeActionExecutionStatus.Succeeded => ProbeActionExecutionStatusType.Succeeded,
            ProbeActionExecutionStatus.Failed => ProbeActionExecutionStatusType.Failed,
            ProbeActionExecutionStatus.TimedOut => ProbeActionExecutionStatusType.TimedOut,
            _ => ProbeActionExecutionStatusType.Failed
        };
    }
}
