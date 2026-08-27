import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CriarProdutoRequest, Produto } from '../models/produto.model';

@Injectable({ providedIn: 'root' })
export class ProdutoService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.estoqueApiUrl}/produtos`;

  listar(): Observable<Produto[]> {
    return this.http.get<Produto[]>(this.baseUrl);
  }

  obterPorCodigo(codigo: string): Observable<Produto> {
    return this.http.get<Produto>(`${this.baseUrl}/${encodeURIComponent(codigo)}`);
  }

  criar(request: CriarProdutoRequest): Observable<Produto> {
    return this.http.post<Produto>(this.baseUrl, request);
  }
}
