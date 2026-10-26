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
  userId: number | null
}

export type ClientInput = Omit<Client, 'id' | 'status' | 'tierDiscountPercent' | 'isActive' | 'userId'> & { userId?: number | null }

export type OrderStatus = 'PendingApproval' | 'InProgress' | 'OnHold' | 'Completed' | 'Cancelled'
export type StepStatus = 'Waiting' | 'Ready' | 'InProgress' | 'OnHold' | 'Completed' | 'Rejected'

export interface Priority {
  id: number
  name: string
  rank: number
  cost: number
  isActive: boolean
}

export interface OrderItem {
  id: number
  catalogId: number
  catalogSku: string
  catalogName: string
  quantity: number
  description: string | null
  specs: { name: string; value: string }[]
  unitPrice: number
  catalogDiscountPercent: number
  clientDiscountPercent: number
  lineNet: number
  steps: { id: number; jobTypeId: number; jobTypeName: string; sortOrder: number; status: StepStatus }[]
}

export interface Order {
  id: number
  clientId: number
  clientCompanyName: string
  priorityId: number
  priorityName: string
  priorityCost: number
  commitmentDeadline: string | null
  reference: string | null
  notes: string | null
  subTotal: number
  discountTotal: number
  netAmount: number
  status: OrderStatus
  returnedToDepartmentId: number | null
  placedAt: string
  items: OrderItem[]
}

export interface OrderEvent {
  id: number
  type: 'Placed' | 'PriorityChangeRequested' | 'QuoteAdjusted' | 'Cancelled'
  actorUserId: number
  fromValue: string | null
  toValue: string | null
  reason: string | null
  at: string
}

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
  isActive: boolean
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

let accessToken: string | null = null
let userId: number | null = null
let signedOut: () => void = () => undefined

export const onSignedOut = (fn: () => void) => {
  signedOut = fn
}
export const currentUserId = () => userId

const adopt = (r: { accessToken: string; userId: number }) => {
  accessToken = r.accessToken
  userId = r.userId
}

const endSession = () => {
  accessToken = null
  userId = null
  signedOut()
}

let refreshing: Promise<boolean> | null = null
export function restoreSession(): Promise<boolean> {
  refreshing ??= fetch('/api/v1/auth/refresh', { method: 'POST' })
    .then(async (r) => {
      if (!r.ok) return false
      adopt(await r.json())
      return true
    })
    .catch(() => false)
    .finally(() => {
      refreshing = null
    })
  return refreshing
}

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
  dispatchEvent(new Event('api:failure'))
  return e
}

const isJson = (r: Response) => r.headers.get('content-type')?.includes('json') ?? false
// A gateway or dev proxy answering for a dead API sends plain text or nothing; real API errors are always problem+json.
const broken = (r: Response) => r.status !== 429 && (r.status >= 500 || (!r.ok && !isJson(r)))

async function unavailable(r: Response) {
  const problem = isJson(r) ? await r.json().catch(() => null) : null
  return fail(new ServiceError(problem ? 'server' : 'offline', typeof problem?.correlationId === 'string' ? problem.correlationId : undefined))
}

async function call<T>(path: string, method = 'GET', body?: unknown, retried = false): Promise<T> {
  const response = await fetch(`/api/v1${path}`, {
    method,
    headers: {
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
    },
    body: body === undefined ? undefined : JSON.stringify(body),
  }).catch(() => {
    throw fail(new ServiceError('offline'))
  })
  if (response.status === 401) {
    if (!retried && (await restoreSession())) return call<T>(path, method, body, true)
    endSession()
    throw new Error('Your session has expired. Sign in again.')
  }
  if (broken(response)) throw await unavailable(response)
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

export async function signIn(login: string, password: string): Promise<void> {
  const response = await fetch('/api/v1/auth/login', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ login, password }),
  }).catch(() => {
    throw fail(new ServiceError('offline'))
  })
  if (broken(response)) throw await unavailable(response)
  if (!response.ok) throw new Error(response.status === 429 ? 'Too many attempts. Wait a minute and try again.' : await problemMessage(response))
  adopt(await response.json())
}

export async function signOut(): Promise<void> {
  await fetch('/api/v1/auth/logout', { method: 'POST' }).catch(() => undefined)
  endSession()
}

