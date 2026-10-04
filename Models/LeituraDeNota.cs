namespace ServidorNegocio.Models;

public sealed class LeituraDeNota
{
    public required Nota? Nota { get; init; }
    public required IReadOnlyList<string> Candidatas { get; init; }
}
