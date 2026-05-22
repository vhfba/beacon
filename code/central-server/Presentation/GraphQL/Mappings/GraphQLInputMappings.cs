namespace CentralServer.Presentation.GraphQL.Mappings;

using CentralServer.Application.DTOs;
using CentralServer.Domain.Models;
using CentralServer.Presentation.GraphQL.Types;

public static class GraphQLInputMappings
{
    public static ProbeHeartbeatInput ToDTO(this ProbeHeartbeatInputType input)
    {
        return new ProbeHeartbeatInput
        {
            ProbeId = input.ProbeId,
            Name = input.Name,
            Location = input.Location,
            IpAddress = input.IpAddress,
            Ssid = input.Ssid,
            AgentVersion = input.AgentVersion
        };
    }

    public static UpdateProbeTestConfigInput ToDTO(this UpdateProbeTestConfigInputType input)
    {
        return new UpdateProbeTestConfigInput
        {
            ProbeId = input.ProbeId,
            TestType = input.TestType,
            IntervalSeconds = input.IntervalSeconds,
            Enabled = input.Enabled
        };
    }

    public static SetProbeTestEnabledInput ToDTO(this SetProbeTestEnabledInputType input)
    {
        return new SetProbeTestEnabledInput
        {
            ProbeId = input.ProbeId,
            TestType = input.TestType,
            Enabled = input.Enabled
        };
    }

    public static SetProbePluginsInput ToDTO(this SetProbePluginsInputType input)
    {
        return new SetProbePluginsInput
        {
            ProbeId = input.ProbeId,
            PluginIds = input.PluginIds
        };
    }

    public static RegisterPluginInput ToDTO(this RegisterPluginInputType input)
    {
        return new RegisterPluginInput
        {
            Id = input.Id,
            Name = input.Name,
            Version = input.Version,
            Checksum = input.Checksum,
            Description = input.Description,
            BundleDownloadUrl = input.BundleDownloadUrl,
            DashboardJson = input.DashboardJson,
            ExecutionMode = input.ExecutionMode == PluginExecutionModeType.Action
                ? PluginExecutionMode.Action
                : PluginExecutionMode.Scheduled
        };
    }

    public static UpdatePluginInput ToDTO(this UpdatePluginInputType input)
    {
        return new UpdatePluginInput
        {
            CurrentId = input.CurrentId,
            Id = input.Id,
            Name = input.Name,
            Version = input.Version,
            Checksum = input.Checksum,
            Description = input.Description,
            BundleDownloadUrl = input.BundleDownloadUrl,
            DashboardJson = input.DashboardJson,
            ExecutionMode = input.ExecutionMode == PluginExecutionModeType.Action
                ? PluginExecutionMode.Action
                : PluginExecutionMode.Scheduled
        };
    }

    public static SetPluginAvailabilityInput ToDTO(this SetPluginAvailabilityInputType input)
    {
        return new SetPluginAvailabilityInput
        {
            PluginId = input.PluginId,
            Available = input.Available
        };
    }

    public static ReportProbeMetricsInput ToDTO(this ReportProbeMetricsInputType input)
    {
        return new ReportProbeMetricsInput
        {
            ProbeId = input.ProbeId,
            Samples = input.Samples.Select(sample => sample.ToDTO()).ToList()
        };
    }

    public static MetricSampleInput ToDTO(this MetricSampleInputType input)
    {
        return new MetricSampleInput
        {
            Name = input.Name,
            Kind = input.Kind,
            Value = input.Value,
            TimestampUtc = input.TimestampUtc,
            Labels = input.Labels
                .Where(label => !string.IsNullOrWhiteSpace(label.Key))
                .ToDictionary(label => label.Key, label => label.Value ?? string.Empty, StringComparer.Ordinal)
        };
    }

    public static TriggerProbeActionInput ToDTO(this TriggerProbeActionInputType input)
    {
        return new TriggerProbeActionInput
        {
            ProbeId = input.ProbeId,
            PluginId = input.PluginId,
            TriggeredBy = input.TriggeredBy
        };
    }

    public static UpdateProbeActionStatusInput ToDTO(this UpdateProbeActionStatusInputType input)
    {
        var normalizedStatus = input.Status.Replace("_", string.Empty, StringComparison.Ordinal);
        if (!Enum.TryParse<ProbeActionExecutionStatus>(normalizedStatus, true, out var parsedStatus))
        {
            throw new DomainException($"Invalid action status '{input.Status}'.");
        }

        return new UpdateProbeActionStatusInput
        {
            ProbeId = input.ProbeId,
            ExecutionId = input.ExecutionId,
            Status = parsedStatus,
            ErrorMessage = input.ErrorMessage
        };
    }

    public static UpdateProbeProfileInput ToDTO(this UpdateProbeProfileInputType input)
    {
        return new UpdateProbeProfileInput
        {
            ProbeId = input.ProbeId,
            Name = input.Name,
            Location = input.Location,
            RequestedBy = input.RequestedBy
        };
    }

    public static RequestWifiScanInput ToDTO(this RequestWifiScanInputType input)
    {
        return new RequestWifiScanInput
        {
            ProbeId = input.ProbeId,
            RequestedBy = input.RequestedBy
        };
    }

    public static RequestWifiConnectInput ToDTO(this RequestWifiConnectInputType input)
    {
        return new RequestWifiConnectInput
        {
            ProbeId = input.ProbeId,
            Ssid = input.Ssid,
            Password = input.Password,
            RequestedBy = input.RequestedBy
        };
    }

    public static UpdateProbeControlCommandStatusInput ToDTO(this UpdateProbeControlCommandStatusInputType input)
    {
        var normalizedStatus = input.Status.Replace("_", string.Empty, StringComparison.Ordinal);
        if (!Enum.TryParse<ProbeControlCommandStatus>(normalizedStatus, true, out var parsedStatus))
        {
            throw new DomainException($"Invalid probe control command status '{input.Status}'.");
        }

        return new UpdateProbeControlCommandStatusInput
        {
            ProbeId = input.ProbeId,
            CommandId = input.CommandId,
            Status = parsedStatus,
            ResultJson = input.ResultJson,
            ErrorMessage = input.ErrorMessage
        };
    }
}
