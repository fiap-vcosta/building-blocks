using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace BuildingBlocks.Http.Auth;

public static class JwtAuthenticationExtensions
{
    public static IServiceCollection AddBuildingBlocksJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(JwtAuthenticationOptions.SectionName)
            .Get<JwtAuthenticationOptions>() ?? new JwtAuthenticationOptions();

        return services.AddBuildingBlocksJwtAuthentication(options);
    }

    public static IServiceCollection AddBuildingBlocksJwtAuthentication(
        this IServiceCollection services,
        Action<JwtAuthenticationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new JwtAuthenticationOptions();
        configure(options);
        return services.AddBuildingBlocksJwtAuthentication(options);
    }

    public static IServiceCollection AddBuildingBlocksJwtAuthentication(
        this IServiceCollection services,
        JwtAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        JwtKeyMaterial.ValidateScheme(nameof(options.Funcionario), options.Funcionario);
        JwtKeyMaterial.ValidateScheme(nameof(options.Cliente), options.Cliente);

        services.AddAuthentication(auth =>
            {
                auth.DefaultAuthenticateScheme = AuthSchemes.Funcionario;
                auth.DefaultChallengeScheme = AuthSchemes.Funcionario;
            })
            .AddJwtBearer(AuthSchemes.Funcionario, bearer => ConfigureBearer(bearer, options.Funcionario))
            .AddJwtBearer(AuthSchemes.Cliente, bearer => ConfigureBearer(bearer, options.Cliente));

        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.FuncionarioJwt, policy =>
            {
                policy.AddAuthenticationSchemes(AuthSchemes.Funcionario);
                policy.RequireAuthenticatedUser();
            })
            .AddPolicy(AuthPolicies.ClienteJwt, policy =>
            {
                policy.AddAuthenticationSchemes(AuthSchemes.Cliente);
                policy.RequireAuthenticatedUser();
                policy.RequireClaim(ClienteJwtClaims.Documento);
            });

        return services;
    }

    private static void ConfigureBearer(JwtBearerOptions bearer, JwtSchemeOptions scheme)
    {
        bearer.RequireHttpsMetadata = scheme.RequireHttpsMetadata;
        bearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = scheme.Issuer,
            ValidAudience = scheme.Audience,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        if (!string.IsNullOrWhiteSpace(scheme.PublicKeyPem))
        {
            bearer.TokenValidationParameters.IssuerSigningKey =
                JwtKeyMaterial.CreateRsaSecurityKey(scheme.PublicKeyPem);
            return;
        }

        bearer.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            scheme.JwksUrl!,
            new JwksConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = scheme.RequireHttpsMetadata })
        {
            AutomaticRefreshInterval = TimeSpan.FromHours(12),
            RefreshInterval = TimeSpan.FromMinutes(5)
        };
    }
}
