import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { GuestGuard } from '../core/guest.guard';
import { ForgotPasswordComponent } from './forgot-password/forgot-password.component';
import { LoginComponent } from './login/login.component';
import { RegisterComponent } from './register/register.component';
import { ResetPasswordComponent } from './reset-password/reset-password.component';

const guestRoutes = {
  canActivate: [GuestGuard],
};

const routes: Routes = [
  { path: 'login', component: LoginComponent, ...guestRoutes },
  { path: 'register', component: RegisterComponent, ...guestRoutes },
  { path: 'forgot-password', component: ForgotPasswordComponent, ...guestRoutes },
  { path: 'reset-password', component: ResetPasswordComponent },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class AuthRoutingModule {}
