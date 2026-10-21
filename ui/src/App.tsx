import { useEffect, useState } from 'react'
import { Catalogs } from './Catalogs'
import { Chains } from './Chains'
import { Clients } from './Clients'
import { Departments } from './Departments'
import { Roles } from './Roles'
import { Users } from './Users'
import { Icon } from './ui'

const pages = {
  departments: { title: 'Departments', icon: 'departments', view: <Departments /> },
  users: { title: 'Users', icon: 'users', view: <Users /> },
  roles: { title: 'Roles', icon: 'roles', view: <Roles /> },
  clients: { title: 'Clients', icon: 'clients', view: <Clients /> },
  catalogs: { title: 'Catalogs', icon: 'catalogs', view: <Catalogs /> },
  approvals: { title: 'Approvals', icon: 'chains', view: <Chains /> },
} as const

type Route = keyof typeof pages

const current = (): Route => {
  const name = location.hash.replace(/^#\//, '')
  return name in pages ? (name as Route) : 'departments'
}

export function App() {
  const [route, setRoute] = useState(current)

  useEffect(() => {
    const onChange = () => setRoute(current())
    addEventListener('hashchange', onChange)
    return () => removeEventListener('hashchange', onChange)
  }, [])

  return (
    <div className="shell">
      <nav aria-label="Main">
        <a className="brand" href="#/departments"><span className="logo">O</span><span>Orchestrator</span></a>
        <ul>
          {(Object.keys(pages) as Route[]).map((key) => (
            <li key={key}>
              <a href={`#/${key}`} aria-current={route === key ? 'page' : undefined}><Icon name={pages[key].icon} /><span>{pages[key].title}</span></a>
            </li>
          ))}
        </ul>
        <p className="acting">Acting as <strong>Super Admin</strong><small>dev mode, no login</small></p>
      </nav>
      <main key={route}>{pages[route].view}</main>
    </div>
  )
}
