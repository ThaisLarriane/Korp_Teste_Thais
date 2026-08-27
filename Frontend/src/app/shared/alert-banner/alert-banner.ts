import { Component, input } from '@angular/core';

export type AlertKind = 'success' | 'error' | 'info';

@Component({
  selector: 'app-alert-banner',
  template: `
    @if (mensagem()) {
      <div class="alert-banner" [class]="'alert-banner--' + kind()">
        {{ mensagem() }}
      </div>
    }
  `,
  styles: `
    .alert-banner {
      padding: 0.75rem 1rem;
      border-radius: 6px;
      font-size: 0.9rem;
      margin-block-end: 1rem;
      border-inline-start: 4px solid transparent;
    }

    .alert-banner--success {
      background: #e7f6ee;
      color: #1e6b41;
      border-inline-start-color: #2f9e5f;
    }

    .alert-banner--error {
      background: #fdecec;
      color: #9a2b2b;
      border-inline-start-color: #d64545;
    }

    .alert-banner--info {
      background: #eef3fb;
      color: #2a4d7a;
      border-inline-start-color: #3f6fb0;
    }
  `,
})
export class AlertBanner {
  readonly mensagem = input<string | null>(null);
  readonly kind = input<AlertKind>('info');
}
