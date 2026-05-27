using System.Diagnostics;

namespace CentralServer.Application;

public static class Diagnostics
{
    public static readonly ActivitySource ActivitySource = new("Beacon.CentralServer");
}
