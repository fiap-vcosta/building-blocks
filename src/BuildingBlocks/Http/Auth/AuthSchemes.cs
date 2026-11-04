namespace BuildingBlocks.Http.Auth;

public static class AuthSchemes
{
    public const string Funcionario = "Funcionario";
    public const string Cliente = "Cliente";
}

public static class AuthPolicies
{
    public const string ClienteJwt = "ClienteJwt";
    public const string FuncionarioJwt = "FuncionarioJwt";
}

public static class ClienteJwtClaims
{
    public const string Documento = "documento";
}
