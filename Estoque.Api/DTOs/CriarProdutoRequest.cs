namespace Estoque.Api.DTOs;

public record CriarProdutoRequest(string Codigo, string Descricao, int Saldo);