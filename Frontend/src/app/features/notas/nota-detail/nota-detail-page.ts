import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize, forkJoin } from 'rxjs';
import { NotaFiscalService } from '../../../core/services/nota-fiscal.service';
import { ProdutoService } from '../../../core/services/produto.service';
import { NotaFiscal } from '../../../core/models/nota-fiscal.model';
import { Produto } from '../../../core/models/produto.model';
import { extrairMensagemErro } from '../../../core/utils/api-error';
import { AlertBanner } from '../../../shared/alert-banner/alert-banner';

@Component({
  selector: 'app-nota-detail-page',
  imports: [RouterLink, DatePipe, FormsModule, AlertBanner],
  templateUrl: './nota-detail-page.html',
  styleUrl: './nota-detail-page.css',
})
export class NotaDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly notaFiscalService = inject(NotaFiscalService);
  private readonly produtoService = inject(ProdutoService);

  readonly numero = Number(this.route.snapshot.paramMap.get('numero'));

  readonly nota = signal<NotaFiscal | null>(null);
  readonly produtos = signal<Produto[]>([]);
  readonly carregando = signal(true);
  readonly erroCarregar = signal<string | null>(null);

  readonly adicionando = signal(false);
  readonly erroAdicionar = signal<string | null>(null);

  readonly removendoCodigo = signal<string | null>(null);

  readonly imprimindo = signal(false);
  readonly erroImprimir = signal<string | null>(null);
  readonly sucessoImprimir = signal<string | null>(null);

  codigoProdutoSelecionado = '';
  quantidade: number | null = 1;

  constructor() {
    this.carregarTudo();
  }

  get notaAberta(): boolean {
    return this.nota()?.status === 'Aberta';
  }

  carregarTudo(): void {
    this.carregando.set(true);
    this.erroCarregar.set(null);

    forkJoin({
      nota: this.notaFiscalService.obterPorNumero(this.numero),
      produtos: this.produtoService.listar(),
    })
      .pipe(finalize(() => this.carregando.set(false)))
      .subscribe({
        next: ({ nota, produtos }) => {
          this.nota.set(nota);
          this.produtos.set([...produtos].sort((a, b) => a.codigo.localeCompare(b.codigo)));
        },
        error: (err) => this.erroCarregar.set(extrairMensagemErro(err, 'Não foi possível carregar a nota fiscal.')),
      });
  }

  adicionarItem(): void {
    this.erroAdicionar.set(null);

    if (!this.codigoProdutoSelecionado) {
      this.erroAdicionar.set('Selecione um produto.');
      return;
    }

    if (this.quantidade === null || this.quantidade <= 0) {
      this.erroAdicionar.set('Informe uma quantidade maior que zero.');
      return;
    }

    this.adicionando.set(true);
    this.notaFiscalService
      .adicionarItem(this.numero, {
        codigoProduto: this.codigoProdutoSelecionado,
        quantidade: this.quantidade,
      })
      .pipe(finalize(() => this.adicionando.set(false)))
      .subscribe({
        next: (nota) => {
          this.nota.set(nota);
          this.codigoProdutoSelecionado = '';
          this.quantidade = 1;
        },
        error: (err) => this.erroAdicionar.set(extrairMensagemErro(err, 'Não foi possível adicionar o item.')),
      });
  }

  removerItem(codigoProduto: string): void {
    this.removendoCodigo.set(codigoProduto);
    this.erroAdicionar.set(null);
    this.notaFiscalService
      .removerItem(this.numero, codigoProduto)
      .pipe(finalize(() => this.removendoCodigo.set(null)))
      .subscribe({
        next: (nota) => this.nota.set(nota),
        error: (err) => this.erroAdicionar.set(extrairMensagemErro(err, 'Não foi possível remover o item.')),
      });
  }

  imprimir(): void {
    this.erroImprimir.set(null);
    this.sucessoImprimir.set(null);
    this.imprimindo.set(true);

    this.notaFiscalService
      .imprimir(this.numero)
      .pipe(finalize(() => this.imprimindo.set(false)))
      .subscribe({
        next: (resultado) => {
          this.sucessoImprimir.set(`Nota #${resultado.numero} impressa e fechada com sucesso.`);
          const notaAtual = this.nota();
          if (notaAtual) {
            this.nota.set({ ...notaAtual, status: 'Fechada' });
          }
          // Recarrega para obter data de fechamento e refletir saldos atualizados dos produtos.
          this.carregarTudo();
        },
        error: (err) => this.erroImprimir.set(extrairMensagemErro(err, 'Não foi possível imprimir a nota fiscal.')),
      });
  }

  voltar(): void {
    this.router.navigate(['/notas']);
  }
}
