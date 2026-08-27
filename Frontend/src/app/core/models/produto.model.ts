export interface Produto {
  codigo: string;
  descricao: string;
  saldo: number;
}

export interface CriarProdutoRequest {
  codigo: string;
  descricao: string;
  saldo: number;
}

export interface ErroResponse {
  erro: string;
  mensagem: string;
}
