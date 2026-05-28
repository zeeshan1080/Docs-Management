import { NgModule } from '@angular/core';
import { SharedModule } from '../shared/shared.module';
import { AdminRoutingModule } from './admin-routing.module';
import { AdminSettingsShellComponent } from './admin-settings-shell.component';
import { SettingsLocationsComponent } from './settings-locations.component';
import { SettingsRolesComponent } from './settings-roles.component';
import { RegistrationsComponent } from './registrations.component';
import { AdminUsersComponent } from './admin-users.component';

@NgModule({
  declarations: [
    AdminSettingsShellComponent,
    SettingsLocationsComponent,
    SettingsRolesComponent,
    RegistrationsComponent,
    AdminUsersComponent,
  ],
  imports: [SharedModule, AdminRoutingModule],
})
export class AdminModule {}
