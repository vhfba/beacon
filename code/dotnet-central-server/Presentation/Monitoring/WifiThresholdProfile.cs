namespace CentralServer.Presentation.Monitoring;

public sealed class WifiThresholdProfile
{
    public double RssiYellowDbm { get; init; } = -70;

    public double RssiGreenDbm { get; init; } = -60;

    public double SnrYellowDb { get; init; } = 20;

    public double SnrGreenDb { get; init; } = 30;

    public double LinkQualityYellowPercent { get; init; } = 60;

    public double LinkQualityGreenPercent { get; init; } = 80;

    public double PingLossWarnPercent { get; init; } = 2;

    public double PingLossCriticalPercent { get; init; } = 5;

    public double IperfDownloadWarnMbps { get; init; } = 100;

    public double IperfDownloadGoodMbps { get; init; } = 200;

    public double IperfUploadWarnMbps { get; init; } = 60;

    public double IperfUploadGoodMbps { get; init; } = 120;

    public static WifiThresholdProfile Default() => new();
}
