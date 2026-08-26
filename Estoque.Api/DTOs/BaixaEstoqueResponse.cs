namespace Estoque.Api.DTOs;

public record ItemBaixaResultado(string Codigo, int SaldoAnterior, int SaldoAtual);

public record BaixaEstoqueResponse(int NotaNumero, List<ItemBaixaResultado> Itens);