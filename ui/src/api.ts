export interface Department {
  id: number
  name: string
  code: string
  description: string | null
  isActive: boolean
  isSystem: boolean
}

export interface Role {
  id: number
  name: string
  description: string
  isActive: boolean
  isSystem: boolean
}

export interface DepartmentInput {
  name: string
  description: string | null
}

// shortcut: hardcoded dev stand-in for authentication (1 = seeded super admin); remove when auth lands (ADR-0003).
const ACTING_USER_ID = '1'

async function call<T>(path: string, method = 'GET', body?: unknown): Promise<T> {
  const response = await fetch(`/api/v1${path}`, {
    method,
    headers: {
      'X-Acting-User-Id': ACTING_USER_ID,
      ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  }).catch(() => {
    throw new Error('Cannot reach the API. Is it running on port 5176?')
  })
  if (!response.ok) {
    throw new Error(await problemMessage(response))
  }
  return response.status === 204 ? (undefined as T) : ((await response.json()) as T)
}

async function problemMessage(response: Response): Promise<string> {
  if (response.status === 500 && !response.headers.get('content-type')?.includes('json')) {
    return 'Cannot reach the API. Is it running on port 5176?'
  }
  try {
    const problem = await response.json()
    const fields = Object.values((problem.errors ?? {}) as Record<string, string[]>).flat()
    return fields.length ? fields.join(' ') : (problem.detail ?? problem.title ?? `Request failed (${response.status}).`)
  } catch {
    return `Request failed (${response.status}).`
  }
}

export const api = {
  list: () => call<Department[]>('/departments'),
  get: (id: number) => call<Department>(`/departments/${id}`),
  create: (input: DepartmentInput & { code: string }) => call<Department>('/departments', 'POST', input),
  update: (id: number, input: DepartmentInput) => call<Department>(`/departments/${id}`, 'PUT', input),
  deactivate: (id: number) => call<void>(`/departments/${id}/deactivate`, 'POST'),
  roles: (id: number) => call<Role[]>(`/departments/${id}/roles`),
  assignRoles: (id: number, roles: string[]) => call<Role[]>(`/departments/${id}/roles`, 'PUT', { roles }),
  allRoles: () => call<Role[]>('/roles'),
}
