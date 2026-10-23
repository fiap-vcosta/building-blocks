# Tech Challenge — Guia para agentes (`building-blocks`)

Biblioteca .NET 8 compartilhada pelos serviços da oficina. Pacote `building-blocks`. Org [fiap-vcosta](https://github.com/fiap-vcosta).

## Antes de mudar código

1. Perguntar antes de alterar um tipo público que os serviços vão referenciar
2. Não criar host web nem API neste repositório
3. **Git:** nunca commit/push direto em `main` — branch → PR → merge (ver [`.cursor/rules/git-workflow.mdc`](.cursor/rules/git-workflow.mdc))

## Layout alvo

Class library .NET 8, sem web host. Testes espelham `src/`. Id do pacote: `building-blocks`. Versões de pacote exatas quando o código existir. Sem comentários narrativos.

## Fora de escopo

Código de domínio de um serviço, host HTTP e Terraform de nuvem. Contratos de evento, worker Pub/Sub, propagação de trace, Problem Details, validação JWT/JWKS, `X-Service-Key` e logging JSON chegam depois; este repositório ainda não contém a biblioteca.

## Comandos

Ainda não há projeto nem testes. Não inventar comandos `dotnet` até o código existir.
