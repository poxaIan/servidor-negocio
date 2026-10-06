# Servidor do negócio

Servidor MCP em C#/.NET que deixa o Claude de outra pessoa ler as minhas notas em Markdown, pela
rede de casa, sem copiar arquivo nenhum para o computador dela.

## O problema

Guardo as notas de um projeto numa pasta de arquivos `.md` (um cofre do Obsidian) no meu PC. A outra
pessoa do projeto usa o Claude Code no notebook dela e precisa do mesmo contexto, sem eu mandar
arquivo por arquivo.

Duas saídas descartadas:

- **Obsidian Sync:** cada um paga uma assinatura e o cofre inteiro é compartilhado, inclusive o que ela
  não deve ver.
- **Repositório no GitHub:** pede `push` do lado dela, e o notebook fica em casa, então a rede local basta.

## O que faz

Três ferramentas MCP, todas só de leitura:

| Ferramenta | Para quê |
| --- | --- |
| `listar` | lista as notas e subpastas de uma pasta; sem pasta, a raiz |
| `buscar` | procura um texto no nome e no conteúdo das notas, sem diferenciar acento (corta em 40 trechos) |
| `ler` | abre uma nota pelo nome, como nos links `[[...]]`, ou pelo caminho |

Ao conectar, o servidor manda ao Claude a instrução de começar pela nota `negocio/indice`. Mudo o jeito
de a conversa andar editando essa nota, sem mexer no computador dela.

## Decisões de segurança

- **Senha (token) em toda requisição**, com pelo menos 32 caracteres, comparada em tempo constante
  (`CryptographicOperations.FixedTimeEquals`). Sem token, 401.
- **Só `.md`.** Qualquer outro arquivo da pasta nunca é listado nem entregue.
- **Só a pasta configurada.** A `ler` escolhe entre os arquivos já listados, em vez de montar um caminho
  a partir do que o cliente pediu, então não há `..` que escape da pasta. Arquivos ocultos e links
  simbólicos são ignorados, e dá para bloquear subpastas inteiras (`PASTAS_BLOQUEADAS`).
- **Só rede local.** Sem túnel público; a regra do firewall libera a porta só para a sub-rede e só em
  rede privada.
- **O `.env` fica fora do git**, e o token se gera num PowerShell fora do Claude.

## O que ele não é

- É uso pessoal: um usuário, uma rede de casa, testado à mão em duas máquinas. **Ainda não tem testes
  automatizados.**
- Só a etapa 1 (leitura) existe. Guardar notas e enviar arquivos estão planejados, mas só entram
  quando a etapa anterior estiver em uso.
- Não é feito para ficar exposto na internet.

## Rodar

Precisa do SDK do .NET 10 (`dotnet --list-sdks`) e do `.env` na raiz (ver abaixo). Então, na raiz do
repositório:

```powershell
dotnet run
```

Deixe a janela aberta. Quando ele escreve `Now listening on: http://0.0.0.0:5180`, está no ar.
Se o Windows perguntar se o `dotnet` pode usar a rede, marque só **Redes privadas**.

### O `.env`

Fica fora do git. Uma variável por linha, no formato `NOME=valor` ou `NOME="valor"`. Aspas duplas o
leitor tira; aspas **simples** entram no valor e estragam o caminho, o endereço e o token.

| Variável | O que decide |
| --- | --- |
| `COFRE_PROJETO` | a pasta de notas que o servidor lê |
| `PASTAS_BLOQUEADAS` | subpastas ou notas dessa pasta que ele nunca entrega, separadas por `;` (pode ficar vazia) |
| `ENDERECO` | onde ele escuta: `0.0.0.0` aceita a rede de casa, e não só este PC |
| `TOKEN` | a senha, com pelo menos 32 caracteres |

Para gerar o token e escrever o `.env`, num PowerShell **fora do Claude**, na raiz do repositório
(troque `$pasta` pela pasta das suas notas):

```powershell
$pasta = "D:\caminho\da\pasta\de\notas"
$bytes = New-Object byte[] 32
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$token = -join ($bytes | ForEach-Object { $_.ToString('x2') })
@"
COFRE_PROJETO=$pasta
PASTAS_BLOQUEADAS=
ENDERECO=http://0.0.0.0:5180
TOKEN=$token
"@ | Set-Content -Path .env -Encoding ascii
```

### A rede de casa

Uma vez, num PowerShell **como administrador**:

```powershell
Get-NetConnectionProfile
New-NetFirewallRule -DisplayName "servidor-negocio" -Direction Inbound -Protocol TCP -LocalPort 5180 -RemoteAddress LocalSubnet -Profile Private -Action Allow
```

O primeiro comando precisa mostrar `NetworkCategory : Private`. Se mostrar `Public`, troque com
`Set-NetConnectionProfile -InterfaceAlias "<nome que apareceu>" -NetworkCategory Private`. A regra abre a
porta 5180 só para aparelhos da mesma rede.

O endereço deste PC na rede sai do `ipconfig`, em **Endereço IPv4** da placa que liga no roteador. Se ele
mudar depois de religar o roteador, reserve o IP no roteador.

### O notebook de quem usa

Os arquivos de `notebook-do-pai/` vão para a pasta `.claude` do usuário dessa máquina:

| Arquivo daqui | Vira lá | Para quê |
| --- | --- | --- |
| `instrucoes.md` | `%USERPROFILE%\.claude\CLAUDE.md` | manda o Claude começar pelo `negocio/indice` |
| `contexto.md` | `%USERPROFILE%\.claude\commands\contexto.md` | o comando `/contexto` |
| `settings.json` | `%USERPROFILE%\.claude\settings.json` | deixa as ferramentas do servidor rodarem sem pedir licença |

Se o `CLAUDE.md` ou o `settings.json` já existirem lá, junte o conteúdo em vez de substituir.

O servidor entra no Claude dessa máquina com o token, num PowerShell dela:

```powershell
$token = Read-Host "Cole o token"
claude mcp add --transport http --scope user negocio http://<IP deste PC>:5180/mcp --header "Authorization: Bearer $token"
claude mcp list
```

`negocio` tem que aparecer como conectado. Se o Claude abrir antes deste servidor estar no ar, ele marca o
servidor como falho; `/mcp reconnect all`, dentro do Claude, tenta de novo.

## Onde fica o quê

```
Program.cs        liga as peças e sobe o servidor
Http/             as ferramentas do MCP (listar, buscar, ler) e a checagem do token
Services/         o que pode ser lido e como uma nota é achada
Infra/            o disco, e o registro das peças em Container/
Models/           o dado já normalizado
notebook-do-pai/  os arquivos que vão para o Claude de quem usa
```

## O que vem depois

Cada etapa só entra quando a anterior estiver em uso.

**2 · Guardar** — a ferramenta `anotar` e o comando `/guardar`: o Claude grava uma nota resumida, ou
acrescenta um bloco datado no fim de uma nota, dentro da pasta. Nunca sobrescreve, apaga nem mexe no
cabeçalho.

**3 · Enviar** — o comando `/enviar`: um script no notebook manda um arquivo (PDF, imagem) direto para
uma pasta `anexos/`, sem passar pelo Claude, com uma nota `.md` que diz o que ele é.
