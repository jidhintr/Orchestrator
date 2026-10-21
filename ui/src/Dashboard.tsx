import { useEffect, useState } from 'react'
import { api, type ApprovalChain, type Catalog, type Client, type ClientStatus, type Department, type JobType, type Role, type User } from './api'
import { message, Page } from './ui'

interface Data { departments: Department[]; users: User[]; roles: Role[]; clients: Client[]; catalogs: Catalog[]; jobTypes: JobType[]; chains: ApprovalChain[] }

const statusColor: Record<ClientStatus, string> = { Active: 'var(--ok)', PaymentPending: 'var(--yellow)', Suspended: 'var(--bad)', Inactive: 'var(--line)' }
const statusLabel: Record<ClientStatus, string> = { Active: 'Active', PaymentPending: 'Payment pending', Suspended: 'Suspended', Inactive: 'Inactive' }

export function Dashboard() {
  const [data, setData] = useState<Data | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    Promise.all([api.list(), api.users(), api.allRoles(), api.clients(), api.catalogs(), api.jobTypes(), api.chains()])
      .then(([departments, users, roles, clients, catalogs, jobTypes, chains]) => setData({ departments, users, roles, clients, catalogs, jobTypes, chains }))
      .catch((e) => setError(message(e)))
  }, [])

  if (!data) return <Page title="Dashboard" subtitle="The press floor at a glance." action={null}>{error ? <p role="alert" className="error">{error}</p> : <p className="muted">Loading...</p>}</Page>

  const activeUsers = data.users.filter((u) => u.isActive)
  const headcount = (departmentId: number) => activeUsers.filter((u) => u.departments.some((d) => d.id === departmentId)).length
  const defaultChain = data.chains.find((c) => c.isDefault)
  const stations = defaultChain
    ? defaultChain.stages.map((s) => ({ id: s.departmentId, name: s.departmentName }))
    : data.departments.filter((d) => d.isActive).map((d) => ({ id: d.id, name: d.name }))

  const clientCount = (status: ClientStatus) => data.clients.filter((c) => c.status === status).length
  const statuses = (Object.keys(statusLabel) as ClientStatus[]).filter((s) => clientCount(s) > 0)

  const attention = [
    ...data.clients.filter((c) => c.status === 'PaymentPending' || c.status === 'Suspended').map((c) => ({ key: `c${c.id}`, href: '#/clients', text: c.companyName, tag: statusLabel[c.status] })),
    ...data.catalogs.filter((c) => c.isActive && c.steps.length === 0).map((c) => ({ key: `k${c.id}`, href: '#/catalogs', text: c.name, tag: 'No job steps' })),
    ...data.departments.filter((d) => d.isActive && headcount(d.id) === 0).map((d) => ({ key: `d${d.id}`, href: '#/departments', text: d.name, tag: 'No members' })),
  ]

  const ledger: [string, string, number][] = [
    ['Active users', '#/users', activeUsers.length],
    ['Departments', '#/departments', data.departments.filter((d) => d.isActive).length],
    ['Roles', '#/roles', data.roles.filter((r) => r.isActive).length],
    ['Catalog items', '#/catalogs', data.catalogs.filter((c) => c.isActive).length],
    ['Job types', '#/job-types', data.jobTypes.filter((j) => j.isActive).length],
    ['Approval chains', '#/approvals', data.chains.length],
  ]

  return (
    <Page title="Dashboard" subtitle="The press floor at a glance." action={null}>
      <section className="line" aria-label="Approval line">
        <h2>{defaultChain ? defaultChain.name : 'Departments'}</h2>
        <p>{defaultChain ? 'Default approval line, in order, with active members at each stage.' : 'No default approval chain yet. Showing active departments.'}</p>
        {stations.length === 0 ? <p className="muted">No departments yet.</p> : (
          <ol>
            {stations.map((s, i) => (
              <li key={`${s.id}-${i}`}><strong>{s.name}</strong><small>{headcount(s.id)} {headcount(s.id) === 1 ? 'member' : 'members'}</small></li>
            ))}
          </ol>
        )}
      </section>
      <div className="grid">
        <section className="panel">
          <h2>Needs attention</h2>
          {attention.length === 0 ? <p className="muted">Nothing needs attention.</p> : (
            <ul className="todo">
              {attention.slice(0, 8).map((a) => <li key={a.key}><a href={a.href}><span>{a.text}</span><span className="muted">{a.tag}</span></a></li>)}
            </ul>
          )}
          {attention.length > 8 && <p className="muted">and {attention.length - 8} more</p>}
        </section>
        <section className="panel">
          <h2>Clients</h2>
          {data.clients.length === 0 ? <p className="muted">No clients yet.</p> : (
            <>
              <div className="seg" role="img" aria-label="Clients by status">
                {statuses.map((s) => <span key={s} style={{ flex: clientCount(s), background: statusColor[s] }} />)}
              </div>
              <ul className="legend">
                {statuses.map((s) => <li key={s}><i style={{ background: statusColor[s] }} />{statusLabel[s]}<b>{clientCount(s)}</b></li>)}
              </ul>
            </>
          )}
        </section>
        <section className="panel">
          <h2>Directory</h2>
          <dl className="ledger">
            {ledger.map(([label, href, count]) => <div key={label}><dt><a href={href}>{label}</a></dt><dd>{count}</dd></div>)}
          </dl>
        </section>
      </div>
    </Page>
  )
}
