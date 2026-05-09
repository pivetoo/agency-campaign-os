export function cleanFormPayload<T extends object>(payload: T): T {
  const result = {} as Record<string, unknown>
  for (const [key, value] of Object.entries(payload)) {
    if (typeof value === 'string') {
      const trimmed = value.trim()
      result[key] = trimmed.length === 0 ? undefined : trimmed
      continue
    }
    result[key] = value
  }
  return result as T
}
