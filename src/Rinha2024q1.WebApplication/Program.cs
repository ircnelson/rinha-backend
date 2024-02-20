using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Rinha2024q1.WebApplication.Components;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddRequestTimeouts(options => options.DefaultPolicy = new RequestTimeoutPolicy
{
    Timeout = TimeSpan.FromSeconds(2)
});

builder.Services.AddResponseCompression();

var app = builder.Build();

app.UseResponseCompression();
app.UseRequestTimeouts();

app.MapPost("/clientes/{id}/transacoes", async (
    HttpContext ctx,
    [FromRoute] string id,
    [FromBody] CreateTransactionRequest request) =>
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

    if (request.Tipo != "d" && request.Tipo != "c")
    {
        ctx.Response.StatusCode = 422;
        return;
    }

    if (request.Valor != Math.Floor(request.Valor) || request.Valor <= 0)
    {
        ctx.Response.StatusCode = 422;
        return;
    }

    await using var connection = new NpgsqlConnection(builder.Configuration.GetConnectionString("Postgres"));

    var db = new Database(connection);

    var conta = await db.CriarTransacaoAsync(clienteId, (int)request.Valor, request.Tipo[0], request.Descricao);

    if (conta is not null)
    {
        await ctx.Response.WriteAsJsonAsync(conta);
        return;
    }

    ctx.Response.StatusCode = 422;

});

app.MapGet("/clientes/{id}/extrato", async (
    HttpContext ctx, 
    [FromRoute] string id) =>
{
    if (!int.TryParse(id, out var clienteId))
    {
        ctx.Response.StatusCode = 422;
        return;
    }

    await using var connection = new NpgsqlConnection(builder.Configuration.GetConnectionString("Postgres"));

    var db = new Database(connection);

    var conta = await db.GetExtratoAsync(clienteId);

    if (conta is not null)
    {
        await ctx.Response.WriteAsJsonAsync(conta);
        return;
    }

    ctx.Response.StatusCode = 404;
});

await app.RunAsync();