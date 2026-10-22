import { useAccess } from './access'
import { type FormEvent, useEffect, useState } from 'react'
import { type Col, useGrid } from './grid'
import { api, type Department, type Role, type User } from './api'
import { Badge, ChipPicker, Filters, Icon, matches, message, Modal, Page, Select, StatusBadges, toggled, locked, useList, useRunner, useToast, Form, sameSet, Submit } from './ui'

type Panel = { mode: 'create' } | { mode: 'detail'; id: number; edit?: boolean } | null

export function Departments() {
  const { items, error, load } = useList(api.list)
  const { items: users, load: loadUsers } = useList(api.users)
  const [panel, setPanel] = useState<Panel>(null)
  const [query, setQuery] = useState('')
  const [status, setStatus] = useState('all')
  const [type, setType] = useState('all')
  const [toast, notify] = useToast()
  const canEdit = useAccess().can('departments')
  const open = (next: Panel) => { if (!locked(panel)) setPanel(next) }

  const close = () => setPanel(null)
  const saved = (text: string) => {
    close()
    notify(text)
    void load()
    void loadUsers()
  }

  const employees = (id: number) => users?.filter((u) => u.isActive && u.departments.some((x) => x.id === id)).length ?? 0
  const all: Col<Department>[] = [
    { key: 'name', label: 'Name', kind: 'text', locked: true, value: (d) => d.name, cell: (d) => <button className="link" onClick={() => open({ mode: 'detail', id: d.id })}>{d.name}</button> },
    { key: 'code', label: 'Code', kind: 'text', value: (d) => d.code, cell: (d) => <code>{d.code}</code> },
    { key: 'description', label: 'Description', kind: 'text', value: (d) => d.description ?? '', cell: (d) => <span className={d.description ? undefined : 'none'}>{d.description}</span> },
    { key: 'status', label: 'Status', kind: 'text', value: (d) => (d.isActive ? 'Active' : 'Inactive'), cell: (d) => <><Badge kind="info">{employees(d.id)} EMP</Badge><StatusBadges active={d.isActive} system={d.isSystem} /></> },
    { key: 'employees', label: 'Employees', kind: 'number', value: (d) => employees(d.id), cell: (d) => employees(d.id) },
  ]
  const grid = useGrid('grid:departments', all)

  const visible = items && grid.filter(items).filter((d) =>
    (status === 'all' || (status === 'active') === d.isActive) &&
    (type === 'all' || (type === 'system') === d.isSystem) &&
    matches(query, d.name, d.code, d.description))
  const dirty = query !== '' || status !== 'all' || type !== 'all' || grid.active

  return (
    <Page title="Departments" subtitle="Teams and the roles their members inherit."
      action={canEdit ? <button onClick={() => setPanel({ mode: 'create' })}><Icon name="plus" />New department</button> : null}>
      {error && <p role="alert" className="error">{error}</p>}
      <Filters query={query} onQuery={setQuery} count={visible?.length ?? 0} total={items?.length ?? 0} dirty={dirty} placeholder="Search name, code or description"
        onClear={() => { grid.clear(); setQuery(''); setStatus('all'); setType('all') }}>
        <Select label="Status" value={status} onChange={setStatus} options={[['active', 'Active'], ['inactive', 'Inactive'], ['all', 'All']]} />
        <Select label="Type" value={type} onChange={setType} options={[['all', 'All'], ['system', 'System'], ['custom', 'Custom']]} />
        {grid.tools}
      </Filters>
      {grid.panel}
      {items === null && !error && <p className="muted">Loading...</p>}
      {visible?.length === 0 && <p className="empty">{items?.length ? 'No departments match the filters.' : 'No departments yet.'}</p>}
      {visible && visible.length > 0 && (
        <table>
          <thead><tr>{grid.head}<th><span className="sr">Actions</span></th></tr></thead>
          <tbody>
            {visible.map((d) => (
              <tr key={d.id}>
                {grid.cells(d)}
                <td data-label="" className="row-actions">{canEdit && <button className="secondary small" aria-label={`Edit ${d.name}`} onClick={() => setPanel({ mode: 'detail', id: d.id, edit: true })}><Icon name="edit" />Edit</button>}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {panel?.mode === 'create' && <Modal title="New department" onClose={close}><CreateForm onDone={saved} onCancel={close} /></Modal>}
      {panel?.mode === 'detail' && <Modal title={panel.edit ? 'Edit department' : 'Department'} readOnly={!panel.edit} onEdit={canEdit ? () => setPanel({ ...panel, edit: true }) : undefined} changeKey={panel.id} onClose={close}><Detail id={panel.id} onSaved={() => { void load(); void loadUsers() }} onClose={close} /></Modal>}
      {toast}
    </Page>
  )
}

function CreateForm({ onDone, onCancel }: { onDone: (text: string) => void; onCancel: () => void }) {
  const { run, feedback } = useRunner()

  function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    const data = new FormData(e.currentTarget)
    return run(async () => {
      await api.create({ name: String(data.get('name')).trim(), code: String(data.get('code')).trim(), description: String(data.get('description')).trim() || null })
      onDone('Department created.')
    })
  }

  return (
    <Form onSubmit={submit}>
      <label>Name<input name="name" required maxLength={100} autoFocus /></label>
      <label>Code<input name="code" required minLength={2} maxLength={32} pattern="[A-Z0-9_]+" title="Capital letters, digits and underscore" placeholder="e.g. PRINTING" /></label>
      <label>Description<input name="description" maxLength={500} /></label>
      {feedback}
      <div className="actions"><Submit>Create</Submit><button type="button" className="secondary" onClick={onCancel}>Cancel</button></div>
    </Form>
  )
}

function Detail({ id, onSaved, onClose }: { onSaved: () => void; id: number; onClose: () => void }) {
  const [dept, setDept] = useState<Department | null>(null)
  const [allRoles, setAllRoles] = useState<Role[]>([])
  const [assigned, setAssigned] = useState<Set<string>>(new Set())
  const [savedRoles, setSavedRoles] = useState<Set<string>>(new Set())
  const [users, setUsers] = useState<User[]>([])
  const [members, setMembers] = useState<Set<string>>(new Set())
  const [savedMembers, setSavedMembers] = useState<Set<string>>(new Set())
  const { run, feedback, error, setError } = useRunner()

  const memberKeys = (all: User[]) => new Set(all.filter((u) => u.departments.some((d) => d.id === id)).map((u) => String(u.id)))

  useEffect(() => {
    Promise.all([api.get(id), api.departmentRoles(id), api.allRoles(), api.users()])
      .then(([d, mine, all, people]) => {
        setDept(d)
        setAssigned(new Set(mine.map((r) => r.name)))
        setSavedRoles(new Set(mine.map((r) => r.name)))
        setAllRoles(all.filter((r) => r.isActive))
        setUsers(people)
        setMembers(memberKeys(people))
        setSavedMembers(memberKeys(people))
      })
      .catch((e) => setError(message(e)))
  }, [id, setError])

  const save = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const data = new FormData(e.currentTarget)
    const active = dept?.isSystem ? true : data.get('isActive') === 'on'
    return run(async () => {
      const updated = await api.update(id, { name: String(data.get('name')).trim(), description: String(data.get('description')).trim() || null })
      if (active !== updated.isActive) await (active ? api.activate(id) : api.deactivate(id))
      setDept({ ...updated, isActive: active })
      if (!sameSet(assigned, savedRoles)) {
        const next = new Set((await api.assignDepartmentRoles(id, [...assigned])).map((r) => r.name))
        setAssigned(next)
        setSavedRoles(next)
      }
      for (const u of users) {
        const key = String(u.id)
        if (members.has(key) === savedMembers.has(key)) continue
        const others = u.departments.map((d) => d.id).filter((d) => d !== id)
        await api.assignUserDepartments(u.id, members.has(key) ? [...others, id] : others)
      }
      const people = await api.users()
      setUsers(people)
      setMembers(memberKeys(people))
      setSavedMembers(memberKeys(people))
      onSaved()
    }, 'Saved.')
  }

  if (!dept) return error ? feedback : <p className="muted">Loading...</p>

  const person = (key: string) => users.find((u) => String(u.id) === key)
  const name = (key: string) => { const u = person(key); return u ? `${u.firstName} ${u.lastName}` : `#${key}` }
  const listed = [...members].sort((a, b) => name(a).localeCompare(name(b)))

  return (
    <>
      <p className="meta"><code>{dept.code}</code><StatusBadges active={dept.isActive} system={dept.isSystem} /></p>
      <Form onSubmit={save} requireChange className="ro-h" changed={!sameSet(assigned, savedRoles) || !sameSet(members, savedMembers)} key={dept.name + dept.description + dept.isActive}>
        <label>Name<input name="name" required maxLength={100} defaultValue={dept.name} /></label>
        <label>Description<input name="description" maxLength={500} defaultValue={dept.description ?? ''} /></label>
        <label className="switch"><input type="checkbox" role="switch" name="isActive" defaultChecked={dept.isActive} disabled={dept.isSystem} /><span>Active</span></label>
        <fieldset>
          <legend>Roles members inherit</legend>
          <ChipPicker options={allRoles.map((r) => ({ key: r.name, label: r.name, hint: r.description }))} selected={assigned} onToggle={(k) => setAssigned((s) => toggled(s, k))} />
        </fieldset>
        <fieldset>
          <legend>Employees ({members.size})</legend>
          <div className="picker">
            <div className="add"><ChipPicker showChosen={false} addLabel="Add employee" options={users.filter((u) => u.isActive).map((u) => ({ key: String(u.id), label: `${u.firstName} ${u.lastName}` }))} selected={members} onToggle={(k) => setMembers((s) => toggled(s, k))} /></div>
            {listed.length === 0 && <p className="muted">No employees in this department.</p>}
            <ul className="members">
              {listed.map((key) => (
                <li key={key}>
                  <span className="step-name">{name(key)}</span>
                  <button type="button" className="icon-btn" aria-label={`Remove ${name(key)}`} onClick={() => setMembers((s) => toggled(s, key))}><Icon name="close" /></button>
                </li>
              ))}
            </ul>
          </div>
        </fieldset>
        {feedback}
        <div className="actions">
          <Submit>Save</Submit>
          <button type="button" className="secondary" onClick={onClose}>Close</button>
        </div>
      </Form>
    </>
  )
}
