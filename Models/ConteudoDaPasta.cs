namespace ServidorNegocio.Models;

public sealed class ConteudoDaPasta
{
    public required IReadOnlyList<string> Pastas { get; init; }
    public required IReadOnlyList<string> Notas { get; init; }
}
