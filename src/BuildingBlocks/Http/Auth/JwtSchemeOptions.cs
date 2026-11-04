namespace BuildingBlocks.Http.Auth;

public sealed class JwtSchemeOptions
{
    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string? JwksUrl { get; set; }

    public string? PublicKeyPem { get; set; }

    public bool RequireHttpsMetadata { get; set; } = true;
}

public sealed class JwtAuthenticationOptions
{
    public const string SectionName = "Jwt";

    public JwtSchemeOptions Funcionario { get; set; } = new();

    public JwtSchemeOptions Cliente { get; set; } = new();
}
