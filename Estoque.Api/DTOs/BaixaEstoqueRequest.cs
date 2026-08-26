namespace Estoque.Api.DTOs;

public record ItemBaixaRequest(string Codigo, int Quantidade);

public record BaixaEstoqueRequest(int NotaNumero, List<ItemBaixaRequest> Itens);