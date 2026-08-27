import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AdicionarItemRequest,
  ImprimirNotaFiscalResponse,
  NotaFiscal,
} from '../models/nota-fiscal.model';

@Injectable({ providedIn: 'root' })
export class NotaFiscalService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.faturamentoApiUrl}/notas-fiscais`;

  listar(): Observable<NotaFiscal[]> {
    return this.http.get<NotaFiscal[]>(this.baseUrl);
  }

  obterPorNumero(numero: number): Observable<NotaFiscal> {
    return this.http.get<NotaFiscal>(`${this.baseUrl}/${numero}`);
  }

  criar(): Observable<NotaFiscal> {
    return this.http.post<NotaFiscal>(this.baseUrl, {});
  }

  adicionarItem(numero: number, request: AdicionarItemRequest): Observable<NotaFiscal> {
    return this.http.post<NotaFiscal>(`${this.baseUrl}/${numero}/itens`, request);
  }

  removerItem(numero: number, codigoProduto: string): Observable<NotaFiscal> {
    return this.http.delete<NotaFiscal>(
      `${this.baseUrl}/${numero}/itens/${encodeURIComponent(codigoProduto)}`,
    );
  }

  imprimir(numero: number): Observable<ImprimirNotaFiscalResponse> {
    return this.http.post<ImprimirNotaFiscalResponse>(`${this.baseUrl}/${numero}/imprimir`, {});
  }
}
