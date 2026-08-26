using Faturamento.Api.DTOs.Estoque;
using Faturamento.Api.Models;

namespace Faturamento.Api.DTOs;

public record AdicionarItemRequest(string CodigoProduto, int Quantidade);

public record ItemNotaFiscalResponse(string CodigoProduto, string DescricaoProduto, int Quantidade)
{
    public static ItemNotaFiscalResponse FromEntity(ItemNotaFiscal item) =>
        new(item.CodigoProduto, item.DescricaoProduto, item.Quantidade);
}

public record NotaFiscalResponse(
    int Numero,
    string Status,
    DateTime DataCriacao,
    DateTime? DataFechamento,
    List<ItemNotaFiscalResponse> Itens)
{
    public static NotaFiscalResponse FromEntity(NotaFiscal nota) =>
        new(
            nota.Numero,
            nota.Status.ToString(),
            nota.DataCriacao,
            nota.DataFechamento,
            nota.Itens.Select(ItemNotaFiscalResponse.FromEntity).ToList());
}

public record ImprimirNotaFiscalResponse(int Numero, string Status, List<ItemBaixaResultado> Itens);
