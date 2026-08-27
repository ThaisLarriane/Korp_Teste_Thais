import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { finalize } from 'rxjs';
import { NotaFiscalService } from '../../../core/services/nota-fiscal.service';
import { NotaFiscal } from '../../../core/models/nota-fiscal.model';
import { extrairMensagemErro } from '../../../core/utils/api-error';
import { AlertBanner } from '../../../shared/alert-banner/alert-banner';

@Component({
  selector: 'app-notas-list-page',
  imports: [RouterLink, DatePipe, AlertBanner],
  templateUrl: './notas-list-page.html',
  styleUrl: './notas-list-page.css',
})
export class NotasListPage {
  private readonly notaFiscalService = inject(NotaFiscalService);
  private readonly router = inject(Router);

  readonly notas = signal<NotaFiscal[]>([]);
  readonly carregando = signal(true);
  readonly criando = signal(false);
  readonly erro = signal<string | null>(null);

  constructor() {
    this.carregarNotas();
  }

  carregarNotas(): void {
    this.carregando.set(true);
    this.erro.set(null);
    this.notaFiscalService
      .listar()
      .pipe(finalize(() => this.carregando.set(false)))
      .subscribe({
        next: (notas) => this.notas.set(notas),
        error: (err) => this.erro.set(extrairMensagemErro(err, 'Não foi possível carregar as notas fiscais.')),
      });
  }

  novaNota(): void {
    this.criando.set(true);
    this.erro.set(null);
    this.notaFiscalService
      .criar()
      .pipe(finalize(() => this.criando.set(false)))
      .subscribe({
        next: (nota) => this.router.navigate(['/notas', nota.numero]),
        error: (err) => this.erro.set(extrairMensagemErro(err, 'Não foi possível criar a nota fiscal.')),
      });
  }
}
