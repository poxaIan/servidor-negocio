namespace ServidorNegocio.Models;

public sealed class ResultadoDaBusca
{
    public required IReadOnlyList<Trecho> Trechos { get; init; }
    public required bool Cortado { get; init; }
}
