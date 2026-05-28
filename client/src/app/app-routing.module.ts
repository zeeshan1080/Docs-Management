import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

/**
 * Shell routes only. Feature routes live in Auth, Documents, and Admin modules.
 */
const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'documents' },
  { path: '**', redirectTo: 'documents' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
