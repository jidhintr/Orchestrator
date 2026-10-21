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

export interface User {
  id: number
  firstName: string
  lastName: string
  email: string | null
  mobile: string
  departments: { id: number; name: string }[]
  departmentRoles: string[]
  roles: string[]
  isActive: boolean
}

export type ClientTier = 'Normal' | 'Gold' | 'Platinum'
export type ClientStatus = 'Active' | 'PaymentPending' | 'Suspended' | 'Inactive'

export interface Client {
  id: number
  companyName: string
  contactName: string
  email: string
  phone: string
  shippingAddress: string
  tier: ClientTier
  status: ClientStatus
  tierDiscountPercent: number
  isActive: boolean
}

export type ClientInput = Omit<Client, 'id' | 'status' | 'tierDiscountPercent' | 'isActive'>

export interface JobType {
  id: number
  name: string
  description: string | null
  laborCost: number | null
  commissionPercent: number | null
  profitPercent: number | null
  isActive: boolean
}

export type JobTypeInput = Omit<JobType, 'id' | 'isActive'>

export interface Catalog {
  id: number
  sku: string
  name: string
  description: string | null
  basePrice: number
  catalogDiscountPercent: number
  laborCost: number
  commissionPercent: number
  profitPercent: number
  isActive: boolean
  steps: { jobTypeId: number; jobTypeName: string; sequence: number }[]
}

export type CatalogInput = Omit<Catalog, 'id' | 'isActive' | 'steps'> & { jobTypeIds: number[] }

export interface ApprovalChain {
  id: number
  name: string
  description: string | null
  isDefault: boolean
  catalogId: number | null
  stages: { sequence: number; departmentId: number; departmentName: string }[]
}

export interface ChainInput {
  name: string
  description: string | null
  departmentIds: number[]
}

export interface DepartmentInput {
  name: string
  description: string | null
}

export interface RoleInput {
  name: string
  description: string
}

export interface UserInput {
  firstName: string
  lastName: string
  email: string | null
  mobile: string
}

// shortcut: hardcoded dev stand-in for authentication (1 = seeded super admin); remove when auth lands (ADR-0003).
export const ACTING_USER_ID = '1'

export class ServiceError extends Error {
  constructor(readonly kind: 'offline' | 'server', readonly reference?: string) {
    super('The service is unavailable. Try again in a moment.')
  }
}

let report: (e: ServiceError) => void = () => undefined
export const onServiceError = (fn: (e: ServiceError) => void) => {
  report = fn
}

const fail = (e: ServiceError) => {
  report(e)
  return e
}

async function call<T>(path: string, method = 'GET', body?: unknown): Promise<T> {
  const response = await fetch(`/api/v1${path}`, {
    method,
    headers: {
      'X-Acting-User-Id': ACTING_USER_ID,
      ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  }).catch(() => {
    throw fail(new ServiceError('offline'))
  })
  if (response.status >= 500) {
    const problem = response.headers.get('content-type')?.includes('json') ? await response.json().catch(() => null) : null
    throw fail(new ServiceError(problem ? 'server' : 'offline', typeof problem?.correlationId === 'string' ? problem.correlationId : undefined))
  }
  if (!response.ok) {
    throw new Error(await problemMessage(response))
  }
  return response.status === 204 ? (undefined as T) : ((await response.json()) as T)
}

async function problemMessage(response: Response): Promise<string> {
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
  departmentRoles: (id: number) => call<Role[]>(`/departments/${id}/roles`),
  assignDepartmentRoles: (id: number, roles: string[]) => call<Role[]>(`/departments/${id}/roles`, 'PUT', { roles }),

  users: () => call<User[]>('/users'),
  user: (id: number) => call<User>(`/users/${id}`),
  createUser: (input: UserInput & { departmentIds?: number[]; roles?: string[] }) => call<User>('/users', 'POST', input),
  updateUser: (id: number, input: UserInput & { isActive: boolean }) => call<User>(`/users/${id}`, 'PUT', input),
  assignUserDepartments: (id: number, departmentIds: number[]) => call<User>(`/users/${id}/departments`, 'PUT', { departmentIds }),
  assignUserRoles: (id: number, roles: string[]) => call<User>(`/users/${id}/roles`, 'PUT', { roles }),

  allRoles: () => call<Role[]>('/roles'),
  role: (id: number) => call<Role>(`/roles/${id}`),
  roleUsers: (id: number) => call<User[]>(`/roles/${id}/users`),
  createRole: (input: RoleInput) => call<Role>('/roles', 'POST', input),
  updateRole: (id: number, input: RoleInput) => call<Role>(`/roles/${id}`, 'PUT', input),
  disableRole: (id: number) => call<void>(`/roles/${id}/disable`, 'POST'),
  enableRole: (id: number) => call<void>(`/roles/${id}/enable`, 'POST'),
  deleteRole: (id: number) => call<void>(`/roles/${id}`, 'DELETE'),

  clients: () => call<Client[]>('/clients'),
  client: (id: number) => call<Client>(`/clients/${id}`),
  createClient: (input: ClientInput) => call<Client>('/clients', 'POST', input),
  updateClient: (id: number, input: ClientInput) => call<Client>(`/clients/${id}`, 'PUT', input),
  setClientStatus: (id: number, status: ClientStatus) => call<Client>(`/clients/${id}/status`, 'PUT', { status }),

  jobTypes: () => call<JobType[]>('/job-types'),
  jobType: (id: number) => call<JobType>(`/job-types/${id}`),
  jobTypeCatalogs: (id: number) => call<Catalog[]>(`/job-types/${id}/catalogs`),
  createJobType: (input: JobTypeInput) => call<JobType>('/job-types', 'POST', input),
  updateJobType: (id: number, input: JobTypeInput) => call<JobType>(`/job-types/${id}`, 'PUT', input),
  deactivateJobType: (id: number) => call<void>(`/job-types/${id}/deactivate`, 'POST'),

  catalogs: () => call<Catalog[]>('/catalogs'),
  catalog: (id: number) => call<Catalog>(`/catalogs/${id}`),
  createCatalog: (input: CatalogInput) => call<Catalog>('/catalogs', 'POST', input),
  updateCatalog: (id: number, input: CatalogInput) => call<Catalog>(`/catalogs/${id}`, 'PUT', input),
  deactivateCatalog: (id: number) => call<void>(`/catalogs/${id}/deactivate`, 'POST'),
  cloneCatalog: (id: number, sku: string, name: string) => call<Catalog>(`/catalogs/${id}/clone`, 'POST', { sku, name }),

  chains: () => call<ApprovalChain[]>('/approval-chains'),
  chain: (id: number) => call<ApprovalChain>(`/approval-chains/${id}`),
  chainForCatalog: (catalogId: number) => call<ApprovalChain>(`/approval-chains/for-catalog/${catalogId}`),
  createChain: (input: ChainInput & { catalogId: number }) => call<ApprovalChain>('/approval-chains', 'POST', input),
  updateChain: (id: number, input: ChainInput) => call<ApprovalChain>(`/approval-chains/${id}`, 'PUT', input),
  deleteChain: (id: number) => call<void>(`/approval-chains/${id}`, 'DELETE'),
}
