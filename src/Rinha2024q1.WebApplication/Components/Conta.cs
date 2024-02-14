namespace Rinha2024q1.WebApplication.Components;

public sealed class Conta(int id, int limite, int saldo)
{
    private readonly List<Transacao> _transacoes = new(10);
    
    public int Id { get; } = id;
    public int Limite { get; } = limite;
    public int Saldo { get; } = saldo;

    public IEnumerable<Transacao> Transacoes => _transacoes.ToArray();

    public void AddTransaction(int valor, char tipo, string descricao, DateTimeOffset realizadoEm)
    {
        _transacoes.Add(new Transacao(valor, tipo, descricao, realizadoEm));
    } 
}