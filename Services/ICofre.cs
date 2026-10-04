namespace ServidorNegocio.Services;

public interface ICofre
{
    IReadOnlyList<string> ListarArquivos();
    string LerTexto(string caminho);
}
