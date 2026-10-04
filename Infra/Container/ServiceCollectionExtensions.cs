using ServidorNegocio.Http;
using ServidorNegocio.Infra.Cofre;
using ServidorNegocio.Services;

namespace ServidorNegocio.Infra.Container;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServidorNegocio(this IServiceCollection services, IConfiguration configuracao)
    {
        var raiz = configuracao["COFRE_PROJETO"] ?? throw new InvalidOperationException("Falta COFRE_PROJETO no .env");
        var raizExiste = Directory.Exists(raiz);

        if (!raizExiste)
            throw new InvalidOperationException("A pasta de COFRE_PROJETO nao existe: " + raiz);

        var bloqueadas = configuracao["PASTAS_BLOQUEADAS"] ?? string.Empty;
        var pastasBloqueadas = bloqueadas.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var nomeDaRaiz = new DirectoryInfo(raiz).Name;

        var cofre = new CofreEmDisco(raiz);
        services.AddSingleton(new NotasService(cofre, nomeDaRaiz, pastasBloqueadas));

        services.AddMcpServer(opcoes => opcoes.ServerInstructions =
                "Este servidor da leitura as notas do negocio do Ian e do pai dele. "
                + "No comeco de toda conversa, leia a nota negocio/indice com a ferramenta ler e siga o que ela disser.")
            .WithHttpTransport()
            .WithTools<NotasHandlers>();

        return services;
    }
}
