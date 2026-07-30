/**
 * Extracts a user-facing message from an HttpErrorResponse-shaped error, falling back to a
 * default message. Centralizes the ProblemDetails/validation-error parsing that was previously
 * duplicated across feature components.
 */
export function extractErrorMessage(error: any, fallback: string): string {
  const apiError = error?.error;

  if (apiError?.errors && typeof apiError.errors === 'object') {
    const firstEntry = Object.values(apiError.errors)[0] as string[] | undefined;
    if (Array.isArray(firstEntry) && firstEntry.length > 0) {
      return firstEntry[0];
    }
  }

  return apiError?.title ?? apiError?.message ?? fallback;
}
