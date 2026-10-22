import { useAccess } from './access'
import { type FormEvent, useCallback, useEffect, useState } from 'react'
import { api, type Department, type Role, type User } from './api'
import { Avatar, Chips, ChipPicker, Filters, Icon, matches, message, Modal, Page, Select, StatusBadges, toggled, locked, useList, useRunner, useToast, Form, sameSet, Submit } from './ui'

type Panel = { mode: 'create' } | { mode: 'detail'; id: number; edit?: boolean } | null
type Lookups = { departments: Department[]; roles: Role[] }

const fullName = (u: User) => `${u.firstName} ${u.lastName}`
const effectiveRoles = (u: User) => [...new Set([...u.departmentRoles, ...u.roles])].sort()

function useLookups(): Lookups {
  const [lookups, setLookups] = useState<Lookups>({ departments: [], roles: [] })
  useEffect(() => {
    void Promise.all([api.list(), api.allRoles()]).then(([departments, roles]) => setLookups({ departments, roles })).catch(() => undefined)
  }, [])
  return lookups
}

export function Users() {
  const { items, error, load } = useList(api.users)
  const lookups = useLookups()
  const [panel, setPanel] = useState<Panel>(null)
  const [query, setQuery] = useState('')
  const [status, setStatus] = useState('all')
  const [department, setDepartment] = useState('all')
  const [role, setRole] = useState('all')
  const [toast, notify] = useToast()
  const canEdit = useAccess().can('users')
  const open = (next: Panel) => { if (!locked(panel)) setPanel(next) }

  const close = useCallback(() => setPanel(null), [])
  const saved = (text: string) => {
    close()
    notify(text)
    void load()
  }

  const visible = items?.filter((u) =>
    (status === 'all' || (status === 'active') === u.isActive) &&
    (department === 'all' || u.departments.some((d) => String(d.id) === department)) &&
    (role === 'all' || effectiveRoles(u).includes(role)) &&
    matches(query, fullName(u), u.email, u.mobile))
  const dirty = query !== '' || status !== 'all' || department !== 'all' || role !== 'all'

  return (
    <Page title="Users" subtitle="People, their departments and roles."
      action={canEdit ? <button onClick={() => setPanel({ mode: 'create' })}><Icon name="plus" />New user</button> : null}>
      {error && <p role="alert" className="error">{error}</p>}
      <Filters query={query} onQuery={setQuery} count={visible?.length ?? 0} total={items?.length ?? 0} dirty={dirty} placeholder="Search name, email or mobile"
        onClear={() => { setQuery(''); setStatus('all'); setDepartment('all'); setRole('all') }}>
        <Select label="Status" value={status} onChange={setStatus} options={[['active', 'Active'], ['inactive', 'Inactive'], ['all', 'All']]} />
        <Select label="Department" value={department} onChange={setDepartment} options={[['all', 'All'], ...lookups.departments.map((d): [string, string] => [String(d.id), d.name])]} />
        <Select label="Role" value={role} onChange={setRole} options={[['all', 'All'], ...lookups.roles.map((r): [string, string] => [r.name, r.name])]} />
      </Filters>
      {items === null && !error && <p className="muted">Loading...</p>}
      {visible?.length === 0 && <p className="empty">{items?.length ? 'No users match the filters.' : 'No users yet.'}</p>}
      {visible && visible.length > 0 && (
        <table>
          <thead><tr><th>User</th><th>Contact</th><th>Departments</th><th>Roles</th><th>Status</th><th><span className="sr">Actions</span></th></tr></thead>
          <tbody>
            {visible.map((u) => (
              <tr key={u.id}>
                <td data-label="User"><span className="who"><Avatar name={fullName(u)} /><button className="link" onClick={() => open({ mode: 'detail', id: u.id })}>{fullName(u)}</button></span></td>
                <td data-label="Contact"><span className="stack">{u.email && <span>{u.email}</span>}<span className="muted">{u.mobile}</span></span></td>
                <td data-label="Departments"><Chips items={u.departments.map((d) => d.name)} /></td>
                <td data-label="Roles"><Chips items={effectiveRoles(u)} /></td>
                <td data-label="Status"><StatusBadges active={u.isActive} /></td>
                <td data-label="" className="row-actions">{canEdit && <button className="secondary small" aria-label={`Edit ${fullName(u)}`} onClick={() => setPanel({ mode: 'detail', id: u.id, edit: true })}><Icon name="edit" />Edit</button>}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {panel?.mode === 'create' && <Modal title="New user" onClose={close}><CreateForm lookups={lookups} onDone={saved} onCancel={close} /></Modal>}
      {panel?.mode === 'detail' && <Modal title={panel.edit ? 'Edit user' : 'User'} readOnly={!panel.edit} onEdit={canEdit ? () => setPanel({ ...panel, edit: true }) : undefined} changeKey={panel.id} onClose={close}><Detail id={panel.id} lookups={lookups} onChanged={() => void load()} onClose={close} /></Modal>}
      {toast}
    </Page>
  )
}

const contact = (form: HTMLFormElement) => {
  const data = new FormData(form)
  return { firstName: String(data.get('firstName')).trim(), lastName: String(data.get('lastName')).trim(), email: String(data.get('email')).trim() || null, mobile: String(data.get('mobile')).trim() }
}

function ContactFields({ user }: { user?: User }) {
  return (
    <>
      <div className="row">
        <label>First name<input name="firstName" required minLength={2} maxLength={50} defaultValue={user?.firstName} autoFocus /></label>
        <label>Last name<input name="lastName" required minLength={2} maxLength={50} defaultValue={user?.lastName} /></label>
      </div>
      <label>Email<input name="email" type="email" maxLength={254} defaultValue={user?.email ?? ''} /></label>
      <label>Mobile<input name="mobile" type="tel" required minLength={5} maxLength={20} pattern="\+?[0-9]+" title="Digits, optional leading +" defaultValue={user?.mobile} /></label>
    </>
  )
}

const departmentOptions = (l: Lookups) => l.departments.filter((d) => d.isActive).map((d) => ({ key: String(d.id), label: d.name }))
const roleOptions = (l: Lookups) => l.roles.filter((r) => r.isActive).map((r) => ({ key: r.name, label: r.name, hint: r.description }))

function CreateForm({ lookups, onDone, onCancel }: { lookups: Lookups; onDone: (text: string) => void; onCancel: () => void }) {
  const { run, feedback } = useRunner()
  const [depts, setDepts] = useState<Set<string>>(new Set())
  const [roles, setRoles] = useState<Set<string>>(new Set())

  const submit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const input = contact(e.currentTarget)
    const password = String(new FormData(e.currentTarget).get('password')) || undefined
    return run(async () => {
      await api.createUser({ ...input, password, departmentIds: depts.size ? [...depts].map(Number) : undefined, roles: roles.size ? [...roles] : undefined })
      onDone('User created.')
    })
  }

  return (
    <Form onSubmit={submit}>
      <ContactFields />
      <label>Password (optional, lets them sign in)<input name="password" type="password" minLength={10} maxLength={128} autoComplete="new-password" /></label>
      <fieldset><legend>Departments (none = Unassigned)</legend>
        <ChipPicker options={departmentOptions(lookups)} selected={depts} onToggle={(k) => setDepts((s) => toggled(s, k))} />
      </fieldset>
      <fieldset><legend>Extra roles</legend>
        <ChipPicker options={roleOptions(lookups)} selected={roles} onToggle={(k) => setRoles((s) => toggled(s, k))} />
      </fieldset>
      {feedback}
      <div className="actions"><Submit>Create</Submit><button type="button" className="secondary" onClick={onCancel}>Cancel</button></div>
    </Form>
  )
}

function Detail({ id, lookups, onChanged, onClose }: { id: number; lookups: Lookups; onChanged: () => void; onClose: () => void }) {
  const [user, setUser] = useState<User | null>(null)
  const [depts, setDepts] = useState<Set<string>>(new Set())
  const [roles, setRoles] = useState<Set<string>>(new Set())
  const [password, setPassword] = useState('')
  const { run, feedback, error, setError, busy } = useRunner()

  const apply = (u: User) => {
    setUser(u)
    setDepts(new Set(u.departments.map((d) => String(d.id))))
    setRoles(new Set(u.roles))
    onChanged()
  }

  useEffect(() => {
    api.user(id).then((u) => {
      setUser(u)
      setDepts(new Set(u.departments.map((d) => String(d.id))))
      setRoles(new Set(u.roles))
    }).catch((e) => setError(message(e)))
  }, [id, setError])

  const save = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const form = e.currentTarget
    const input = contact(form)
    const isActive = (new FormData(form).get('isActive') === 'on')
    return run(async () => apply(await api.updateUser(id, { ...input, isActive })), 'Saved.')
  }

  const saveDepartments = () => run(async () => apply(await api.assignUserDepartments(id, [...depts].map(Number))), 'Departments saved.')
  const savePassword = () => run(async () => {
    await api.setUserPassword(id, password)
    setPassword('')
  }, 'Password set. Their sessions were ended.')
  const saveRoles = () => run(async () => apply(await api.assignUserRoles(id, [...roles])), 'Roles saved.')

  if (!user) return error ? feedback : <p className="muted">Loading...</p>

  return (
    <>
      <p className="meta"><Avatar name={fullName(user)} /><strong>{fullName(user)}</strong><StatusBadges active={user.isActive} /></p>
      <Form onSubmit={save} requireChange key={JSON.stringify([user.firstName, user.lastName, user.email, user.mobile, user.isActive])}>
        <ContactFields user={user} />
        <label className="switch"><input type="checkbox" role="switch" name="isActive" defaultChecked={user.isActive} /><span>Active</span></label>
        <div className="actions"><Submit>Save details</Submit></div>
      </Form>
      <fieldset><legend>Departments</legend>
        <ChipPicker options={departmentOptions(lookups)} selected={depts} onToggle={(k) => setDepts((s) => toggled(s, k))} />
        <div className="actions"><button disabled={busy || sameSet(depts, new Set(user.departments.map((d) => String(d.id))))} onClick={saveDepartments}>Save departments</button></div>
      </fieldset>
      <fieldset><legend>Roles from departments</legend><Chips items={user.departmentRoles} empty="None" /></fieldset>
      <fieldset><legend>Extra roles</legend>
        <ChipPicker options={roleOptions(lookups)} selected={roles} onToggle={(k) => setRoles((s) => toggled(s, k))} />
        <div className="actions"><button disabled={busy || sameSet(roles, new Set(user.roles))} onClick={saveRoles}>Save roles</button></div>
      </fieldset>
      <fieldset><legend>Password</legend>
        <label>New password<input type="password" value={password} minLength={10} maxLength={128} autoComplete="new-password" onChange={(e) => setPassword(e.target.value)} /></label>
        <div className="actions"><button disabled={busy || password.length < 10} onClick={savePassword}>Set password</button></div>
      </fieldset>
      {feedback}
      <div className="actions"><button className="secondary" onClick={onClose}>Close</button></div>
    </>
  )
}
