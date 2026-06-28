import type { ProblemDetails, ValidationProblemDetails } from '../types/api.types'

const FALLBACK_MESSAGE = 'Something went wrong. Please try again.'
const NETWORK_MESSAGE = 'Could not reach the server. Check your connection and try again.'

export function isProblemDetails(error: unknown): error is ProblemDetails {
  return typeof error === 'object' && error !== null && 'status' in error && 'title' in error
}

export function isValidationProblemDetails(error: unknown): error is ValidationProblemDetails {
  return isProblemDetails(error) && 'errors' in error
}

// Top-level message for a banner/alert — covers 401/403/404/409/500 ProblemDetails and network errors.
export function getErrorMessage(error: unknown): string {
  if (isProblemDetails(error)) return (error as { detail?: string }).detail ?? error.title ?? FALLBACK_MESSAGE
  if (error instanceof Error) return NETWORK_MESSAGE
  return FALLBACK_MESSAGE
}

// FluentValidation's PropertyName is serialized PascalCase (e.g. "DisplayName"),
// while form fields are camelCase — lowercase the first letter to line them up.
export function getFieldErrors(error: unknown): Record<string, string> | null {
  if (!isValidationProblemDetails(error)) return null

  return Object.fromEntries(
    Object.entries(error.errors).map(([property, messages]) => [
      property.charAt(0).toLowerCase() + property.slice(1),
      messages[0] ?? FALLBACK_MESSAGE,
    ]),
  )
}
