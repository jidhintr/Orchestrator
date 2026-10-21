import { type FormEvent, useCallback, useEffect, useState } from 'react'
import { api, type Department, type Role } from './api'

type Panel = { mode: 'create' } | { mode: 'detail'; id: number } | null

const message = (e: unknown) => (e instanceof Error ? e.message : 'Something went wrong.')

export function Departments() {
  const [items, setItems] = useState<Department[] | null>(null)
  const [error, setError] = useState('')
  const [panel, setPanel] = useState<Panel>(null)
  const [query, setQuery] = useState('')
  const [showInactive, setShowInactive] = useState(false)
  const [toast, setToast] = useState('')

  const load = useCallback(async () => {
    try {
      setItems(await api.list())
      setError('')
    } catch (e) {
      setError(message(e))
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  const close = () => setPanel(null)
  const saved = (text: string) => {
    close()
    setToast(text)
    void load()
  }

  useEffect(() => {
    if (!toast) return
    const t = setTimeout(() => setToast(''), 3000)
    return () => clearTimeout(t)
  }, [toast])

  const q = query.trim().toLowerCase()
  const visible = items?.filter((d) => (showInactive || d.isActive) && (!q || `${d.name} ${d.code} ${d.description ?? ''}`.toLowerCase().includes(q)))
  const hidden = items ? items.filter((d) => !d.isActive).length : 0

  return (
    <main>
      <header>
        <h1>Departments</h1>
        <button onClick={() => setPanel({ mode: 'create' })}>New department</button>
      </header>
      {error && <p role="alert" className="error">{error}</p>}
      <div className="toolbar">
        <input type="search" placeholder="Search name, code or description" aria-label="Search departments" value={query} onChange={(e) => setQuery(e.target.value)} />
        <label className="check"><input type="checkbox" checked={showInactive} onChange={(e) => setShowInactive(e.target.checked)} />Show inactive ({hidden})</label>
      </div>
      {items === null && !error && <p>Loading...</p>}
      {visible?.length === 0 && <p className="empty">{items?.length ? 'No departments match.' : 'No departments yet.'}</p>}
      {visible && visible.length > 0 && (
        <table>
          <thead>
            <tr><th>Name</th><th>Code</th><th>Description</th><th>Status</th></tr>
          </thead>
          <tbody>
            {visible.map((d) => (
              <tr key={d.id}>
                <td data-label="Name"><button className="link" onClick={() => setPanel({ mode: 'detail', id: d.id })}>{d.name}</button></td>
                <td data-label="Code">{d.code}</td>
                <td data-label="Description" className={d.description ? undefined : 'none'}>{d.description}</td>
                <td data-label="Status">
                  <span className={d.isActive ? 'badge ok' : 'badge off'}>{d.isActive ? 'Active' : 'Inactive'}</span>
                  {d.isSystem && <span className="badge">System</span>}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {panel && (
        <div className="overlay" onClick={close}>
          <aside role="dialog" aria-modal="true" onClick={(e) => e.stopPropagation()}>
            {panel.mode === 'create' ? <CreateForm onDone={saved} onCancel={close} /> : <Detail id={panel.id} onChanged={saved} onClose={close} />}
          </aside>
        </div>
      )}
      {toast && <p role="status" className="toast">{toast}</p>}
    </main>
  )
}

function CreateForm({ onDone, onCancel }: { onDone: (text: string) => void; onCancel: () => void }) {
  const [error, setError] = useState('')

  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    const data = new FormData(e.currentTarget)
    try {
      await api.create({ name: String(data.get('name')).trim(), code: String(data.get('code')).trim(), description: String(data.get('description')).trim() || null })
      onDone('Department created.')
    } catch (err) {
      setError(message(err))
    }
  }

  return (
    <form onSubmit={submit}>
      <h2>New department</h2>
      <label>Name<input name="name" required maxLength={100} autoFocus /></label>
      <label>Code<input name="code" required minLength={2} maxLength={32} pattern="[A-Z0-9_]+" title="Capital letters, digits and underscore" /></label>
      <label>Description<textarea name="description" maxLength={500} /></label>
      {error && <p role="alert" className="error">{error}</p>}
      <div className="actions"><button type="submit">Create</button><button type="button" className="secondary" onClick={onCancel}>Cancel</button></div>
    </form>
  )
}

function Detail({ id, onChanged, onClose }: { id: number; onChanged: (text: string) => void; onClose: () => void }) {
  const [dept, setDept] = useState<Department | null>(null)
  const [allRoles, setAllRoles] = useState<Role[]>([])
  const [assigned, setAssigned] = useState<Set<string>>(new Set())
  const [error, setError] = useState('')
  const [note, setNote] = useState('')

  useEffect(() => {
    Promise.all([api.get(id), api.roles(id), api.allRoles()])
      .then(([d, mine, all]) => {
        setDept(d)
        setAssigned(new Set(mine.map((r) => r.name)))
        setAllRoles(all.filter((r) => r.isActive))
      })
      .catch((e) => setError(message(e)))
  }, [id])

  async function run(action: () => Promise<void>, done?: string) {
    setError('')
    setNote('')
    try {
      await action()
      if (done) setNote(done)
    } catch (e) {
      setError(message(e))
    }
  }

  const save = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const data = new FormData(e.currentTarget)
    return run(async () => {
      setDept(await api.update(id, { name: String(data.get('name')).trim(), description: String(data.get('description')).trim() || null }))
    }, 'Saved.')
  }

  const saveRoles = () => run(async () => {
    setAssigned(new Set((await api.assignRoles(id, [...assigned])).map((r) => r.name)))
  }, 'Roles saved.')

  const deactivate = () => {
    if (dept && window.confirm(`Deactivate ${dept.name}?`)) {
      void run(async () => {
        await api.deactivate(id)
        onChanged('Department deactivated.')
      })
    }
  }

  const toggle = (name: string) => setAssigned((s) => {
    const next = new Set(s)
    if (!next.delete(name)) next.add(name)
    return next
  })

  if (!dept) return <>{error ? <p role="alert" className="error">{error}</p> : <p>Loading...</p>}<button className="secondary" onClick={onClose}>Close</button></>

  return (
    <>
      <form onSubmit={save} key={dept.name + dept.description}>
        <h2>{dept.name}</h2>
        <p>{dept.code} <span className={dept.isActive ? 'badge ok' : 'badge off'}>{dept.isActive ? 'Active' : 'Inactive'}</span> {dept.isSystem && <span className="badge">System</span>}</p>
        <label>Name<input name="name" required maxLength={100} defaultValue={dept.name} /></label>
        <label>Description<textarea name="description" maxLength={500} defaultValue={dept.description ?? ''} /></label>
        <div className="actions"><button type="submit">Save</button></div>
      </form>
      <fieldset>
        <legend>Roles</legend>
        {allRoles.map((r) => (
          <label key={r.id} className="check"><input type="checkbox" checked={assigned.has(r.name)} onChange={() => toggle(r.name)} />{r.name}</label>
        ))}
        <div className="actions"><button onClick={saveRoles}>Save roles</button></div>
      </fieldset>
      {error && <p role="alert" className="error">{error}</p>}
      {note && <p role="status">{note}</p>}
      <div className="actions">
        <button className="danger" disabled={dept.isSystem || !dept.isActive} onClick={deactivate}>Deactivate</button>
        <button className="secondary" onClick={onClose}>Close</button>
      </div>
    </>
  )
}
