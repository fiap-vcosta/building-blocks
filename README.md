# building-blocks

Biblioteca .NET 8 compartilhada pelos serviços da oficina. Pacote NuGet `building-blocks` (versão `1.0.0`) publicado no GitHub Packages da org [fiap-vcosta](https://github.com/fiap-vcosta).

## Conteúdo

- Contratos de eventos de integração (envelope camelCase)
- Pub/Sub (`IPubSubTransport`, publisher, worker) com propagação Datadog.Trace
- Problem Details (BCL + mapeamentos extras no host)
- `X-Service-Key` (`ServiceAuth:Key`)
- JWT RS256 (`Funcionario` / `Cliente`, JWKS ou PEM pública; claim `documento` no cliente)
- Serilog compact + sink Datadog opcional; middleware `X-Request-Id`

## Restore local

1. Crie um PAT com escopo `read:packages` (não commitar o token).
2. Autentique o feed da org:

```bash
dotnet nuget add source https://nuget.pkg.github.com/fiap-vcosta/index.json \
  --name github \
  --username SEU_USUARIO_GITHUB \
  --password SEU_PAT \
  --store-password-in-clear-text
```

3. No projeto consumidor (com `nuget.config` mapeando `building-blocks` → `github`):

```bash
dotnet add package building-blocks --version 1.0.0
dotnet restore
```

O `nuget.config` deste repositório já define o mapping: `building-blocks` só no feed `github`; demais pacotes em `nuget.org`.

## Docker build com secret mount

No `Dockerfile` do serviço, monte o PAT só no restore (BuildKit):

```dockerfile
# syntax=docker/dockerfile:1
RUN --mount=type=secret,id=nuget_token \
    dotnet nuget update source github \
      --username SEU_USUARIO_GITHUB \
      --password-stdin \
      --store-password-in-clear-text < /run/secrets/nuget_token \
 && dotnet restore
```

```bash
DOCKER_BUILDKIT=1 docker build \
  --secret id=nuget_token,env=NUGET_AUTH_TOKEN \
  -t meu-servico .
```

## Cobertura

```bash
dotnet test tests/UnitTests/UnitTests.csproj -c Release \
  /p:CollectCoverage=true \
  /p:CoverletOutputFormat=cobertura \
  /p:CoverletOutput=../../test-results/coverage/coverage \
  /p:Threshold=80 \
  /p:ThresholdType=line%2Cbranch \
  /p:ThresholdStat=minimum
```

Gate: ≥ 80% line e branch.

## CI / publish

- `CI`: InspectCode (warning = falha), unitários + Coverlet 80, SonarCloud (`fiap-vcosta_building-blocks`)
- `Publish` (push em `main`): `dotnet pack` + push para GitHub Packages; se a versão `1.0.0` já existir, o job termina verde
