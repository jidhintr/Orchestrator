import { useAccess } from './access'
import { type FormEvent, useEffect, useState } from 'react'
import { api, type ApprovalChain, type Catalog, type Department } from './api'
import { Badge, Timeline, Filters, Icon, matches, message, Modal, OrderedPicker, Page, Select, locked, useList, useRunner, useToast, Form, Submit } from './ui'

type Panel = { mode: 'create' } | { mode: 'detail'; id: number; edit?: boolean } | null
type Lookups = { catalogs: Catalog[]; departments: Department[] }

function useLookups(): Lookups {
  const [lookups, setLookups] = useState<Lookups>({ catalogs: [], departments: [] })
  useEffect(() => {
    void Promise.all([api.catalogs(), api.list()]).then(([catalogs, departments]) => setLookups({ catalogs, departments })).catch(() => undefined)
  }, [])
  return lookups
}

const catalogName = (l: Lookups, id: number | null) => l.catalogs.find((c) => c.id === id)?.name ?? `Catalog #${id}`

export function Chains() {
  const { items, error, load } = useList(api.chains)
  const lookups = useLookups()
  const [panel, setPanel] = useState<Panel>(null)
  const [query, setQuery] = useState('')
  const [scope, setScope] = useState('all')
  const [department, setDepartment] = useState('all')
  const [toast, notify] = useToast()
  const canEdit = useAccess().can('chains')
  const open = (next: Panel) => { if (!locked(panel)) setPanel(next) }

  const close = () => setPanel(null)
  const saved = (text: string) => {
    close()
    notify(text)
    void load()
  }

  const visible = items?.filter((c) =>
    (scope === 'all' || (scope === 'default') === c.isDefault) &&
    (department === 'all' || c.stages.some((s) => String(s.departmentId) === department)) &&
    matches(query, c.name, c.description, c.isDefault ? 'default' : catalogName(lookups, c.catalogId)))
  const dirty = query !== '' || scope !== 'all' || department !== 'all'

  return (
    <Page title="Approval chains" subtitle="Which departments approve an order, in order."
      action={canEdit ? <button onClick={() => setPanel({ mode: 'create' })}><Icon name="plus" />New chain</button> : null}>
      {error && <p role="alert" className="error">{error}</p>}
      <Filters query={query} onQuery={setQuery} count={visible?.length ?? 0} total={items?.length ?? 0} dirty={dirty} placeholder="Search name, description or catalog"
        onClear={() => { setQuery(''); setScope('all'); setDepartment('all') }}>
        <Select label="Scope" value={scope} onChange={setScope} options={[['all', 'All'], ['default', 'Default'], ['catalog', 'Catalog']]} />
        <Select label="Department" value={department} onChange={setDepartment} options={[['all', 'All'], ...lookups.departments.map((d): [string, string] => [String(d.id), d.name])]} />
      </Filters>
      {items === null && !error && <p className="muted">Loading...</p>}
      {visible?.length === 0 && <p className="empty">{items?.length ? 'No chains match the filters.' : 'No chains yet.'}</p>}
      {visible && visible.length > 0 && (
        <table>
          <thead><tr><th>Chain</th><th>Applies to</th><th>Stages</th><th><span className="sr">Actions</span></th></tr></thead>
          <tbody>
            {visible.map((c) => (
              <tr key={c.id}>
                <td data-label="Chain"><button className="link" onClick={() => open({ mode: 'detail', id: c.id })}>{c.name}</button></td>
                <td data-label="Applies to">{c.isDefault ? <Badge>Default</Badge> : catalogName(lookups, c.catalogId)}</td>
                <td data-label="Stages"><Timeline items={c.stages.map((s) => s.departmentName)} empty="No stages" /></td>
                <td data-label="" className="row-actions">{canEdit && <button className="secondary small" aria-label={`Edit ${c.name}`} onClick={() => setPanel({ mode: 'detail', id: c.id, edit: true })}><Icon name="edit" />Edit</button>}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {panel?.mode === 'create' && <Modal title="New approval chain" onClose={close}><CreateForm lookups={lookups} onDone={saved} onCancel={close} /></Modal>}
      {panel?.mode === 'detail' && <Modal title={panel.edit ? 'Edit approval chain' : 'Approval chain'} readOnly={!panel.edit} onEdit={canEdit ? () => setPanel({ ...panel, edit: true }) : undefined} changeKey={panel.id} onClose={close}><Detail id={panel.id} lookups={lookups} onChanged={saved} onSaved={() => void load()} onClose={close} /></Modal>}
      {toast}
    </Page>
  )
}

const departmentOptions = (l: Lookups) => l.departments.filter((d) => d.isActive).map((d) => ({ id: d.id, label: d.name }))

function ChainFields({ chain }: { chain?: ApprovalChain }) {
  return (
    <>
      <label>Name<input name="name" required minLength={2} maxLength={80} defaultValue={chain?.name} autoFocus /></label>
      <label>Description<textarea name="description" maxLength={250} defaultValue={chain?.description ?? ''} /></label>
    </>
  )
}

const read = (form: HTMLFormElement) => {
  const d = new FormData(form)
  return { name: String(d.get('name')).trim(), description: String(d.get('description')).trim() || null }
}

function CreateForm({ lookups, onDone, onCancel }: { lookups: Lookups; onDone: (text: string) => void; onCancel: () => void }) {
  const { run, feedback } = useRunner()
  const [stages, setStages] = useState<number[]>([])

  const submit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const catalogId = Number(new FormData(e.currentTarget).get('catalogId'))
    const input = read(e.currentTarget)
    return run(async () => {
      await api.createChain({ ...input, catalogId, departmentIds: stages })
      onDone('Approval chain created.')
    })
  }

  return (
    <Form onSubmit={submit}>
      <ChainFields />
      <label>Catalog
        <select name="catalogId" required defaultValue="">
          <option value="" disabled>-- Select --</option>
          {lookups.catalogs.filter((c) => c.isActive).map((c) => <option key={c.id} value={c.id}>{c.name} ({c.sku})</option>)}
        </select>
      </label>
      <fieldset><legend>Approving departments in order</legend>
        <OrderedPicker label="Add a department" options={departmentOptions(lookups)} value={stages} onChange={setStages} empty="Add at least one department." />
      </fieldset>
      {feedback}
      <div className="actions"><Submit disabled={stages.length === 0}>Create</Submit><button type="button" className="secondary" onClick={onCancel}>Cancel</button></div>
    </Form>
  )
}

function Detail({ id, lookups, onChanged, onSaved, onClose }: { onSaved: () => void; id: number; lookups: Lookups; onChanged: (text: string) => void; onClose: () => void }) {
  const [chain, setChain] = useState<ApprovalChain | null>(null)
  const [stages, setStages] = useState<number[]>([])
  const { run, feedback, error, setError } = useRunner()

  useEffect(() => {
    api.chain(id).then((c) => {
      setChain(c)
      setStages(c.stages.map((s) => s.departmentId))
    }).catch((e) => setError(message(e)))
  }, [id, setError])

  const save = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const input = read(e.currentTarget)
    return run(async () => {
      const updated = await api.updateChain(id, { ...input, departmentIds: stages })
      setChain(updated)
      setStages(updated.stages.map((s) => s.departmentId))
      onSaved()
    }, 'Saved.')
  }

  const remove = () => {
    if (chain && window.confirm(`Delete ${chain.name}? Its catalog goes back to the default chain.`)) {
      return run(async () => {
        await api.deleteChain(id)
        onChanged('Approval chain deleted.')
      })
    }
  }

  if (!chain) return error ? feedback : <p className="muted">Loading...</p>

  const options = departmentOptions(lookups)
  for (const s of chain.stages) {
    if (!options.some((o) => o.id === s.departmentId)) options.push({ id: s.departmentId, label: s.departmentName })
  }

  return (
    <>
      <p className="meta">{chain.isDefault ? <Badge>Default chain</Badge> : <Badge kind="muted">{catalogName(lookups, chain.catalogId)}</Badge>}</p>
      <Form onSubmit={save} requireChange changed={JSON.stringify(stages) !== JSON.stringify(chain.stages.map((s) => s.departmentId))} key={JSON.stringify(chain)}>
        <ChainFields chain={chain} />
        <fieldset><legend>Approving departments in order</legend>
          <OrderedPicker label="Add a department" options={options} value={stages} onChange={setStages} empty="Add at least one department." />
        </fieldset>
        <div className="actions"><Submit disabled={stages.length === 0}>Save chain</Submit></div>
      </Form>
      {feedback}
      <div className="actions">
        <button className="danger" disabled={chain.isDefault} onClick={remove}>Delete</button>
        <button className="secondary" onClick={onClose}>Close</button>
      </div>
    </>
  )
}
