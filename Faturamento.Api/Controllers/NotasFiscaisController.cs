using System.Net;
using System.Net.Http.Json;
using Faturamento.Api.Data;
using Faturamento.Api.DTOs;
using Faturamento.Api.DTOs.Estoque;
using Faturamento.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Polly.CircuitBreaker;

namespace Faturamento.Api.Controllers;

[ApiController]
[Route("api/v1/notas-fiscais")]
public class NotasFiscaisController : ControllerBase
{
    private readonly FaturamentoDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NotasFiscaisController> _logger;

    private const string ErroServicoIndisponivel =
        "O serviço de Estoque está indisponível no momento. Tente novamente em instantes.";

    public NotasFiscaisController(
        FaturamentoDbContext db,
        IHttpClientFactory httpClientFactory,
        ILogger<NotasFiscaisController> logger)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<NotaFiscalResponse>> Criar()
    {
        var nota = new NotaFiscal();
        _db.NotasFiscais.Add(nota);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(ObterPorNumero), new { numero = nota.Numero }, NotaFiscalResponse.FromEntity(nota));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotaFiscalResponse>>> Listar()
    {
        var notas = await _db.NotasFiscais
            .Include(n => n.Itens)
            .AsNoTracking()
            .OrderByDescending(n => n.Numero)
            .ToListAsync();

        return Ok(notas.Select(NotaFiscalResponse.FromEntity));
    }

    [HttpGet("{numero:int}")]
    public async Task<ActionResult<NotaFiscalResponse>> ObterPorNumero(int numero)
    {
        var nota = await _db.NotasFiscais
            .Include(n => n.Itens)
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Numero == numero);

        if (nota is null)
        {
            return NotFound(new ErroResponse("NOTA_NAO_ENCONTRADA", $"Nota fiscal {numero} não encontrada."));
        }

