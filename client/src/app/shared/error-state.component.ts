import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-error-state',
  template: `
    <div class="rounded-2xl border border-red-200/80 bg-red-50/70 px-5 py-4">
      <div class="flex items-start justify-between gap-4">
        <div>
          <p class="text-sm font-semibold text-red-800">{{ title }}</p>
          <p class="mt-1 text-sm text-red-700">{{ message || defaultMessage }}</p>
        </div>
        <button
          type="button"
          (click)="retry.emit()"
          class="inline-flex shrink-0 items-center gap-1.5 rounded-lg border border-red-200 bg-white px-3 py-1.5 text-xs font-semibold text-red-700 hover:bg-red-50"
        >
          <svg class="h-3.5 w-3.5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2" aria-hidden="true">
            <path stroke-linecap="round" stroke-linejoin="round" d="M16.023 9.348h4.992v-.001M11.977 3.001v4.992m0 0h4.992m-4.993 0l3.181 3.183a8.25 8.25 0 01-11.668 0l3.18-3.183m4.993 0L12 3.001" />
          </svg>
          Retry
        </button>
      </div>
    </div>
  `,
})
export class ErrorStateComponent {
  @Input() message = '';
  @Input() type: 'network' | 'server' | 'permission' | 'not-found' = 'server';
  @Output() retry = new EventEmitter<void>();

  get title(): string {
    if (this.type === 'permission') return 'Access denied';
    if (this.type === 'not-found') return 'Not found';
    if (this.type === 'network') return 'Network issue';
    return 'Something went wrong';
  }

  get defaultMessage(): string {
    if (this.type === 'permission') return 'You do not have permission for this action.';
    if (this.type === 'not-found') return 'The requested resource was not found.';
    if (this.type === 'network') return 'Please check your connection and try again.';
    return 'Please try again.';
  }
}
