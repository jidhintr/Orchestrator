import { type FormEvent, useEffect, useState } from 'react'
import { api, type Department, type Role } from './api'
import { ChipPicker, Filters, Icon, matches, message, Modal, Page, Select, StatusBadges, toggled, locked, useList, useRunner, useToast, Form, sameSet, Submit } from './ui'

type Panel = { mode: 'create' } | { mode: 'detail'; id: number; edit?: boolean } | null

export function Departments() {
  const { items, error, load } = useList(api.list)
  const [panel, setPanel] = useState<Panel>(null)
  const [query, setQuery] = useState('')
  const [status, setStatus] = useState('all')
  const [type, setType] = useState('all')
  const [toast, notify] = useToast()
  const open = (next: Panel) => { if (!locked(panel)) setPanel(next) }

  const close = () => setPanel(null)
  const saved = (text: string) => {
    close()
    notify(text)
    void load()
  }

  const visible = items?.filter((d) =>
    (status === 'all' || (status === 'active') === d.isActive) &&
    (type === 'all' || (type === 'system') === d.isSystem) &&
    matches(query, d.name, d.code, d.description))
  const dirty = query !== '' || status !== 'all' || type !== 'all'

  return (
    <Page title="Departments" subtitle="Teams and the roles their members inherit."
      action={<button onClick={() => setPanel({ mode: 'create' })}><Icon name="plus" />New department</button>}>
      {error && <p role="alert" className="error">{error}</p>}
      <Filters query={query} onQuery={setQuery} count={visible?.length ?? 0} total={items?.length ?? 0} dirty={dirty} placeholder="Search name, code or description"
        onClear={() => { setQuery(''); setStatus('all'); setType('all') }}>
        <Select label="Status" value={status} onChange={setStatus} options={[['active', 'Active'], ['inactive', 'Inactive'], ['all', 'All']]} />
        <Select label="Type" value={type} onChange={setType} options={[['all', 'All'], ['system', 'System'], ['custom', 'Custom']]} />
      </Filters>
      {items === null && !error && <p className="muted">Loading...</p>}
      {visible?.length === 0 && <p className="empty">{items?.length ? 'No departments match the filters.' : 'No departments yet.'}</p>}
      {visible && visible.length > 0 && (
        <table>
          <thead><tr><th>Name</th><th>Code</th><th>Description</th><th>Status</th><th><span className="sr">Actions</span></th></tr></thead>
          <tbody>
            {visible.map((d) => (
              <tr key={d.id}>
                <td data-label="Name"><button className="link" onClick={() => open({ mode: 'detail', id: d.id })}>{d.name}</button></td>
                <td data-label="Code"><code>{d.code}</code></td>
                <td data-label="Description" className={d.description ? undefined : 'none'}>{d.description}</td>
                <td data-label="Status"><StatusBadges active={d.isActive} system={d.isSystem} /></td>
                <td data-label="" className="row-actions"><button className="secondary small" aria-label={`Edit ${d.name}`} onClick={() => setPanel({ mode: 'detail', id: d.id, edit: true })}><Icon name="edit" />Edit</button></td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {panel?.mode === 'create' && <Modal title="New department" onClose={close}><CreateForm onDone={saved} onCancel={close} /></Modal>}
      {panel?.mode === 'detail' && <Modal title={panel.edit ? 'Edit department' : 'Department'} readOnly={!panel.edit} onEdit={() => setPanel({ ...panel, edit: true })} changeKey={panel.id} onClose={close}><Detail id={panel.id} onChanged={saved} onSaved={() => void load()} onClose={close} /></Modal>}
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
      <label>Description<textarea name="description" maxLength={500} /></label>
      {feedback}
      <div className="actions"><Submit>Create</Submit><button type="button" className="secondary" onClick={onCancel}>Cancel</button></div>
    </Form>
  )
}

function Detail({ id, onChanged, onSaved, onClose }: { onSaved: () => void; id: number; onChanged: (text: string) => void; onClose: () => void }) {
  const [dept, setDept] = useState<Department | null>(null)
  const [allRoles, setAllRoles] = useState<Role[]>([])
  const [assigned, setAssigned] = useState<Set<string>>(new Set())
  const [savedRoles, setSavedRoles] = useState<Set<string>>(new Set())
  const { run, feedback, error, setError, busy } = useRunner()

  useEffect(() => {
    Promise.all([api.get(id), api.departmentRoles(id), api.allRoles()])
      .then(([d, mine, all]) => {
        setDept(d)
        setAssigned(new Set(mine.map((r) => r.name)))
        setSavedRoles(new Set(mine.map((r) => r.name)))
        setAllRoles(all.filter((r) => r.isActive))
      })
      .catch((e) => setError(message(e)))
  }, [id, setError])

  const save = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const data = new FormData(e.currentTarget)
    return run(async () => {
      setDept(await api.update(id, { name: String(data.get('name')).trim(), description: String(data.get('description')).trim() || null }))
      onSaved()
    }, 'Saved.')
  }

  const saveRoles = () => run(async () => {
    const next = new Set((await api.assignDepartmentRoles(id, [...assigned])).map((r) => r.name))
    setAssigned(next)
    setSavedRoles(next)
  }, 'Roles saved.')

  const deactivate = () => {
    if (dept && window.confirm(`Deactivate ${dept.name}?`)) {
      return run(async () => {
        await api.deactivate(id)
        onChanged('Department deactivated.')
      })
    }
  }

  if (!dept) return error ? feedback : <p className="muted">Loading...</p>

  return (
    <>
      <p className="meta"><code>{dept.code}</code><StatusBadges active={dept.isActive} system={dept.isSystem} /></p>
      <Form onSubmit={save} requireChange key={dept.name + dept.description}>
        <label>Name<input name="name" required maxLength={100} defaultValue={dept.name} /></label>
        <label>Description<textarea name="description" maxLength={500} defaultValue={dept.description ?? ''} /></label>
        <div className="actions"><Submit>Save details</Submit></div>
      </Form>
      <fieldset>
        <legend>Roles members inherit</legend>
        <ChipPicker options={allRoles.map((r) => ({ key: r.name, label: r.name, hint: r.description }))} selected={assigned} onToggle={(k) => setAssigned((s) => toggled(s, k))} />
        <div className="actions"><button disabled={busy || sameSet(assigned, savedRoles)} onClick={saveRoles}>Save roles</button></div>
      </fieldset>
      {feedback}
      <div className="actions">
        <button className="danger" disabled={dept.isSystem || !dept.isActive} onClick={deactivate}>Deactivate</button>
        <button className="secondary" onClick={onClose}>Close</button>
      </div>
    </>
  )
}
