import { useAccess } from './access'
import { type FormEvent, useEffect, useState } from 'react'
import { type Col, useGrid } from './grid'
import { api, type Category } from './api'
import { Filters, Icon, matches, message, Modal, Page, Select, StatusBadges, locked, useList, useRunner, useToast, Form, Submit } from './ui'

type Panel = { mode: 'create' } | { mode: 'detail'; id: number; edit?: boolean } | null

export function Categories() {
  const { items, error, load } = useList(api.categories)
  const [panel, setPanel] = useState<Panel>(null)
  const [query, setQuery] = useState('')
  const [status, setStatus] = useState('all')
  const [toast, notify] = useToast()
  const canEdit = useAccess().can('categories')
  const open = (next: Panel) => { if (!locked(panel)) setPanel(next) }

  const close = () => setPanel(null)
  const saved = (text: string) => {
    close()
    notify(text)
    void load()
  }

  const all: Col<Category>[] = [
    { key: 'name', label: 'Name', kind: 'text', locked: true, value: (c) => c.name, cell: (c) => <button className="link" onClick={() => open({ mode: 'detail', id: c.id })}>{c.name}</button> },
    { key: 'description', label: 'Description', kind: 'text', value: (c) => c.description ?? '', cell: (c) => <span className="muted">{c.description}</span> },
    { key: 'status', label: 'Status', kind: 'text', value: (c) => (c.isActive ? 'Active' : 'Inactive'), cell: (c) => <StatusBadges active={c.isActive} /> },
  ]
  const grid = useGrid('grid:categories', all)

  const visible = items && grid.filter(items).filter((c) => (status === 'all' || (status === 'active') === c.isActive) && matches(query, c.name, c.description))
  const dirty = query !== '' || status !== 'all' || grid.active

  return (
    <Page title="Categories" subtitle="Groups of catalogs, such as Bags, Boxes or Seasonal."
      action={canEdit ? <button onClick={() => setPanel({ mode: 'create' })}><Icon name="plus" />New category</button> : null}>
      {error && <p role="alert" className="error">{error}</p>}
      <Filters query={query} onQuery={setQuery} count={visible?.length ?? 0} total={items?.length ?? 0} dirty={dirty} placeholder="Search name or description"
        onClear={() => { grid.clear(); setQuery(''); setStatus('all') }}>
        <Select label="Status" value={status} onChange={setStatus} options={[['active', 'Active'], ['inactive', 'Inactive'], ['all', 'All']]} />
        {grid.tools}
      </Filters>
      {grid.panel}
      {items === null && !error && <p className="muted">Loading...</p>}
      {visible?.length === 0 && <p className="empty">{items?.length ? 'No categories match the filters.' : 'No categories yet.'}</p>}
      {visible && visible.length > 0 && (
        <table>
          <thead><tr>{grid.head}<th><span className="sr">Actions</span></th></tr></thead>
          <tbody>
            {visible.map((c) => (
              <tr key={c.id}>
                {grid.cells(c)}
                <td data-label="" className="row-actions">{canEdit && <button className="secondary small" aria-label={`Edit ${c.name}`} onClick={() => setPanel({ mode: 'detail', id: c.id, edit: true })}><Icon name="edit" />Edit</button>}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {panel?.mode === 'create' && <Modal title="New category" onClose={close}><CreateForm onDone={saved} onCancel={close} /></Modal>}
      {panel?.mode === 'detail' && <Modal title={panel.edit ? 'Edit category' : 'Category'} readOnly={!panel.edit} onEdit={canEdit ? () => setPanel({ ...panel, edit: true }) : undefined} changeKey={panel.id} onClose={close}><Detail id={panel.id} onSaved={() => void load()} onClose={close} /></Modal>}
      {toast}
    </Page>
  )
}

const read = (form: HTMLFormElement) => {
  const d = new FormData(form)
  return { name: String(d.get('name')).trim(), description: String(d.get('description')).trim() || null }
}

function Fields({ category }: { category?: Category }) {
  return (
    <>
      <label>Name<input name="name" required minLength={2} maxLength={100} defaultValue={category?.name} autoFocus /></label>
      <label>Description<input name="description" maxLength={500} defaultValue={category?.description ?? ''} /></label>
    </>
  )
}

function CreateForm({ onDone, onCancel }: { onDone: (text: string) => void; onCancel: () => void }) {
  const { run, feedback } = useRunner()
  const submit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const input = read(e.currentTarget)
    return run(async () => {
      await api.createCategory(input)
      onDone('Category created.')
    })
  }
  return (
    <Form onSubmit={submit}>
      <Fields />
      {feedback}
      <div className="actions"><Submit>Create</Submit><button type="button" className="secondary" onClick={onCancel}>Cancel</button></div>
    </Form>
  )
}

function Detail({ id, onSaved, onClose }: { onSaved: () => void; id: number; onClose: () => void }) {
  const [category, setCategory] = useState<Category | null>(null)
  const { run, feedback, error, setError } = useRunner()

  useEffect(() => {
    api.category(id).then(setCategory).catch((e) => setError(message(e)))
  }, [id, setError])

  const save = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const input = read(e.currentTarget)
    const active = new FormData(e.currentTarget).get('isActive') === 'on'
    return run(async () => {
      const updated = await api.updateCategory(id, input)
      if (active !== updated.isActive) await (active ? api.activateCategory(id) : api.deactivateCategory(id))
      setCategory({ ...updated, isActive: active })
      onSaved()
    }, 'Saved.')
  }

  if (!category) return error ? feedback : <p className="muted">Loading...</p>

  return (
    <>
      <p className="meta"><StatusBadges active={category.isActive} /></p>
      <Form onSubmit={save} requireChange className="ro-h" key={JSON.stringify(category)}>
        <Fields category={category} />
        <label className="switch"><input type="checkbox" role="switch" name="isActive" defaultChecked={category.isActive} /><span>Active</span></label>
        {feedback}
        <div className="actions">
          <Submit>Save</Submit>
          <button type="button" className="secondary" onClick={onClose}>Close</button>
        </div>
      </Form>
    </>
  )
}
