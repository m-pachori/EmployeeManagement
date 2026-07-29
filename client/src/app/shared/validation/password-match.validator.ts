import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/**
 * Cross-field validator for a FormGroup: sets a `mismatch` error on the confirm-password
 * control when it doesn't equal the password control's value, so the error can be shown
 * as a normal field-level message on the confirm field.
 */
export function passwordsMatchValidator(passwordKey: string, confirmKey: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const password = group.get(passwordKey);
    const confirm = group.get(confirmKey);

    if (!password || !confirm) {
      return null;
    }

    if (confirm.value && password.value !== confirm.value) {
      confirm.setErrors({ ...confirm.errors, mismatch: true });
    } else if (confirm.hasError('mismatch')) {
      const { mismatch, ...rest } = confirm.errors ?? {};
      confirm.setErrors(Object.keys(rest).length ? rest : null);
    }

    return null;
  };
}
