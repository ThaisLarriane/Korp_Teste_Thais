namespace Faturamento.Api.Models;

public enum StatusNotaFiscal
{
    Aberta = 0,
    Fechada = 1
}

public class NotaFiscal
{
    public int Numero { get; set; }
    public StatusNotaFiscal Status { get; set; } = StatusNotaFiscal.Aberta;
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public DateTime? DataFechamento { get; set; }

    public List<ItemNotaFiscal> Itens { get; set; } = new();
}
