import Swal from 'sweetalert2';
import type { SweetAlertOptions } from 'sweetalert2';

/** Tailwind-aligned dialog chrome (use with `buttonsStyling: false`). */
const dmDialogClass: NonNullable<SweetAlertOptions['customClass']> = {
  popup: 'swal2-dm-popup',
  title: 'swal2-dm-title',
  htmlContainer: 'swal2-dm-html',
  confirmButton: 'swal2-dm-confirm',
  cancelButton: 'swal2-dm-cancel',
  actions: 'swal2-dm-actions',
};

const dmDialogBase: Pick<
  SweetAlertOptions,
  'customClass' | 'buttonsStyling' | 'heightAuto' | 'reverseButtons'
> = {
  customClass: dmDialogClass,
  buttonsStyling: false,
  heightAuto: false,
  reverseButtons: true,
};

type SweetAlertResult = Awaited<ReturnType<typeof Swal.fire>>;

function dialog(opts: SweetAlertOptions): Promise<SweetAlertResult> {
  const extra =
    typeof opts.customClass === 'object' && opts.customClass ? opts.customClass : {};
  return Swal.fire({
    ...dmDialogBase,
    ...opts,
    customClass: { ...dmDialogClass, ...extra },
  });
}

export function swalSuccess(title: string, text?: string): Promise<void> {
  return dialog({
    icon: 'success',
    iconColor: '#059669',
    title,
    text: text || undefined,
    confirmButtonText: 'OK',
    showConfirmButton: true,
  }).then(() => undefined);
}

export function swalError(title: string, text?: string): Promise<void> {
  return dialog({
    icon: 'error',
    iconColor: '#dc2626',
    title,
    text: text || undefined,
    confirmButtonText: 'OK',
    showConfirmButton: true,
  }).then(() => undefined);
}

export function swalWarning(title: string, text?: string): Promise<void> {
  return dialog({
    icon: 'warning',
    iconColor: '#d97706',
    title,
    text: text || undefined,
    confirmButtonText: 'OK',
    showConfirmButton: true,
  }).then(() => undefined);
}

/** Confirm a destructive action (default button label: Delete). */
export function swalConfirmDelete(title: string, text: string): Promise<boolean> {
  return dialog({
    icon: 'warning',
    iconColor: '#d97706',
    title,
    text,
    showCancelButton: true,
    confirmButtonText: 'Delete',
    cancelButtonText: 'Cancel',
    focusCancel: true,
  }).then((r) => r.isConfirmed);
}

/** Confirm removal (shares, files, etc.). */
export function swalConfirmRemove(title: string, text: string): Promise<boolean> {
  return dialog({
    icon: 'warning',
    iconColor: '#d97706',
    title,
    text,
    showCancelButton: true,
    confirmButtonText: 'Remove',
    cancelButtonText: 'Cancel',
    focusCancel: true,
  }).then((r) => r.isConfirmed);
}

/** Generic confirm (reject flow, etc.). */
export function swalConfirm(
  title: string,
  text: string,
  confirmButtonText: string,
  icon: 'warning' | 'question' = 'warning'
): Promise<boolean> {
  return dialog({
    icon,
    iconColor: icon === 'question' ? '#3d9bd9' : '#d97706',
    title,
    text,
    showCancelButton: true,
    confirmButtonText,
    cancelButtonText: 'Cancel',
    focusCancel: true,
  }).then((r) => r.isConfirmed);
}

const toastBase = Swal.mixin({
  toast: true,
  position: 'top-end',
  showConfirmButton: false,
  buttonsStyling: false,
  customClass: {
    popup: 'swal2-dm-toast',
    title: 'swal2-dm-toast-title',
  },
});

export function swalToastSuccess(title: string): void {
  void toastBase.fire({
    icon: 'success',
    iconColor: '#059669',
    title,
    timer: 3500,
    timerProgressBar: true,
  });
}

export function swalToastWarning(title: string): void {
  void toastBase.fire({
    icon: 'warning',
    iconColor: '#d97706',
    title,
    timer: 4500,
    timerProgressBar: true,
  });
}

export function swalToastError(title: string): void {
  void toastBase.fire({
    icon: 'error',
    iconColor: '#dc2626',
    title,
    timer: 5000,
    timerProgressBar: true,
  });
}
