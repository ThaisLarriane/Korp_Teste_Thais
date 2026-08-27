export type StatusNotaFiscal = 'Aberta' | 'Fechada';

export interface ItemNotaFiscal {
  codigoProduto: string;
  descricaoProduto: string;
  quantidade: number;
}

export interface NotaFiscal {
  numero: number;
  status: StatusNotaFiscal;
  dataCriacao: string;
  dataFechamento: string | null;
  itens: ItemNotaFiscal[];
}

export interface AdicionarItemRequest {
  codigoProduto: string;
  quantidade: number;
}

export interface ItemBaixaResultado {
  codigo: string;
  saldoAnterior: number;
  saldoAtual: number;
}

export interface ImprimirNotaFiscalResponse {
  numero: number;
  status: StatusNotaFiscal;
  itens: ItemBaixaResultado[];
}
