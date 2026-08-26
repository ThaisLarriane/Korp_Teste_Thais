using Estoque.Api.Models;

namespace Estoque.Api.DTOs;

public record ProdutoResponse(string Codigo, string Descricao, int Saldo)
{
    public static ProdutoResponse FromEntity(Produto produto) =>
        new(produto.Codigo, produto.Descricao, produto.Saldo);
}