        return Ok(NotaFiscalResponse.FromEntity(nota));
    }

    [HttpPost("{numero:int}/itens")]
    public async Task<ActionResult<NotaFiscalResponse>> AdicionarItem(int numero, AdicionarItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CodigoProduto))
        {
            return BadRequest(new ErroResponse("DADOS_INVALIDOS", "Código do produto é obrigatório."));
        }

        if (request.Quantidade <= 0)
        {
            return BadRequest(new ErroResponse("DADOS_INVALIDOS", "Quantidade deve ser maior que zero."));
        }

        var nota = await _db.NotasFiscais
            .Include(n => n.Itens)
            .FirstOrDefaultAsync(n => n.Numero == numero);

        if (nota is null)
        {
            return NotFound(new ErroResponse("NOTA_NAO_ENCONTRADA", $"Nota fiscal {numero} não encontrada."));
        }

        if (nota.Status != StatusNotaFiscal.Aberta)
        {
            return Conflict(new ErroResponse("NOTA_NAO_ABERTA", "Só é possível adicionar itens a uma nota com status Aberta."));
        }

        // Consulta o produto no Estoque.Api para validar existência e obter a descrição (snapshot).
        var client = _httpClientFactory.CreateClient("EstoqueApi");
        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync($"api/v1/produtos/{Uri.EscapeDataString(request.CodigoProduto)}");
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Circuito aberto ao consultar produto {Codigo} no Estoque.Api", request.CodigoProduto);
            return StatusCode((int)HttpStatusCode.ServiceUnavailable,
                new ErroResponse("SERVICO_ESTOQUE_INDISPONIVEL", ErroServicoIndisponivel));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Falha de comunicação com o Estoque.Api ao consultar produto {Codigo}", request.CodigoProduto);
            return StatusCode((int)HttpStatusCode.ServiceUnavailable,
                new ErroResponse("SERVICO_ESTOQUE_INDISPONIVEL", ErroServicoIndisponivel));
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return NotFound(new ErroResponse("PRODUTO_NAO_ENCONTRADO", $"Produto '{request.CodigoProduto}' não encontrado no Estoque."));
        }

        if (!response.IsSuccessStatusCode)
        {
            var erroEstoque = await response.Content.ReadFromJsonAsync<ErroResponse>();
            return StatusCode((int)response.StatusCode,
                erroEstoque ?? new ErroResponse("ERRO_ESTOQUE", "Erro ao consultar produto no Estoque."));
        }

        var produto = await response.Content.ReadFromJsonAsync<ProdutoEstoqueResponse>();
        if (produto is null)
        {
            return StatusCode((int)HttpStatusCode.BadGateway,
                new ErroResponse("RESPOSTA_INVALIDA", "Resposta inesperada do serviço de Estoque."));
        }

        var itemExistente = nota.Itens.FirstOrDefault(i => i.CodigoProduto == produto.Codigo);
        if (itemExistente is not null)
        {
            itemExistente.Quantidade += request.Quantidade;
            itemExistente.DescricaoProduto = produto.Descricao;
        }
        else
        {
            nota.Itens.Add(new ItemNotaFiscal
            {
                CodigoProduto = produto.Codigo,
                DescricaoProduto = produto.Descricao,
                Quantidade = request.Quantidade
            });
        }

        await _db.SaveChangesAsync();

        return Ok(NotaFiscalResponse.FromEntity(nota));
    }

    [HttpDelete("{numero:int}/itens/{codigoProduto}")]
    public async Task<ActionResult<NotaFiscalResponse>> RemoverItem(int numero, string codigoProduto)
    {
        var nota = await _db.NotasFiscais
            .Include(n => n.Itens)
            .FirstOrDefaultAsync(n => n.Numero == numero);

        if (nota is null)
        {
            return NotFound(new ErroResponse("NOTA_NAO_ENCONTRADA", $"Nota fiscal {numero} não encontrada."));
        }

        if (nota.Status != StatusNotaFiscal.Aberta)
        {
            return Conflict(new ErroResponse("NOTA_NAO_ABERTA", "Só é possível remover itens de uma nota com status Aberta."));
        }

        var item = nota.Itens.FirstOrDefault(i => i.CodigoProduto == codigoProduto);
        if (item is null)
        {
            return NotFound(new ErroResponse("ITEM_NAO_ENCONTRADO", $"Item com código '{codigoProduto}' não encontrado na nota."));
        }

        nota.Itens.Remove(item);
        _db.ItensNotaFiscal.Remove(item);
        await _db.SaveChangesAsync();

        return Ok(NotaFiscalResponse.FromEntity(nota));
    }

    [HttpPost("{numero:int}/imprimir")]
    public async Task<ActionResult<ImprimirNotaFiscalResponse>> Imprimir(int numero)
    {
        var nota = await _db.NotasFiscais
            .Include(n => n.Itens)
            .FirstOrDefaultAsync(n => n.Numero == numero);

        if (nota is null)
        {
            return NotFound(new ErroResponse("NOTA_NAO_ENCONTRADA", $"Nota fiscal {numero} não encontrada."));
        }

        if (nota.Status != StatusNotaFiscal.Aberta)
        {
            return Conflict(new ErroResponse("NOTA_NAO_ABERTA", "Só é possível imprimir notas com status Aberta."));
        }

        if (nota.Itens.Count == 0)
        {
            return BadRequest(new ErroResponse("NOTA_SEM_ITENS", "Adicione ao menos um item antes de imprimir a nota."));
        }

        var payload = new BaixaEstoqueRequest(
            nota.Numero,
            nota.Itens.Select(i => new ItemBaixaRequest(i.CodigoProduto, i.Quantidade)).ToList());

        var client = _httpClientFactory.CreateClient("EstoqueApi");
        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync("api/v1/produtos/baixa-lote", payload);
        }
        catch (BrokenCircuitException)
        {
            _logger.LogWarning("Circuito aberto ao tentar dar baixa no Estoque.Api para a nota {Numero}", numero);
            return StatusCode((int)HttpStatusCode.ServiceUnavailable,
                new ErroResponse("SERVICO_ESTOQUE_INDISPONIVEL", ErroServicoIndisponivel));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Falha de comunicação com o Estoque.Api ao imprimir a nota {Numero}", numero);
            return StatusCode((int)HttpStatusCode.ServiceUnavailable,
                new ErroResponse("SERVICO_ESTOQUE_INDISPONIVEL", ErroServicoIndisponivel));
        }

        if (!response.IsSuccessStatusCode)
        {
            var erroEstoque = await response.Content.ReadFromJsonAsync<ErroResponse>();
            _logger.LogWarning(
                "Estoque.Api recusou a baixa da nota {Numero}: {StatusCode} - {Erro}",
                numero, response.StatusCode, erroEstoque?.Mensagem);

            return StatusCode((int)response.StatusCode,
                erroEstoque ?? new ErroResponse("ERRO_ESTOQUE", "Erro ao dar baixa no estoque."));
        }

        var baixa = await response.Content.ReadFromJsonAsync<BaixaEstoqueResponse>();
        if (baixa is null)
        {
            return StatusCode((int)HttpStatusCode.BadGateway,
                new ErroResponse("RESPOSTA_INVALIDA", "Resposta inesperada do serviço de Estoque."));
        }

        nota.Status = StatusNotaFiscal.Fechada;
        nota.DataFechamento = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new ImprimirNotaFiscalResponse(nota.Numero, nota.Status.ToString(), baixa.Itens));
    }
}
