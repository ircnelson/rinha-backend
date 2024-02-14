using Npgsql;
using NpgsqlTypes;

namespace Rinha2024q1.WebApplication.Components;

public class Database
{
    private readonly NpgsqlConnection _dbConnection;

    public Database(NpgsqlConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<Conta?> GetExtratoAsync(int id)
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

            var conta = new Conta(id, dataReader.GetInt32(0), dataReader.GetInt32(1));

            if (!dataReader.NextResult())
            {
                return conta;
            }

            while (await dataReader.ReadAsync())
            {
                conta.AddTransaction(dataReader.GetInt32(0), 
                    dataReader.GetChar(1), 
                    dataReader.GetString(2),
                    new DateTimeOffset(dataReader.GetDateTime(3)));
            }

            return conta;
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }

    public async Task<(bool, int, int)> CriarTransacaoAsync(int clienteId, int valor, char tipo, string descricao)
    {
        var query = tipo switch
        {
            'c' => "SELECT \"creditar\"(@ClienteId, @Valor, @Descricao)",
            'd' => "SELECT \"debitar\"(@ClienteId, @Valor, @Descricao)",
            _ => throw new InvalidOperationException()
        };
        
        await using var cmd = _dbConnection.CreateCommand();
        cmd.CommandText = query;

        cmd.Parameters.AddWithValue("@ClienteId", NpgsqlDbType.Integer, clienteId);
        cmd.Parameters.AddWithValue("@Valor", NpgsqlDbType.Integer, valor);
        cmd.Parameters.AddWithValue("@Descricao", NpgsqlDbType.Varchar, 10, descricao);

        try
        {
            await _dbConnection.OpenAsync();

            var commandResult = (object[]) (await cmd.ExecuteScalarAsync())!;

            return (commandResult[0] is 0, (int)commandResult[1], (int)commandResult[2]);
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }
    }
}