import { useEffect, useState } from 'react'
import { ACTING_USER_ID, api, onServiceError, type ServiceError, type User } from './api'
import { Catalogs } from './Catalogs'
import { Chains } from './Chains'
import { Clients } from './Clients'
import { Dashboard } from './Dashboard'
import { Departments } from './Departments'
import { ErrorPage } from './ErrorPage'
import { JobTypes } from './JobTypes'
import { Profile } from './Profile'
import { Roles } from './Roles'
import { Users } from './Users'
import { Avatar, Icon } from './ui'

const pages = {
  dashboard: { title: 'Dashboard', icon: 'dashboard', view: <Dashboard /> },
  catalogs: { title: 'Catalogs', icon: 'catalogs', view: <Catalogs /> },
  'job-types': { title: 'Job types', icon: 'jobtypes', view: <JobTypes /> },
  approvals: { title: 'Approvals', icon: 'chains', view: <Chains /> },
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
  { title: 'Customers', routes: ['clients'] },
  { title: 'People', routes: ['departments', 'users', 'roles'] },
]

const current = (): Route => {
  const name = location.hash.replace(/^#\//, '')
  return name in pages ? (name as Route) : 'dashboard'
}

export function App() {
  const [route, setRoute] = useState(current)
  const [me, setMe] = useState<User | null>(null)
  const [down, setDown] = useState<ServiceError | null>(null)
  const [attempt, setAttempt] = useState(0)

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
    onServiceError(setDown)
  }, [])

  useEffect(() => {
    api.user(Number(ACTING_USER_ID)).then(setMe).catch(() => undefined)
  }, [attempt])

  const name = me ? `${me.firstName} ${me.lastName}` : 'Profile'

  if (down) return <ErrorPage kind={down.kind} reference={down.reference} onRetry={() => { setDown(null); setAttempt((a) => a + 1) }} />

  return (
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
          <span className="stack"><strong>{name}</strong><small>Dev mode, no login</small></span>
        </a>
      </nav>
      <main key={route}>{pages[route].view}</main>
    </div>
  )
}
