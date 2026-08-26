namespace Faturamento.Api.DTOs.Estoque;

// Espelham os contratos expostos pelo Estoque.Api (ver Estoque.Api/DTOs).
// Mantidos em um projeto separado propositalmente: cada microsserviço é
// dono do seu próprio contrato; o Faturamento só conhece o formato JSON
// que consome/envia via HTTP, sem referenciar o assembly do Estoque.Api.

public record ProdutoEstoqueResponse(string Codigo, string Descricao, int Saldo);

public record ItemBaixaRequest(string Codigo, int Quantidade);

public record BaixaEstoqueRequest(int NotaNumero, List<ItemBaixaRequest> Itens);

public record ItemBaixaResultado(string Codigo, int SaldoAnterior, int SaldoAtual);

public record BaixaEstoqueResponse(int NotaNumero, List<ItemBaixaResultado> Itens);
