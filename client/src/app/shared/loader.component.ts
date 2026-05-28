import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-loader',
  template: `
    <ng-container *ngIf="active">
      <div *ngIf="type === 'full'" class="fixed inset-0 z-[400] flex items-center justify-center bg-slate-900/35 backdrop-blur-[1px]">
        <div class="rounded-2xl border border-slate-200 bg-white px-6 py-5 shadow-2xl">
          <div class="flex items-center gap-3">
            <span class="h-5 w-5 animate-spin rounded-full border-2 border-portal-200 border-t-portal-600"></span>
            <span class="text-sm font-semibold text-slate-700">Loading...</span>
          </div>
        </div>
      </div>
      <div *ngIf="type === 'inline'" class="flex items-center gap-2 text-sm text-slate-600">
        <span class="h-4 w-4 animate-spin rounded-full border-2 border-portal-200 border-t-portal-600"></span>
        <span>Loading...</span>
      </div>
      <span *ngIf="type === 'button'" class="inline-flex items-center gap-2">
        <span class="h-4 w-4 animate-spin rounded-full border-2 border-white/40 border-t-white"></span>
        <span>Loading...</span>
      </span>
    </ng-container>
  `,
})
export class LoaderComponent {
  @Input() type: 'full' | 'inline' | 'button' = 'inline';
  @Input() active = false;
}
