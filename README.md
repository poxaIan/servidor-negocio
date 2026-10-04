# Servidor do negócio

Servidor MCP que deixa o Claude do meu pai ler as notas de `Projeto/` do cofre, pela rede de
casa. Só leitura, só arquivo `.md`, com senha (token). O desenho e as decisões estão na nota
`Projeto/negocio/servidor interno.md` do cofre.

## Rodar

Precisa do SDK do .NET 10 (`dotnet --list-sdks`) e do `.env` na raiz (ver abaixo). Então, na
raiz do repositório:

```powershell
dotnet run
```

Deixe a janela aberta. Quando ele escreve `Now listening on: http://0.0.0.0:5180`, está no ar.
Se o Windows perguntar se o `dotnet` pode usar a rede, marque só **Redes privadas**.

## O `.env`

Fica fora do git. Uma variável por linha, no formato `NOME=valor`:

| Variável | O que decide | Valor de hoje |
| --- | --- | --- |
| `COFRE_PROJETO` | a pasta que o servidor lê | `D:\Ian\obsidian\github\My-Second-Brain\Projeto` |
| `PASTAS_BLOQUEADAS` | pastas ou notas de dentro dela que ele nunca entrega, separadas por `;` | `financas;TCC Maria` |
| `ENDERECO` | onde ele escuta: `0.0.0.0` aceita a rede de casa, e não só este PC | `http://0.0.0.0:5180` |
| `TOKEN` | a senha, com pelo menos 32 caracteres | gerada uma vez, como abaixo |

Para gerar o token e escrever o `.env`, num PowerShell **fora do Claude**, na raiz do
repositório:

```powershell
$bytes = New-Object byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$token = -join ($bytes | ForEach-Object { $_.ToString('x2') })
@"
COFRE_PROJETO=D:\Ian\obsidian\github\My-Second-Brain\Projeto
PASTAS_BLOQUEADAS=financas;TCC Maria
ENDERECO=http://0.0.0.0:5180
TOKEN=$token
"@ | Set-Content -Path .env -Encoding ascii
```

## A rede de casa

Uma vez, num PowerShell **como administrador**:

```powershell
Get-NetConnectionProfile
New-NetFirewallRule -DisplayName "servidor-negocio" -Direction Inbound -Protocol TCP -LocalPort 5180 -RemoteAddress LocalSubnet -Profile Private -Action Allow
```

O primeiro comando precisa mostrar `NetworkCategory : Private`. Se mostrar `Public`, troque
com `Set-NetConnectionProfile -InterfaceAlias "<nome que apareceu>" -NetworkCategory Private`.
A regra abre a porta 5180 só para aparelhos da mesma rede.

O endereço deste PC na rede sai do `ipconfig`, em **Endereço IPv4** da placa que liga no
roteador (algo como `192.168.0.15`). Se ele mudar depois de religar o roteador, reserve o IP
no roteador.

## O notebook do meu pai

Os arquivos de `notebook-do-pai/` vão para a pasta `.claude` do usuário dele:

| Arquivo daqui | Vira lá | Para quê |
| --- | --- | --- |
| `instrucoes.md` | `%USERPROFILE%\.claude\CLAUDE.md` | manda o Claude dele começar pelo `negocio/indice` |
| `contexto.md` | `%USERPROFILE%\.claude\commands\contexto.md` | o comando `/contexto` |
| `settings.json` | `%USERPROFILE%\.claude\settings.json` | deixa as ferramentas do servidor rodarem sem pedir licença |

Se o `CLAUDE.md` ou o `settings.json` já existirem lá, junte o conteúdo em vez de substituir.

O servidor entra no Claude dele com o token, num PowerShell do notebook:

```powershell
$token = Read-Host "Cole o token"
claude mcp add --transport http --scope user negocio http://<IP deste PC>:5180/mcp --header "Authorization: Bearer $token"
claude mcp list
```

`negocio` tem que aparecer como conectado. Se o Claude dele abrir antes deste servidor estar
no ar, ele marca o servidor como falho; `/mcp reconnect all`, dentro do Claude, tenta de novo.

## Onde fica o quê

```
Program.cs        liga as peças e sobe o servidor
Http/             as ferramentas do MCP (ler, buscar, listar) e a checagem do token
Services/         o que pode ser lido e como uma nota é achada
Infra/            o disco, e o registro das peças em Container/
Models/           o dado já normalizado
notebook-do-pai/  os arquivos que vão para o Claude dele
```

## O que vem depois

Cada etapa só entra quando a anterior estiver em uso.

**2 · Guardar** — a ferramenta `anotar` e o comando `/guardar`: o Claude dele grava uma nota
resumida, ou acrescenta um bloco datado no fim de uma nota, dentro de `Projeto/negocio/`. Nunca
sobrescreve, apaga nem mexe no cabeçalho.

**3 · Enviar** — o comando `/enviar`: um script no notebook manda um arquivo (PDF, imagem)
direto para uma pasta `anexos/`, sem passar pelo Claude, com uma nota `.md` que diz o que ele é.
