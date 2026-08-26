namespace Faturamento.Api.Models;

public class ItemNotaFiscal
{
    public int Id { get; set; }
    public int NotaFiscalId { get; set; }
    public NotaFiscal? NotaFiscal { get; set; }

    public string CodigoProduto { get; set; } = default!;
    public string DescricaoProduto { get; set; } = default!;
    public int Quantidade { get; set; }
}
