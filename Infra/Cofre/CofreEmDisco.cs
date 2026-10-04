using ServidorNegocio.Services;

namespace ServidorNegocio.Infra.Cofre;

public sealed class CofreEmDisco(string raiz) : ICofre
{
    public IReadOnlyList<string> ListarArquivos()
    {
        var opcoes = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReparsePoint,
        };

        var encontrados = Directory.EnumerateFiles(raiz, "*", opcoes);
        var arquivos = new List<string>();

        foreach (var arquivo in encontrados)
        {
            var relativo = Path.GetRelativePath(raiz, arquivo).Replace('\\', '/');
            arquivos.Add(relativo);
        }

        return arquivos;
    }

    public string LerTexto(string caminho)
    {
        var completo = Path.Combine(raiz, caminho);
        var texto = File.ReadAllText(completo);

        return texto;
    }
}
