import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { LoaderComponent } from './loader.component';
import { ErrorStateComponent } from './error-state.component';
import { EmptyStateComponent } from './empty-state.component';
import { PortalHeaderComponent } from './portal-header.component';

/**
 * Shared UI primitives for feature modules (forms, router directives, pipes).
 */
@NgModule({
  declarations: [LoaderComponent, ErrorStateComponent, EmptyStateComponent, PortalHeaderComponent],
  imports: [CommonModule, FormsModule, RouterModule],
  exports: [
    CommonModule,
    FormsModule,
    RouterModule,
    LoaderComponent,
    ErrorStateComponent,
    EmptyStateComponent,
    PortalHeaderComponent,
  ],
})
export class SharedModule {}
