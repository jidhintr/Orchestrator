import { useAccess } from './access'
import { type FormEvent, useEffect, useState } from 'react'
import { type Col, useGrid } from './grid'
import { api, type Role, type User } from './api'
import { Avatar, Filters, Icon, matches, message, Modal, Page, Select, StatusBadges, locked, useList, useRunner, useToast, Form, Submit } from './ui'

type Panel = { mode: 'create' } | { mode: 'detail'; id: number; edit?: boolean } | null

export function Roles() {
  const { items, error, load } = useList(api.allRoles)
  const [panel, setPanel] = useState<Panel>(null)
  const [query, setQuery] = useState('')
  const [status, setStatus] = useState('all')
  const [type, setType] = useState('all')
  const [toast, notify] = useToast()
  const canEdit = useAccess().can('roles')
  const open = (next: Panel) => { if (!locked(panel)) setPanel(next) }

  const close = () => setPanel(null)
  const saved = (text: string) => {
    close()
    notify(text)
    void load()
  }

  const all: Col<Role>[] = [
    { key: 'name', label: 'Name', kind: 'text', locked: true, value: (r) => r.name, cell: (r) => <button className="link" onClick={() => open({ mode: 'detail', id: r.id })}>{r.name}</button> },
    { key: 'description', label: 'Description', kind: 'text', value: (r) => r.description, cell: (r) => <span className={r.description ? undefined : 'none'}>{r.description}</span> },
    { key: 'status', label: 'Status', kind: 'text', value: (r) => (r.isActive ? 'Enabled' : 'Disabled'), cell: (r) => <StatusBadges active={r.isActive} system={r.isSystem} activeLabel="Enabled" inactiveLabel="Disabled" /> },
  ]
  const grid = useGrid('grid:roles', all)

  const visible = items && grid.filter(items).filter((r) =>
    (status === 'all' || (status === 'enabled') === r.isActive) &&
    (type === 'all' || (type === 'system') === r.isSystem) &&
    matches(query, r.name, r.description))
  const dirty = query !== '' || status !== 'all' || type !== 'all' || grid.active

  return (
    <Page title="Roles" subtitle="What people may do. Assigned to departments and users."
      action={canEdit ? <button onClick={() => setPanel({ mode: 'create' })}><Icon name="plus" />New role</button> : null}>
      {error && <p role="alert" className="error">{error}</p>}
      <Filters query={query} onQuery={setQuery} count={visible?.length ?? 0} total={items?.length ?? 0} dirty={dirty} placeholder="Search name or description"
        onClear={() => { grid.clear(); setQuery(''); setStatus('all'); setType('all') }}>
        <Select label="Status" value={status} onChange={setStatus} options={[['enabled', 'Enabled'], ['disabled', 'Disabled'], ['all', 'All']]} />
        <Select label="Type" value={type} onChange={setType} options={[['all', 'All'], ['system', 'System'], ['custom', 'Custom']]} />
        {grid.tools}
      </Filters>
      {grid.panel}
      {items === null && !error && <p className="muted">Loading...</p>}
      {visible?.length === 0 && <p className="empty">{items?.length ? 'No roles match the filters.' : 'No roles yet.'}</p>}
      {visible && visible.length > 0 && (
        <table>
          <thead><tr>{grid.head}<th><span className="sr">Actions</span></th></tr></thead>
          <tbody>
            {visible.map((r) => (
              <tr key={r.id}>
                {grid.cells(r)}
                <td data-label="" className="row-actions">{canEdit && <button className="secondary small" aria-label={`Edit ${r.name}`} onClick={() => setPanel({ mode: 'detail', id: r.id, edit: true })}><Icon name="edit" />Edit</button>}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {panel?.mode === 'create' && <Modal title="New role" onClose={close}><CreateForm onDone={saved} onCancel={close} /></Modal>}
      {panel?.mode === 'detail' && <Modal title={panel.edit ? 'Edit role' : 'Role'} readOnly={!panel.edit} onEdit={canEdit ? () => setPanel({ ...panel, edit: true }) : undefined} changeKey={panel.id} onClose={close}><Detail id={panel.id} onChanged={saved} onSaved={() => void load()} onClose={close} /></Modal>}
      {toast}
    </Page>
  )
}

const fields = (form: HTMLFormElement) => {
  const data = new FormData(form)
  return { name: String(data.get('name')).trim(), description: String(data.get('description')).trim() }
}

function CreateForm({ onDone, onCancel }: { onDone: (text: string) => void; onCancel: () => void }) {
  const { run, feedback } = useRunner()
  const submit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const input = fields(e.currentTarget)
    return run(async () => {
      await api.createRole(input)
      onDone('Role created.')
    })
  }
  return (
    <Form onSubmit={submit}>
      <label>Name<input name="name" required minLength={2} maxLength={50} autoFocus /></label>
      <label>Description<input name="description" maxLength={500} /></label>
      {feedback}
      <div className="actions"><Submit>Create</Submit><button type="button" className="secondary" onClick={onCancel}>Cancel</button></div>
    </Form>
  )
}

function Detail({ id, onChanged, onSaved, onClose }: { onSaved: () => void; id: number; onChanged: (text: string) => void; onClose: () => void }) {
  const [role, setRole] = useState<Role | null>(null)
  const [users, setUsers] = useState<User[]>([])
  const { run, feedback, error, setError } = useRunner()

  useEffect(() => {
    Promise.all([api.role(id), api.roleUsers(id)])
      .then(([r, u]) => {
        setRole(r)
        setUsers(u)
      })
      .catch((e) => setError(message(e)))
  }, [id, setError])

  const save = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const input = fields(e.currentTarget)
    const enabled = role?.isSystem ? true : new FormData(e.currentTarget).get('isActive') === 'on'
    return run(async () => {
      const updated = await api.updateRole(id, input)
      if (enabled !== updated.isActive) await (enabled ? api.enableRole(id) : api.disableRole(id))
      setRole({ ...updated, isActive: enabled })
      onSaved()
    }, 'Saved.')
  }

  const remove = () => {
    if (role && window.confirm(`Permanently delete ${role.name}?`)) {
      return run(async () => {
        await api.deleteRole(id)
        onChanged('Role deleted.')
      })
    }
  }

  if (!role) return error ? feedback : <p className="muted">Loading...</p>

  return (
    <>
      <p className="meta"><StatusBadges active={role.isActive} system={role.isSystem} activeLabel="Enabled" inactiveLabel="Disabled" /></p>
      <Form onSubmit={save} requireChange className="ro-h" key={role.name + role.description + role.isActive}>
        <label>Name<input name="name" required minLength={2} maxLength={50} defaultValue={role.name} /></label>
        <label>Description<input name="description" maxLength={500} defaultValue={role.description} /></label>
        <label className="switch"><input type="checkbox" role="switch" name="isActive" defaultChecked={role.isActive} disabled={role.isSystem} /><span>Enabled</span></label>
        <fieldset>
          <legend>Users with this role ({users.length})</legend>
          {users.length === 0 && <p className="muted">Nobody yet.</p>}
          <ul className="people">
            {users.map((u) => <li key={u.id}><Avatar name={`${u.firstName} ${u.lastName}`} />{u.firstName} {u.lastName}</li>)}
          </ul>
        </fieldset>
        {feedback}
        <div className="actions">
          <Submit>Save</Submit>
          <button type="button" className="danger" disabled={role.isSystem} onClick={remove}>Delete</button>
          <button type="button" className="secondary" onClick={onClose}>Close</button>
        </div>
      </Form>
    </>
  )
}
