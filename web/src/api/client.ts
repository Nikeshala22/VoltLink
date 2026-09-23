const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080/api/v1'


const TOKEN_KEY = 'voltlink.token'


export class ApiError extends Error {
  public readonly status: number
  public readonly code: string

  constructor(status: number, code: string, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
  }

  
  get isUnauthorised(): boolean {
    return this.status === 401
  }

 
  get isForbidden(): boolean {
    return this.status === 403
  }
}


export function setToken(token: string | null): void {

  try {
    if (token === null) {
      sessionStorage.removeItem(TOKEN_KEY)
    } else {
      sessionStorage.setItem(TOKEN_KEY, token)
    }
  } catch {
    
  }
}


export function getToken(): string | null {
  try {
    return sessionStorage.getItem(TOKEN_KEY)
  } catch {
    return null
  }
}


interface ProblemDetails {
  title?: string
  status?: number
  detail?: string
  errorCode?: string
}


async function toApiError(response: Response): Promise<ApiError> {
  let code = 'UNKNOWN'
  let message = `Request failed with status ${response.status}.`

  
  try {
    const problem = (await response.json()) as ProblemDetails

    if (problem.errorCode) code = problem.errorCode
    else if (problem.title) code = problem.title

    if (problem.detail) message = problem.detail
  } catch {
    if (response.status === 401) {
      message = 'Your session has expired. Please sign in again.'
      code = 'UNAUTHORISED'
    } else if (response.status === 403) {
      message = 'You do not have permission to perform this action.'
      code = 'FORBIDDEN'
    }
  }

  return new ApiError(response.status, code, message)
}


async function request<T>(
  path: string,
  options: { method?: string; body?: unknown; signal?: AbortSignal } = {},
): Promise<T> {
  const { method = 'GET', body, signal } = options

  const headers: Record<string, string> = {}
  if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
  }

  const token = getToken()
  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  let response: Response
  try {
    response = await fetch(`${BASE_URL}${path}`, {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
      signal,
    })
  } catch (error) {
    
    if (error instanceof DOMException && error.name === 'AbortError') {
      throw error
    }

    
    throw new ApiError(
      0,
      'NETWORK_ERROR',
      `Could not reach the VoltLink service at ${BASE_URL}. ` +
        `Either the API is not running, or this page's address ` +
        `(${window.location.origin}) is not an allowed origin on the API.`,
    )
  }

  if (!response.ok) {
    const apiError = await toApiError(response)


    if (apiError.isUnauthorised) {
      setToken(null)
    }

    throw apiError
  }

  
  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}


export const api = {
  get: <T>(path: string, signal?: AbortSignal) => request<T>(path, { signal }),

  post: <T>(path: string, body?: unknown) => request<T>(path, { method: 'POST', body }),

  put: <T>(path: string, body?: unknown) => request<T>(path, { method: 'PUT', body }),

  patch: <T>(path: string, body?: unknown) => request<T>(path, { method: 'PATCH', body }),

  del: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
}


export function buildQuery(params: Record<string, string | number | boolean | undefined | null>): string {
  const search = new URLSearchParams()

  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === '') continue
    search.append(key, String(value))
  }

  const query = search.toString()
  return query ? `?${query}` : ''
}
