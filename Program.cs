using ServidorNegocio.Http;
using ServidorNegocio.Infra.Container;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddIniFile(".env", optional: false);
builder.Services.AddServidorNegocio(builder.Configuration);

var app = builder.Build();
var endereco = app.Configuration["ENDERECO"] ?? throw new InvalidOperationException("Falta ENDERECO no .env");

app.UseAcessoPorToken();
app.MapMcp("/mcp");
app.Run(endereco);
