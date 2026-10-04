using System.Security.Cryptography;
using System.Text;

namespace ServidorNegocio.Http;

public static class AcessoPorToken
{
    public static void UseAcessoPorToken(this WebApplication app)
    {
        var token = app.Configuration["TOKEN"] ?? throw new InvalidOperationException("Falta TOKEN no .env");
        var tokenCurto = token.Length < 32;

        if (tokenCurto)
            throw new InvalidOperationException("O TOKEN do .env precisa ter pelo menos 32 caracteres");

        var esperado = Encoding.UTF8.GetBytes("Bearer " + token);

        app.Use(async (contexto, proximo) =>
        {
            var recebido = Encoding.UTF8.GetBytes(contexto.Request.Headers.Authorization.ToString());
            var autorizado = CryptographicOperations.FixedTimeEquals(recebido, esperado);

            if (!autorizado)
            {
                contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await proximo(contexto);
        });
    }
}
