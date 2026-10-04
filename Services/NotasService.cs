using System.Globalization;
using System.Text;
using ServidorNegocio.Models;

namespace ServidorNegocio.Services;

public sealed class NotasService(ICofre cofre, string nomeDaRaiz, IReadOnlyList<string> pastasBloqueadas)
{
    private readonly string[] bloqueadas = [.. pastasBloqueadas.Select(pasta => pasta.Replace('\\', '/').Trim('/'))];

    public ConteudoDaPasta Listar(string pasta)
    {
        var prefixo = Normalizar(pasta);
        var naRaiz = prefixo.Length == 0;
        var subpastas = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var notas = new List<string>();
        var permitidas = NotasPermitidas();

        foreach (var caminho in permitidas)
        {
            var dentroDaPasta = naRaiz || caminho.StartsWith(prefixo + "/", StringComparison.OrdinalIgnoreCase);

            if (!dentroDaPasta)
                continue;

            var resto = naRaiz ? caminho : caminho[(prefixo.Length + 1)..];
            var barra = resto.IndexOf('/');

            if (barra < 0)
            {
                notas.Add(caminho);
                continue;
            }

            var fimDaSubpasta = caminho.Length - resto.Length + barra;
            subpastas.Add(caminho[..fimDaSubpasta]);
        }

        var conteudo = new ConteudoDaPasta { Pastas = [.. subpastas], Notas = notas };

        return conteudo;
    }

    public ResultadoDaBusca Buscar(string texto)
    {
        var procurado = SemAcento(texto.Trim());
        var limite = 40;
        var trechos = new List<Trecho>();
        var permitidas = NotasPermitidas();

        foreach (var caminho in permitidas)
        {
            var noNome = SemAcento(caminho).Contains(procurado, StringComparison.OrdinalIgnoreCase);

            if (noNome)
                trechos.Add(new Trecho { Caminho = caminho, Linha = null, Texto = caminho });

            var linhas = cofre.LerTexto(caminho).Split('\n');

            for (var indice = 0; indice < linhas.Length; indice++)
            {
                var linha = linhas[indice].Trim();
                var encontrou = SemAcento(linha).Contains(procurado, StringComparison.OrdinalIgnoreCase);

                if (encontrou)
                    trechos.Add(new Trecho { Caminho = caminho, Linha = indice + 1, Texto = linha });
            }
        }

        var cortado = trechos.Count > limite;
        var resultado = new ResultadoDaBusca
        {
            Trechos = cortado ? trechos.GetRange(0, limite) : trechos,
            Cortado = cortado,
        };

        return resultado;
    }

    public LeituraDeNota Ler(string referencia)
    {
        var alvo = SemAcento(Normalizar(referencia));
        var exatas = new List<string>();
        var parecidas = new List<string>();
        var permitidas = NotasPermitidas();

        foreach (var caminho in permitidas)
        {
            var semExtensao = SemAcento(caminho[..^3]);
            var igual = semExtensao.Equals(alvo, StringComparison.OrdinalIgnoreCase);
            var terminaIgual = semExtensao.EndsWith("/" + alvo, StringComparison.OrdinalIgnoreCase);

            if (igual)
                exatas.Add(caminho);

            if (terminaIgual)
                parecidas.Add(caminho);
        }

        var candidatas = exatas.Count == 1 ? exatas : parecidas;

        if (candidatas.Count != 1)
        {
            var semNota = new LeituraDeNota { Nota = null, Candidatas = candidatas };
            return semNota;
        }

        var unica = candidatas[0];
        var nota = new Nota { Caminho = unica, Conteudo = cofre.LerTexto(unica) };
        var leitura = new LeituraDeNota { Nota = nota, Candidatas = candidatas };

        return leitura;
    }

    private List<string> NotasPermitidas()
    {
        var arquivos = cofre.ListarArquivos();
        var permitidas = new List<string>();

        foreach (var caminho in arquivos)
        {
            var ehNota = caminho.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
            var oculto = caminho.StartsWith('.') || caminho.Contains("/.", StringComparison.Ordinal);
            var bloqueado = bloqueadas.Any(bloqueada =>
                caminho.Equals(bloqueada, StringComparison.OrdinalIgnoreCase)
                || caminho.StartsWith(bloqueada + "/", StringComparison.OrdinalIgnoreCase));
            var permitida = ehNota && !oculto && !bloqueado;

            if (permitida)
                permitidas.Add(caminho);
        }

        permitidas.Sort(StringComparer.OrdinalIgnoreCase);

        return permitidas;
    }

    private string Normalizar(string referencia)
    {
        var caminho = referencia.Trim().TrimStart('[').TrimEnd(']');
        var fimDoAlvo = caminho.IndexOfAny(['|', '#']);

        if (fimDoAlvo >= 0)
            caminho = caminho[..fimDoAlvo];

        caminho = caminho.Replace('\\', '/').Trim().Trim('/');

        var comExtensao = caminho.EndsWith(".md", StringComparison.OrdinalIgnoreCase);

        if (comExtensao)
            caminho = caminho[..^3];

        var comPrefixoDoCofre = caminho.StartsWith(nomeDaRaiz + "/", StringComparison.OrdinalIgnoreCase);

        if (comPrefixoDoCofre)
            caminho = caminho[(nomeDaRaiz.Length + 1)..];

        return caminho;
    }

    private static string SemAcento(string texto)
    {
        var decomposto = texto.Normalize(NormalizationForm.FormD);
        var limpo = new StringBuilder(decomposto.Length);

        foreach (var letra in decomposto)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(letra);

            if (categoria == UnicodeCategory.NonSpacingMark)
                continue;

            limpo.Append(letra);
        }

        var resultado = limpo.ToString();

        return resultado;
    }
}
