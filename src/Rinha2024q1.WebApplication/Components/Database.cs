using System.Text.Json.Nodes;
using Npgsql;
using NpgsqlTypes;

namespace Rinha2024q1.WebApplication.Components;

public class Database
{
    private readonly static IDictionary<char, string> _procedures = new Dictionary<char, string> 
    {
        { 'd', "SELECT \"debitar\"(@ClienteId, @Valor, @Descricao)" },
        { 'c', "SELECT \"creditar\"(@ClienteId, @Valor, @Descricao)" }
    };

    private readonly NpgsqlConnection _dbConnection;

    public Database(NpgsqlConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<JsonObject?> GetExtratoAsync(int id)
    {
        const string query = """
                             SELECT clientes.limite,
                                    clientes.saldo
                             FROM clientes
                             WHERE clientes.id = @ClienteId;

                             SELECT transacoes.valor
                                  , transacoes.tipo
                                  , transacoes.descricao
                                  , transacoes.realizada_em
                             FROM transacoes
                             WHERE transacoes.cliente_id = @ClienteId
                             ORDER BY transacoes.realizada_em DESC
                             LIMIT 10;
                             """;

        await using var cmd = _dbConnection.CreateCommand();
        cmd.CommandText = query;

        cmd.Parameters.AddWithValue("@ClienteId", NpgsqlDbType.Integer, id);

        try
        {
            await _dbConnection.OpenAsync();

            await using var dataReader = await cmd.ExecuteReaderAsync();

            if (!await dataReader.ReadAsync())
            {
                return null;
            }

            var doc = new JsonObject {
                {   "saldo", new JsonObject
                    {
                        { "limite", dataReader.GetInt32(0) },
                        { "total", dataReader.GetInt32(1) }
                    }
                }
            };

            if (!dataReader.NextResult())
            {
                return doc;
            }

            var ultimasTransacoes = new JsonArray();

            while (await dataReader.ReadAsync())
            {
                ultimasTransacoes.Add(new JsonObject {
                    { "valor", dataReader.GetInt32(0) },
                    { "tipo", dataReader.GetChar(1) },
                    { "descricao", dataReader.GetString(2) },
                    { "realizado_em", new DateTimeOffset(dataReader.GetDateTime(3)).ToString("R") }
                });

            }
            
            doc.Add("ultimas_transacoes", ultimasTransacoes);

            return doc;
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }

    public async Task<JsonObject?> CriarTransacaoAsync(int clienteId, int valor, char tipo, string descricao)
    {
        await using var cmd = _dbConnection.CreateCommand();
        cmd.CommandText = _procedures[tipo];

        cmd.Parameters.AddWithValue("@ClienteId", NpgsqlDbType.Integer, clienteId);
        cmd.Parameters.AddWithValue("@Valor", NpgsqlDbType.Integer, valor);
        cmd.Parameters.AddWithValue("@Descricao", NpgsqlDbType.Varchar, 10, descricao);

        try
        {
            await _dbConnection.OpenAsync();

            var commandResult = (object[]) (await cmd.ExecuteScalarAsync())!;

            if (commandResult[0] is 1) {
                return null;
            }

            return new JsonObject 
            {
                { "limite", (int)commandResult[1] },
                { "saldo", (int)commandResult[2] }
            };
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }
}