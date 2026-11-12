using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using BuildingBlocks.Http.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace UnitTests.Http.Auth;

public class JwtAuthenticationExtensionsTests
{
    [Fact]
    public void AddBuildingBlocksJwtAuthentication_WithPem_ConfiguresSchemesAndPolicies()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();
        var services = new ServiceCollection();

        // Act
        services.AddLogging();
        services.AddBuildingBlocksJwtAuthentication(options =>
        {
            options.Funcionario.Issuer = "issuer-staff";
            options.Funcionario.Audience = "audience-staff";
            options.Funcionario.PublicKeyPem = publicPem;
            options.Funcionario.RequireHttpsMetadata = false;

            options.Cliente.Issuer = "issuer-cliente";
            options.Cliente.Audience = "audience-cliente";
            options.Cliente.PublicKeyPem = publicPem;
            options.Cliente.RequireHttpsMetadata = false;
        });
        using var provider = services.BuildServiceProvider();

        // Assert
        var funcionario = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(AuthSchemes.Funcionario);
        Assert.Equal("issuer-staff", funcionario.TokenValidationParameters.ValidIssuer);
        Assert.NotNull(funcionario.TokenValidationParameters.IssuerSigningKey);

        var cliente = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(AuthSchemes.Cliente);
        Assert.Equal("issuer-cliente", cliente.TokenValidationParameters.ValidIssuer);

        var authOptions = provider.GetRequiredService<IOptions<Microsoft.AspNetCore.Authorization.AuthorizationOptions>>().Value;
        Assert.NotNull(authOptions.GetPolicy(AuthPolicies.ClienteJwt));
        Assert.NotNull(authOptions.GetPolicy(AuthPolicies.FuncionarioJwt));
    }

    [Fact]
    public void AddBuildingBlocksJwtAuthentication_FromConfiguration_ReadsJwtSection()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Funcionario:Issuer"] = "cfg-staff",
                ["Jwt:Funcionario:Audience"] = "aud-staff",
                ["Jwt:Funcionario:PublicKeyPem"] = publicPem,
                ["Jwt:Funcionario:RequireHttpsMetadata"] = "false",
                ["Jwt:Cliente:Issuer"] = "cfg-cliente",
                ["Jwt:Cliente:Audience"] = "aud-cliente",
                ["Jwt:Cliente:PublicKeyPem"] = publicPem,
                ["Jwt:Cliente:RequireHttpsMetadata"] = "false"
            })
            .Build();
        var services = new ServiceCollection();

        // Act
        services.AddLogging();
        services.AddBuildingBlocksJwtAuthentication(configuration);
        using var provider = services.BuildServiceProvider();

        // Assert
        var funcionario = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(AuthSchemes.Funcionario);
        Assert.Equal("cfg-staff", funcionario.TokenValidationParameters.ValidIssuer);
    }

    [Fact]
    public void AddBuildingBlocksJwtAuthentication_WithJwksUrl_SetsConfigurationManager()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddLogging();
        services.AddBuildingBlocksJwtAuthentication(options =>
        {
            options.Funcionario.Issuer = "issuer-staff";
            options.Funcionario.Audience = "audience-staff";
            options.Funcionario.JwksUrl = "http://localhost/.well-known/jwks.json";
            options.Funcionario.RequireHttpsMetadata = false;

            options.Cliente.Issuer = "issuer-cliente";
            options.Cliente.Audience = "audience-cliente";
            options.Cliente.JwksUrl = "http://localhost/.well-known/jwks.json";
            options.Cliente.RequireHttpsMetadata = false;
        });
        using var provider = services.BuildServiceProvider();

        // Assert
        var funcionario = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(AuthSchemes.Funcionario);
        Assert.NotNull(funcionario.ConfigurationManager);
        Assert.Null(funcionario.TokenValidationParameters.IssuerSigningKey);
    }

    [Fact]
    public void AddBuildingBlocksJwtAuthentication_Throws_WhenBothPemAndJwksConfigured()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() =>
            services.AddBuildingBlocksJwtAuthentication(options =>
            {
                options.Funcionario.Issuer = "i";
                options.Funcionario.Audience = "a";
                options.Funcionario.PublicKeyPem = "pem";
                options.Funcionario.JwksUrl = "http://jwks";
                options.Cliente.Issuer = "i";
                options.Cliente.Audience = "a";
                options.Cliente.PublicKeyPem = "pem";
            }));
    }

    [Fact]
    public void CreateRsaSecurityKey_ValidatesSignedToken()
    {
        // Arrange
        using var rsa = RSA.Create(2048);
        var privateKey = new RsaSecurityKey(rsa);
        var publicPem = rsa.ExportSubjectPublicKeyInfoPem();
        var signingCredentials = new SigningCredentials(privateKey, SecurityAlgorithms.RsaSha256);
        var token = new JwtSecurityToken(
            issuer: "issuer-cliente",
            audience: "audience-cliente",
            claims: [new Claim(ClienteJwtClaims.Documento, "86421537090")],
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: signingCredentials);
        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = "issuer-cliente",
            ValidAudience = "audience-cliente",
            IssuerSigningKey = JwtKeyMaterialAccessor.Create(publicPem)
        };

        // Act
        var principal = new JwtSecurityTokenHandler().ValidateToken(jwt, parameters, out _);

        // Assert
        Assert.Equal("86421537090", principal.FindFirst(ClienteJwtClaims.Documento)?.Value);
    }

    private static class JwtKeyMaterialAccessor
    {
        public static RsaSecurityKey Create(string pem)
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(pem);
            return new RsaSecurityKey(rsa.ExportParameters(false));
        }
    }
}
