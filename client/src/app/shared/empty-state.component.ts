import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  template: `
    <div class="rounded-2xl border border-dashed border-slate-300/90 bg-white/70 px-6 py-10 text-center">
      <p class="text-base font-semibold text-slate-800">{{ title }}</p>
      <p class="mt-1 text-sm text-slate-500">{{ description }}</p>
      <button *ngIf="actionLabel" type="button" (click)="action.emit()" class="mt-4 rounded-lg border border-slate-200 bg-white px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50">
        {{ actionLabel }}
      </button>
    </div>
  `,
})
export class EmptyStateComponent {
  @Input() title = 'No data';
  @Input() description = 'Nothing to display right now.';
  @Input() actionLabel = '';
  @Output() action = new EventEmitter<void>();
}
