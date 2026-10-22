import { createContext, useContext } from 'react'
import type { User } from './api'

const ranks: Record<string, number> = { 'Super Admin': 4, Admin: 3, Manager: 2 }
const labels = ['', 'Employee', 'Manager', 'Admin', 'Super Admin']
// shortcut: mirrors the API's rank rules to hide controls; the API stays the authority and answers 403, revisit if rules move to claims.
const needs = { users: 2, roles: 3, departments: 3, jobTypes: 3, chains: 3, catalogs: 1, clients: 1, orders: 1, orderManage: 2 } as const

export type Area = keyof typeof needs
export interface Access { user: User; rank: number; can: (area: Area) => boolean }

export const rankLabel = (rank: number) => labels[rank]

export function makeAccess(user: User): Access {
  const rank = Math.max(1, ...[...user.roles, ...user.departmentRoles].map((r) => ranks[r] ?? 1))
  return { user, rank, can: (area) => rank >= needs[area] }
}

export const AccessContext = createContext<Access | null>(null)

export function useAccess() {
  const access = useContext(AccessContext)
  if (!access) throw new Error('useAccess needs a signed-in user.')
  return access
}
