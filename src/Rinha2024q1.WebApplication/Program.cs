using System.Text.Json;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Rinha2024q1.WebApplication.Components;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddRequestTimeouts(options => options.DefaultPolicy = new RequestTimeoutPolicy
{
    Timeout = TimeSpan.FromSeconds(10)
});
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
});

builder.Services.AddResponseCompression();

builder.Services.AddScoped<NpgsqlConnection>(_ =>
{
    var connection = new NpgsqlConnection(builder.Configuration.GetConnectionString("Postgres"));
    return connection;
});

builder.Services.AddScoped<Database>();

var app = builder.Build();

app.UseResponseCompression();
app.UseRequestTimeouts();

app.MapPost("/clientes/{id}/transacoes", async (
    HttpContext ctx,
    [FromRoute] string id,
    [FromBody] CreateTransactionRequest request,
    [FromServices] Database db) =>
{
    if (!int.TryParse(id, out var clienteId))
    {
        ctx.Response.StatusCode = 422;
        return;
    }

    if (string.IsNullOrWhiteSpace(request.Descricao) || request.Descricao.Length > 10)
    {
        ctx.Response.StatusCode = 422;
        return;
    }

    if (!request.Tipo.Equals("d", StringComparison.InvariantCultureIgnoreCase) &&
        !request.Tipo.Equals("c", StringComparison.InvariantCultureIgnoreCase))
    {
        ctx.Response.StatusCode = 422;
        return;
    }

    if (request.Valor != Math.Floor(request.Valor) || request.Valor <= 0)
    {
        ctx.Response.StatusCode = 422;
        return;
    }

    var (ok, limite, saldo) = await db.CriarTransacaoAsync(clienteId, (int)request.Valor, request.Tipo[0], request.Descricao);

    if (ok)
    {
        await ctx.Response.WriteAsJsonAsync(new { limite, saldo });
    }
    else
    {
        ctx.Response.StatusCode = 422;
    }
});

app.MapGet("/clientes/{id}/extrato", async (
    HttpContext ctx, 
    [FromRoute] string id,
    [FromServices] Database db) =>
{
    if (!int.TryParse(id, out var clienteId))
    {
        ctx.Response.StatusCode = 422;
        return;
    }

    var conta = await db.GetExtratoAsync(clienteId);

    if (conta is not null)
    {
        await ctx.Response.WriteAsJsonAsync(new
        {
            Saldo = new
            {
                conta.Limite,
                Total = conta.Saldo,
            },
            UltimasTransacoes = conta.Transacoes
        });
        
        return;
    }

    ctx.Response.StatusCode = 404;
});

await app.RunAsync();