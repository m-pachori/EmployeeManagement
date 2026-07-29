import { AbstractControl } from '@angular/forms';

/**
 * Resolves a human-readable, field-level validation message for a form control.
 * Returns an empty string when the control is valid, missing, or hasn't been
 * interacted with yet (so messages don't appear before the user has a chance
 * to fill the field in).
 */
export function resolveFieldError(
  control: AbstractControl | null | undefined,
  label: string,
  patternMessage?: string
): string {
  if (!control || !control.errors || !(control.touched || control.dirty)) {
    return '';
  }

  const errors = control.errors;

  if (errors['required']) {
    return `${label} is required.`;
  }

  if (errors['email']) {
    return 'Enter a valid email address.';
  }

  if (errors['minlength']) {
    return `${label} must be at least ${errors['minlength'].requiredLength} characters.`;
  }

  if (errors['maxlength']) {
    return `${label} must not exceed ${errors['maxlength'].requiredLength} characters.`;
  }

  if (errors['min']) {
    return `${label} must be ${errors['min'].min} or greater.`;
  }

  if (errors['max']) {
    return `${label} must be ${errors['max'].max} or less.`;
  }

  if (errors['pattern']) {
    return patternMessage ?? `${label} format is invalid.`;
  }

  if (errors['mismatch']) {
    return patternMessage ?? `${label} does not match.`;
  }

  return `${label} is invalid.`;
}
