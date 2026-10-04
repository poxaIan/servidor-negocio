using System.ComponentModel;
using System.Text;
using ModelContextProtocol.Server;
using ServidorNegocio.Services;

namespace ServidorNegocio.Http;

[McpServerToolType]
public sealed class NotasHandlers(NotasService notas)
{
    [McpServerTool(Name = "ler")]
    [Description("Le uma nota do cofre do negocio pelo nome, como nos links [[...]], ou pelo caminho, como negocio/indice. Comece sempre por negocio/indice.")]
    public string Ler([Description("Nome da nota, link [[...]] ou caminho relativo a raiz.")] string nota)
    {
        var leitura = notas.Ler(nota);

        if (leitura.Nota is not null)
        {
            var conteudo = "Nota: " + leitura.Nota.Caminho + "\n\n" + leitura.Nota.Conteudo;
            return conteudo;
        }

        if (leitura.Candidatas.Count == 0)
            return "Nenhuma nota com esse nome ou caminho neste servidor. Use buscar ou listar.";

        var texto = new StringBuilder();
        texto.AppendLine("Mais de uma nota com esse nome. Chame ler de novo com o caminho de uma delas:");

        foreach (var candidata in leitura.Candidatas)
            texto.AppendLine("- " + candidata);

        var resposta = texto.ToString();

        return resposta;
    }

    [McpServerTool(Name = "buscar")]
    [Description("Procura um texto no nome e no conteudo das notas, sem diferenciar acento nem maiuscula. Devolve o caminho e a linha de cada trecho.")]
    public string Buscar([Description("O texto a procurar, como loja de brinquedos.")] string texto)
    {
        var semTexto = string.IsNullOrWhiteSpace(texto);

        if (semTexto)
            return "Informe o texto a procurar.";

        var resultado = notas.Buscar(texto);

        if (resultado.Trechos.Count == 0)
            return "Nada encontrado. Tente outra palavra ou use listar.";

        var saida = new StringBuilder();

        foreach (var trecho in resultado.Trechos)
        {
            var onde = trecho.Linha is null ? "nome" : "linha " + trecho.Linha;
            saida.AppendLine(trecho.Caminho + " (" + onde + "): " + trecho.Texto);
        }

        if (resultado.Cortado)
            saida.AppendLine("Ha mais resultados. Refine a busca.");

        var resposta = saida.ToString();

        return resposta;
    }

    [McpServerTool(Name = "listar")]
    [Description("Lista as notas e as subpastas de uma pasta do cofre do negocio. Sem pasta, lista a raiz.")]
    public string Listar([Description("Pasta relativa a raiz, como negocio/operis. Vazio para a raiz.")] string pasta = "")
    {
        var conteudo = notas.Listar(pasta);
        var vazia = conteudo.Pastas.Count == 0 && conteudo.Notas.Count == 0;

        if (vazia)
            return "Pasta vazia, inexistente ou fora do que este servidor entrega.";

        var texto = new StringBuilder();
        texto.AppendLine("Subpastas:");

        foreach (var subpasta in conteudo.Pastas)
            texto.AppendLine("- " + subpasta + "/");

        texto.AppendLine();
        texto.AppendLine("Notas:");

        foreach (var nota in conteudo.Notas)
            texto.AppendLine("- " + nota);

        var resposta = texto.ToString();

        return resposta;
    }
}
