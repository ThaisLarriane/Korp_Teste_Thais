import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { ProdutoService } from '../../core/services/produto.service';
import { Produto } from '../../core/models/produto.model';
import { extrairMensagemErro } from '../../core/utils/api-error';
import { AlertBanner } from '../../shared/alert-banner/alert-banner';

@Component({
  selector: 'app-produtos-page',
  imports: [FormsModule, AlertBanner],
  templateUrl: './produtos-page.html',
  styleUrl: './produtos-page.css',
})
export class ProdutosPage {
  private readonly produtoService = inject(ProdutoService);

  readonly produtos = signal<Produto[]>([]);
  readonly carregando = signal(true);
  readonly salvando = signal(false);
  readonly erroLista = signal<string | null>(null);
  readonly erroForm = signal<string | null>(null);
  readonly sucessoForm = signal<string | null>(null);

  codigo = '';
  descricao = '';
  saldo: number | null = 0;

  constructor() {
    this.carregarProdutos();
  }

  carregarProdutos(): void {
    this.carregando.set(true);
    this.erroLista.set(null);
    this.produtoService
      .listar()
      .pipe(finalize(() => this.carregando.set(false)))
      .subscribe({
        next: (produtos) =>
          this.produtos.set([...produtos].sort((a, b) => a.codigo.localeCompare(b.codigo))),
        error: (err) => this.erroLista.set(extrairMensagemErro(err, 'Não foi possível carregar os produtos.')),
      });
  }

  cadastrar(): void {
    this.erroForm.set(null);
    this.sucessoForm.set(null);

    if (!this.codigo.trim() || !this.descricao.trim()) {
      this.erroForm.set('Código e descrição são obrigatórios.');
      return;
    }

    if (this.saldo === null || this.saldo < 0) {
      this.erroForm.set('Informe um saldo inicial válido (0 ou maior).');
      return;
    }

    this.salvando.set(true);
    this.produtoService
      .criar({ codigo: this.codigo.trim(), descricao: this.descricao.trim(), saldo: this.saldo })
      .pipe(finalize(() => this.salvando.set(false)))
      .subscribe({
        next: (produto) => {
          this.sucessoForm.set(`Produto "${produto.codigo}" cadastrado com sucesso.`);
          this.codigo = '';
          this.descricao = '';
          this.saldo = 0;
          this.carregarProdutos();
        },
        error: (err) => this.erroForm.set(extrairMensagemErro(err, 'Não foi possível cadastrar o produto.')),
      });
  }
}
