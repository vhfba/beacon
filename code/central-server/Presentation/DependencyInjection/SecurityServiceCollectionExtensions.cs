namespace CentralServer.Presentation.DependencyInjection;

using CentralServer.Presentation.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

public static class SecurityServiceCollectionExtensions
{
    public static IServiceCollection AddPresentationSecurity(this IServiceCollection services)
    {
        services
            .AddAuthentication(ApiKeyAuthenticationDefaults.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationDefaults.SchemeName,
                _ => { });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
                policy.RequireAuthenticatedUser().RequireRole(AuthorizationPolicies.AdminRole));

            options.AddPolicy(AuthorizationPolicies.ProbeOrAdmin, policy =>
                policy.RequireAuthenticatedUser().RequireRole(AuthorizationPolicies.ProbeRole, AuthorizationPolicies.AdminRole));
        });

        return services;
    }
}
