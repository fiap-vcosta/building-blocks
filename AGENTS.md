# Tech Challenge — Guia para agentes (`building-blocks`)

Biblioteca .NET 8 compartilhada pelos serviços da oficina. Pacote `building-blocks`. Org [fiap-vcosta](https://github.com/fiap-vcosta).

## Antes de mudar código

1. Perguntar antes de alterar um tipo público que os serviços vão referenciar
2. Não criar host web nem API neste repositório
3. **Git:** nunca commit/push direto em `main` — branch → PR → merge (ver [`.cursor/rules/git-workflow.mdc`](.cursor/rules/git-workflow.mdc))

## Layout

| Peça | Papel |
|------|--------|
| `src/BuildingBlocks` | Class library: Events, Messaging, Tracing, Http (Problem Details, Auth, Middleware, Logging) |
| `tests/UnitTests` | Testes unitários espelhando `src/` |
| `.github/workflows/ci.yml` | InspectCode + unit/Coverlet 80 + SonarCloud |
| `.github/workflows/publish.yml` | Pack/push GitHub Packages em push `main` |

Dependências de pacote com versão exata. Sem comentários narrativos em C#.

## Fora de escopo

Código de domínio de um serviço, host HTTP e Terraform de nuvem.

## Comandos

```bash
dotnet restore building-blocks.sln
dotnet build building-blocks.sln -c Release
dotnet test tests/UnitTests/UnitTests.csproj -c Release \
  /p:CollectCoverage=true \
  /p:CoverletOutputFormat=cobertura \
  /p:CoverletOutput=../../test-results/coverage/coverage \
  /p:Threshold=80 \
  /p:ThresholdType=line%2Cbranch \
  /p:ThresholdStat=minimum
dotnet pack src/BuildingBlocks/BuildingBlocks.csproj -c Release -o artifacts
```
