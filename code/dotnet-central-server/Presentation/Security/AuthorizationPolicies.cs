namespace CentralServer.Presentation.Security;

public static class AuthorizationPolicies
{
    public const string AdminRole = "Admin";
    public const string ProbeRole = "Probe";

    public const string AdminOnly = "AdminOnly";
    public const string ProbeOrAdmin = "ProbeOrAdmin";
}
