import { HttpErrorResponse } from '@angular/common/http';

/**
 * Extrai uma mensagem amigável de um erro de requisição HTTP.
 * As APIs (Estoque e Faturamento) retornam erros no formato { erro, mensagem }.
 * Em caso de falha de comunicação entre microsserviços (circuit breaker aberto,
 * timeout, serviço fora do ar), o Faturamento.Api responde 503 com essa mesma forma.
 */
export function extrairMensagemErro(err: unknown, fallback = 'Ocorreu um erro inesperado. Tente novamente.'): string {
  if (err instanceof HttpErrorResponse) {
    if (err.status === 0) {
      return 'Não foi possível conectar ao servidor. Verifique se os serviços estão em execução.';
    }

    const corpo = err.error as { mensagem?: string; erro?: string } | null;
    if (corpo?.mensagem) {
      return corpo.mensagem;
    }

    if (err.status === 503) {
      return 'Serviço temporariamente indisponível. Tente novamente em instantes.';
    }
  }

  return fallback;
}
