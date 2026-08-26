using System.Net;
using System.Text.Json;
using Estoque.Api.DTOs;

namespace Estoque.Api.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;

    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado ao processar {Path}", context.Request.Path);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode= (int)HttpStatusCode.InternalServerError;

            var erro = new ErroResponse("ERRO_INTERNO", "Ocorreu um erro inesperado, tente novamente.");
            await context.Response.WriteAsync(JsonSerializer.Serialize(erro));
        }
    }
}