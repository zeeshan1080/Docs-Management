import { Injectable } from '@angular/core';
import { swalToastError, swalToastSuccess, swalToastWarning } from './swal';

@Injectable({ providedIn: 'root' })
export class ToastService {
  success(message: string): void {
    swalToastSuccess(message);
  }

  error(message: string): void {
    swalToastError(message);
  }

  info(message: string): void {
    swalToastSuccess(message);
  }

  warning(message: string): void {
    swalToastWarning(message);
  }
}
