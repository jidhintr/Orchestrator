import { useEffect, useMemo, useState } from 'react'
import { AccessContext, makeAccess, rankLabel } from './access'
import { api, currentUserId, onServiceError, onSignedOut, restoreSession, signOut, type ServiceError, type User } from './api'
import { Catalogs } from './Catalogs'
import { Chains } from './Chains'
import { Clients } from './Clients'
import { Dashboard } from './Dashboard'
import { Departments } from './Departments'
import { ErrorPage } from './ErrorPage'
import { JobTypes } from './JobTypes'
import { Login } from './Login'
import { Orders } from './Orders'
import { Profile } from './Profile'
import { Roles } from './Roles'
import { Users } from './Users'
import { Avatar, Icon } from './ui'

const pages = {
  dashboard: { title: 'Dashboard', icon: 'dashboard', view: <Dashboard /> },
  catalogs: { title: 'Catalogs', icon: 'catalogs', view: <Catalogs /> },
  'job-types': { title: 'Job types', icon: 'jobtypes', view: <JobTypes /> },
  approvals: { title: 'Approvals', icon: 'chains', view: <Chains /> },
  orders: { title: 'Orders', icon: 'orders', view: <Orders /> },
  clients: { title: 'Clients', icon: 'clients', view: <Clients /> },
  departments: { title: 'Departments', icon: 'departments', view: <Departments /> },
  users: { title: 'Users', icon: 'users', view: <Users /> },
  roles: { title: 'Roles', icon: 'roles', view: <Roles /> },
  profile: { title: 'Profile', icon: 'profile', view: <Profile /> },
} as const

type Route = keyof typeof pages

const groups: { title: string; routes: Route[] }[] = [
  { title: 'Overview', routes: ['dashboard'] },
  { title: 'Production', routes: ['catalogs', 'job-types', 'approvals'] },
  { title: 'Customers', routes: ['orders', 'clients'] },
  { title: 'People', routes: ['departments', 'users', 'roles'] },
]

const NEXT = 'next'
const remember = (hash: string) => {
  try { sessionStorage.setItem(NEXT, hash) } catch { /* private mode: sign in lands on the dashboard */ }
}
const recall = () => {
  try {
    const hash = sessionStorage.getItem(NEXT)
    sessionStorage.removeItem(NEXT)
    return hash
  } catch { return null }
}

const current = (): Route => {
  const name = location.hash.replace(/^#\//, '')
  return name in pages ? (name as Route) : 'dashboard'
}

export function App() {
  const [route, setRoute] = useState(current)
  const [me, setMe] = useState<User | null>(null)
  const [down, setDown] = useState<ServiceError | null>(null)
  const [attempt, setAttempt] = useState(0)
  const [session, setSession] = useState<'loading' | 'in' | 'out'>('loading')

  useEffect(() => {
    const onChange = () => setRoute(current())
    addEventListener('hashchange', onChange)
    return () => removeEventListener('hashchange', onChange)
  }, [])

  useEffect(() => {
    const onClick = (e: MouseEvent) => {
      const target = e.target as HTMLElement
      if (target.closest('button, a, input, select, label') || getSelection()?.toString()) return
      target.closest('tbody tr')?.querySelector<HTMLButtonElement>('button.link')?.click()
    }
    document.addEventListener('click', onClick)
    return () => document.removeEventListener('click', onClick)
  }, [])

  useEffect(() => {
    const recovered = () => {
      setDown(null)
      setAttempt((a) => a + 1)
    }
    addEventListener('api:recovered', recovered)
    return () => removeEventListener('api:recovered', recovered)
  }, [])

  useEffect(() => {
    onServiceError(setDown)
    onSignedOut(() => {
      setMe(null)
      setSession('out')
    })
    void restoreSession().then((ok) => setSession(ok ? 'in' : 'out'))
  }, [])

  useEffect(() => {
    if (session !== 'out') return
    if (location.hash && location.hash !== '#/login') remember(location.hash)
    location.hash = '#/login'
  }, [session])

  useEffect(() => {
    const id = currentUserId()
    if (session === 'in' && id !== null) api.user(id).then(setMe).catch(() => void signOut())
  }, [attempt, session])

  const access = useMemo(() => (me ? makeAccess(me) : null), [me])
  const name = me ? `${me.firstName} ${me.lastName}` : ''

  if (session === 'loading') return null
  if (session === 'out') return <Login onDone={() => { location.hash = recall() ?? '#/dashboard'; setSession('in') }} />
  if (down) return <ErrorPage kind={down.kind} reference={down.reference} onRetry={() => { setDown(null); setAttempt((a) => a + 1) }} />
  if (!access) return null

  return (
    <AccessContext value={access}>
    <div className="shell" key={attempt}>
      <nav aria-label="Main">
        <a className="brand" href="#/dashboard">
          <svg className="logo" viewBox="0 0 28 28" fill="none" stroke="currentColor" strokeWidth="1.6" aria-hidden="true">
            <circle cx="14" cy="14" r="8" /><path d="M14 1v26M1 14h26" /><circle className="c" cx="14" cy="14" r="3" fill="currentColor" stroke="none" />
          </svg>
          Orchestrator
        </a>
        <div className="scroll">
          {groups.map((g) => (
            <div className="group" key={g.title}>
              <h2>{g.title}</h2>
              <ul>
                {g.routes.map((key) => (
                  <li key={key}>
                    <a href={`#/${key}`} aria-current={route === key ? 'page' : undefined}><Icon name={pages[key].icon} /><span>{pages[key].title}</span></a>
                  </li>
                ))}
              </ul>
            </div>
          ))}
        </div>
        <a className="me" href="#/profile" aria-label="Your profile" aria-current={route === 'profile' ? 'page' : undefined}>
          <Avatar name={name} />
          <span className="stack"><strong>{name}</strong><small>{rankLabel(access.rank)}</small></span>
        </a>
        <button className="secondary small sign-out" onClick={() => void signOut()}>Sign out</button>
      </nav>
      <main key={route}>{pages[route].view}</main>
    </div>
    </AccessContext>
  )
}
