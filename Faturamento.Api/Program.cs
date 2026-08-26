using Faturamento.Api.Data;
using Faturamento.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<FaturamentoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("FaturamentoDb")));

var origensPermitidas = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", policy =>
    {
        policy.WithOrigins(origensPermitidas)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// HttpClient nomeado para o Estoque.Api, com política de retry + circuit breaker (Polly).
// Retry: 3 tentativas com backoff exponencial (200ms, 400ms, 800ms) para erros transitórios.
// Circuit breaker: após 5 falhas seguidas, "abre o circuito" por 15s, evitando martelar
// um serviço que já está fora do ar e falhando rápido (com mensagem clara) nesse período.
var estoqueBaseUrl = builder.Configuration["EstoqueApi:BaseUrl"]
    ?? throw new InvalidOperationException("Configuração 'EstoqueApi:BaseUrl' não encontrada.");

builder.Services.AddHttpClient("EstoqueApi", client =>
{
    client.BaseAddress = new Uri(estoqueBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
})
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError() // 5xx e HttpRequestException
    .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
    .WaitAndRetryAsync(3, tentativa => TimeSpan.FromMilliseconds(200 * Math.Pow(2, tentativa - 1))))
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(15)));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FaturamentoDbContext>();
    db.Database.Migrate();
}

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseCors("AngularApp");
app.UseAuthorization();
app.MapControllers();

app.Run();
