namespace ServidorNegocio.Models;

public sealed class Trecho
{
    public required string Caminho { get; init; }
    public required int? Linha { get; init; }
    public required string Texto { get; init; }
}