export const api = {
  changePassword: async (currentPassword: string, newPassword: string) => {
    await call<void>('/auth/change-password', 'POST', { currentPassword, newPassword })
    endSession()
  },
  setUserPassword: (id: number, password: string) => call<void>(`/users/${id}/password`, 'PUT', { password }),

  list: () => call<Department[]>('/departments'),
  get: (id: number) => call<Department>(`/departments/${id}`),
  create: (input: DepartmentInput & { code: string }) => call<Department>('/departments', 'POST', input),
  update: (id: number, input: DepartmentInput) => call<Department>(`/departments/${id}`, 'PUT', input),
  deactivate: (id: number) => call<void>(`/departments/${id}/deactivate`, 'POST'),
  activate: (id: number) => call<void>(`/departments/${id}/activate`, 'POST'),
  departmentRoles: (id: number) => call<Role[]>(`/departments/${id}/roles`),
  assignDepartmentRoles: (id: number, roles: string[]) => call<Role[]>(`/departments/${id}/roles`, 'PUT', { roles }),

  users: () => call<User[]>('/users'),
  user: (id: number) => call<User>(`/users/${id}`),
  createUser: (input: UserInput & { departmentIds?: number[]; roles?: string[]; password?: string }) => call<User>('/users', 'POST', input),
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

  orders: () => call<Order[]>('/orders'),
  order: (id: number) => call<Order>(`/orders/${id}`),
  orderEvents: (id: number) => call<OrderEvent[]>(`/orders/${id}/events`),
  priorities: () => call<Priority[]>('/priorities'),
  placeOrder: (input: { clientId?: number; items: { catalogId: number; quantity: number; description?: string; specs?: { name: string; value: string }[] }[]; priorityId: number; commitmentDeadline?: string; reference?: string; notes?: string }) => call<Order>('/orders', 'POST', input),
  adjustOrderQuote: (id: number, total: number, reason: string) => call<Order>(`/orders/${id}/quote`, 'POST', { total, reason }),
  cancelOrder: (id: number, reason: string) => call<Order>(`/orders/${id}/cancel`, 'POST', { reason }),
  requestOrderPriority: (id: number, priorityId: number, reason?: string) => call<OrderEvent>(`/orders/${id}/priority-requests`, 'POST', { priorityId, reason }),

  jobTypes: () => call<JobType[]>('/job-types'),
  jobType: (id: number) => call<JobType>(`/job-types/${id}`),
  jobTypeCatalogs: (id: number) => call<Catalog[]>(`/job-types/${id}/catalogs`),
  createJobType: (input: JobTypeInput) => call<JobType>('/job-types', 'POST', input),
  updateJobType: (id: number, input: JobTypeInput) => call<JobType>(`/job-types/${id}`, 'PUT', input),
  deactivateJobType: (id: number) => call<void>(`/job-types/${id}/deactivate`, 'POST'),
  activateJobType: (id: number) => call<void>(`/job-types/${id}/activate`, 'POST'),

  catalogs: () => call<Catalog[]>('/catalogs'),
  catalog: (id: number) => call<Catalog>(`/catalogs/${id}`),
  createCatalog: (input: CatalogInput) => call<Catalog>('/catalogs', 'POST', input),
  updateCatalog: (id: number, input: CatalogInput) => call<Catalog>(`/catalogs/${id}`, 'PUT', input),
  deactivateCatalog: (id: number) => call<void>(`/catalogs/${id}/deactivate`, 'POST'),
  activateCatalog: (id: number) => call<void>(`/catalogs/${id}/activate`, 'POST'),
  cloneCatalog: (id: number, sku: string, name: string) => call<Catalog>(`/catalogs/${id}/clone`, 'POST', { sku, name }),

  chains: () => call<ApprovalChain[]>('/approval-chains'),
  chain: (id: number) => call<ApprovalChain>(`/approval-chains/${id}`),
  chainForCatalog: (catalogId: number) => call<ApprovalChain>(`/approval-chains/for-catalog/${catalogId}`),
  createChain: (input: ChainInput & { catalogId: number }) => call<ApprovalChain>('/approval-chains', 'POST', input),
  updateChain: (id: number, input: ChainInput) => call<ApprovalChain>(`/approval-chains/${id}`, 'PUT', input),
  deleteChain: (id: number) => call<void>(`/approval-chains/${id}`, 'DELETE'),
  deactivateChain: (id: number) => call<void>(`/approval-chains/${id}/deactivate`, 'POST'),
  activateChain: (id: number) => call<void>(`/approval-chains/${id}/activate`, 'POST'),
}
