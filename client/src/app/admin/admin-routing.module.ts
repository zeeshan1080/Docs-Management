import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from '../core/auth.guard';
import { ManagementGuard } from '../core/management.guard';
import { AdminSettingsShellComponent } from './admin-settings-shell.component';
import { SettingsLocationsComponent } from './settings-locations.component';
import { SettingsRolesComponent } from './settings-roles.component';
import { RegistrationsComponent } from './registrations.component';
import { AdminUsersComponent } from './admin-users.component';

const routes: Routes = [
  {
    path: 'admin/users',
    component: AdminUsersComponent,
    canActivate: [AuthGuard, ManagementGuard],
  },
  {
    path: 'admin/settings',
    component: AdminSettingsShellComponent,
    canActivate: [AuthGuard, ManagementGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'locations' },
      { path: 'locations', component: SettingsLocationsComponent },
      { path: 'roles', component: SettingsRolesComponent },
    ],
  },
  {
    path: 'admin/registrations',
    component: RegistrationsComponent,
    canActivate: [AuthGuard, ManagementGuard],
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class AdminRoutingModule {}
