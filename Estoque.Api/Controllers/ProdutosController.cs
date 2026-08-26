using Estoque.Api.Data;
using Estoque.Api.DTOs;
using Estoque.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Estoque.Api.Controllers;

[ApiController]
[Route("api/v1/produtos")]
public class ProdutosController : ControllerBase
{
    private readonly EstoqueDbContext _db;

    public ProdutosController(EstoqueDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<ActionResult<ProdutoResponse>> Criar(CriarProdutoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Codigo) || string.IsNullOrWhiteSpace(request.Descricao))
        {
            return BadRequest(new ErroResponse("DADOS_INVALIDOS", "Código e descrição são obrigatórios."));
        }

        if (request.Saldo < 0)
        {
            return BadRequest(new ErroResponse("DADOS_INVALIDOS", "Saldo inicial não pode ser negativo."));
        }

        var existente = await _db.Produtos.FindAsync(request.Codigo);
        if (existente is not null)
        {
            return Conflict(new ErroResponse("PRODUTO_DUPLICADO", $"Já existe um produto com o código '{request.Codigo}'."));
        }

        var produto = new Produto
        {
            Codigo = request.Codigo,
            Descricao = request.Descricao,
            Saldo = request.Saldo
        };

        _db.Produtos.Add(produto);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(ObterPorCodigo), new { codigo = produto.Codigo }, ProdutoResponse.FromEntity(produto));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProdutoResponse>>> Listar()
    {
        var produtos = await _db.Produtos.AsNoTracking().ToListAsync();
        return Ok(produtos.Select(ProdutoResponse.FromEntity));
    }

    [HttpGet("{codigo}")]
    public async Task<ActionResult<ProdutoResponse>> ObterPorCodigo(string codigo)
    {
        var produto = await _db.Produtos.AsNoTracking().FirstOrDefaultAsync(p => p.Codigo == codigo);
        if (produto is null)
        {
            return NotFound(new ErroResponse("PRODUTO_NAO_ENCONTRADO", $"Produto '{codigo}' não encontrado."));
        }

        return Ok(ProdutoResponse.FromEntity(produto));
    }

    [HttpPost("baixa-lote")]
    public async Task<ActionResult<BaixaEstoqueResponse>> BaixaLote(BaixaEstoqueRequest request)
    {
        if (request.Itens is null || request.Itens.Count == 0)
        {
            return BadRequest(new ErroResponse("DADOS_INVALIDOS", "Informe ao menos um item para dar baixa."));
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var codigos = request.Itens.Select(i => i.Codigo).Distinct().ToList();
        var produtos = await _db.Produtos
            .Where(p => codigos.Contains(p.Codigo))
            .ToDictionaryAsync(p => p.Codigo);

        var faltantes = codigos.Where(c => !produtos.ContainsKey(c)).ToList();
        if (faltantes.Count > 0)
        {
            return NotFound(new ErroResponse(
                "PRODUTO_NAO_ENCONTRADO",
                $"Produto(s) não encontrado(s): {string.Join(", ", faltantes)}."));
        }

        var insuficientes = request.Itens
            .Where(i => produtos[i.Codigo].Saldo < i.Quantidade)
            .ToList();
        if (insuficientes.Count > 0)
        {
            var detalhes = string.Join(", ", insuficientes.Select(i =>
                $"{i.Codigo} (disponível: {produtos[i.Codigo].Saldo}, solicitado: {i.Quantidade})"));
            return Conflict(new ErroResponse("SALDO_INSUFICIENTE", $"Saldo insuficiente para: {detalhes}."));
        }

        var resultados = new List<ItemBaixaResultado>();
        foreach (var item in request.Itens)
        {
            var produto = produtos[item.Codigo];
            var saldoAnterior = produto.Saldo;
            produto.Saldo -= item.Quantidade;
            resultados.Add(new ItemBaixaResultado(produto.Codigo, saldoAnterior, produto.Saldo));
        }

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return Ok(new BaixaEstoqueResponse(request.NotaNumero, resultados));
    }
}