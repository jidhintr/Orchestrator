import { useAccess } from './access'
import { type FormEvent, useEffect, useState } from 'react'
import { type Col, useGrid } from './grid'
import { api, type ApprovalChain, type Catalog, type CatalogInput, type JobType } from './api'
import { Badge, Timeline, Filters, Icon, matches, message, Modal, OrderedPicker, Page, Select, StatusBadges, locked, useList, useRunner, useToast, Form, Submit } from './ui'

type Panel = { mode: 'create' } | { mode: 'detail'; id: number; edit?: boolean } | null

const num = (v: FormDataEntryValue | null) => Number(v)
const money = (n: number) => n.toFixed(2)

function useJobTypes() {
  const [jobTypes, setJobTypes] = useState<JobType[]>([])
  useEffect(() => {
    api.jobTypes().then(setJobTypes).catch(() => undefined)
  }, [])
  return jobTypes
}

export function Catalogs() {
  const { items, error, load } = useList(api.catalogs)
  const jobTypes = useJobTypes()
  const [panel, setPanel] = useState<Panel>(null)
  const [query, setQuery] = useState('')
  const [status, setStatus] = useState('all')
  const [jobType, setJobType] = useState('all')
  const [toast, notify] = useToast()
  const canEdit = useAccess().can('catalogs')
  const open = (next: Panel) => { if (!locked(panel)) setPanel(next) }

  const close = () => setPanel(null)
  const saved = (text: string) => {
    close()
    notify(text)
    void load()
  }

  const all: Col<Catalog>[] = [
    { key: 'name', label: 'Catalog', kind: 'text', locked: true, value: (c) => c.name, cell: (c) => <button className="link" onClick={() => open({ mode: 'detail', id: c.id })}>{c.name}</button> },
    { key: 'sku', label: 'SKU', kind: 'text', value: (c) => c.sku, cell: (c) => <code>{c.sku}</code> },
    { key: 'description', label: 'Description', kind: 'text', value: (c) => c.description ?? '', cell: (c) => <span className="muted">{c.description}</span> },
    { key: 'basePrice', label: 'Base price', kind: 'number', value: (c) => c.basePrice, cell: (c) => money(c.basePrice) },
    { key: 'discount', label: 'Discount %', kind: 'number', value: (c) => c.catalogDiscountPercent, cell: (c) => `${c.catalogDiscountPercent}%` },
    { key: 'labor', label: 'Labor cost', kind: 'number', value: (c) => c.laborCost, cell: (c) => money(c.laborCost) },
    { key: 'commission', label: 'Commission %', kind: 'number', value: (c) => c.commissionPercent, cell: (c) => `${c.commissionPercent}%` },
    { key: 'profit', label: 'Profit %', kind: 'number', value: (c) => c.profitPercent, cell: (c) => `${c.profitPercent}%` },
    { key: 'steps', label: 'Steps', kind: 'text', value: (c) => c.steps.map((x) => x.jobTypeName).join(', '), cell: (c) => <Timeline items={c.steps.map((x) => x.jobTypeName)} /> },
    { key: 'status', label: 'Status', kind: 'text', value: (c) => (c.isActive ? 'Active' : 'Inactive'), cell: (c) => <StatusBadges active={c.isActive} /> },
  ]
  const grid = useGrid('grid:catalogs', all, ['description', 'discount', 'labor', 'commission', 'profit'])

  const visible = items && grid.filter(items).filter((c) =>
    (status === 'all' || (status === 'active') === c.isActive) &&
    (jobType === 'all' || c.steps.some((s) => String(s.jobTypeId) === jobType)) &&
    matches(query, c.sku, c.name, c.description))
  const dirty = query !== '' || status !== 'all' || jobType !== 'all' || grid.active

  return (
    <Page title="Catalogs" subtitle="Products, pricing and the job steps each one goes through."
      action={canEdit ? <button onClick={() => setPanel({ mode: 'create' })}><Icon name="plus" />New catalog</button> : null}>
      {error && <p role="alert" className="error">{error}</p>}
      <Filters query={query} onQuery={setQuery} count={visible?.length ?? 0} total={items?.length ?? 0} dirty={dirty} placeholder="Search SKU, name or description"
        onClear={() => { setQuery(''); setStatus('all'); setJobType('all'); grid.clear() }}>
        <Select label="Status" value={status} onChange={setStatus} options={[['active', 'Active'], ['inactive', 'Inactive'], ['all', 'All']]} />
        <Select label="Job type" value={jobType} onChange={setJobType} options={[['all', 'All'], ...jobTypes.map((j): [string, string] => [String(j.id), j.name])]} />
        {grid.tools}
      </Filters>
      {grid.panel}
      {items === null && !error && <p className="muted">Loading...</p>}
      {visible?.length === 0 && <p className="empty">{items?.length ? 'No catalogs match the filters.' : 'No catalogs yet.'}</p>}
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
      {panel?.mode === 'create' && <Modal title="New catalog" onClose={close}><CreateForm jobTypes={jobTypes} onDone={saved} onCancel={close} /></Modal>}
      {panel?.mode === 'detail' && <Modal title={panel.edit ? 'Edit catalog' : 'Catalog'} readOnly={!panel.edit} onEdit={canEdit ? () => setPanel({ ...panel, edit: true }) : undefined} changeKey={panel.id} onClose={close}><Detail id={panel.id} jobTypes={jobTypes} onChanged={saved} onSaved={() => void load()} onClose={close} /></Modal>}
      {toast}
    </Page>
  )
}

function CatalogForm({ catalog, jobTypes, submitLabel, onSubmit, children, extra }: { catalog?: Catalog; jobTypes: JobType[]; submitLabel: string; onSubmit: (input: CatalogInput, active: boolean) => unknown; children?: React.ReactNode; extra?: React.ReactNode }) {
  const [steps, setSteps] = useState<number[]>(catalog?.steps.map((s) => s.jobTypeId) ?? [])

  const submit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const d = new FormData(e.currentTarget)
    return onSubmit({
      sku: String(d.get('sku')).trim(),
      name: String(d.get('name')).trim(),
      description: String(d.get('description')).trim() || null,
      basePrice: num(d.get('basePrice')),
      catalogDiscountPercent: num(d.get('catalogDiscountPercent')),
      laborCost: num(d.get('laborCost')),
      commissionPercent: num(d.get('commissionPercent')),
      profitPercent: num(d.get('profitPercent')),
      jobTypeIds: steps,
    }, d.get('isActive') === 'on')
  }

  const field = (name: keyof Catalog, text: string, max: number) => (
    <label>{text}<input name={name} type="number" min={0} max={max} step="0.01" required defaultValue={catalog ? String(catalog[name]) : '0'} /></label>
  )

  return (
    <Form onSubmit={submit} className="ro-h" requireChange={!!catalog} changed={!!catalog && JSON.stringify(steps) !== JSON.stringify(catalog.steps.map((s) => s.jobTypeId))}>
      <div className="row">
        <label>SKU<input name="sku" required minLength={3} maxLength={50} defaultValue={catalog?.sku} autoFocus /></label>
        <label>Name<input name="name" required minLength={3} maxLength={150} defaultValue={catalog?.name} /></label>
      </div>
      <label>Description<input name="description" maxLength={1000} defaultValue={catalog?.description ?? ''} /></label>
      <div className="row">{field('basePrice', 'Base price', 1000000000)}{field('catalogDiscountPercent', 'Discount %', 100)}</div>
      <div className="row">{field('laborCost', 'Labor cost', 1000000000)}{field('commissionPercent', 'Commission %', 100)}</div>
      <div className="row">{field('profitPercent', 'Profit %', 100)}</div>
      {catalog && <label className="switch"><input type="checkbox" role="switch" name="isActive" defaultChecked={catalog.isActive} /><span>Active</span></label>}
      <fieldset>
        <legend>Job steps in order</legend>
        <OrderedPicker label="Add job type" options={jobTypes.filter((j) => j.isActive).map((j) => ({ id: j.id, label: j.name }))} value={steps} onChange={setSteps} empty="Add at least one job type." />
      </fieldset>
      {children}
      <div className="actions"><Submit disabled={steps.length === 0}>{submitLabel}</Submit>{extra}</div>
    </Form>
  )
}

function CreateForm({ jobTypes, onDone, onCancel }: { jobTypes: JobType[]; onDone: (text: string) => void; onCancel: () => void }) {
  const { run, feedback } = useRunner()
  return (
    <CatalogForm jobTypes={jobTypes} submitLabel="Create" onSubmit={(input) => run(async () => {
      await api.createCatalog(input)
      onDone('Catalog created.')
    })} extra={<button type="button" className="secondary" onClick={onCancel}>Cancel</button>}>
      {feedback}
    </CatalogForm>
  )
}

function Detail({ id, jobTypes, onChanged, onSaved, onClose }: { onSaved: () => void; id: number; jobTypes: JobType[]; onChanged: (text: string) => void; onClose: () => void }) {
  const [catalog, setCatalog] = useState<Catalog | null>(null)
  const [chain, setChain] = useState<ApprovalChain | null>(null)
  const [cloning, setCloning] = useState(false)
  const { run, feedback, error, setError } = useRunner()

  useEffect(() => {
    Promise.all([api.catalog(id), api.chainForCatalog(id)])
      .then(([c, ch]) => {
        setCatalog(c)
        setChain(ch)
      })
      .catch((e) => setError(message(e)))
  }, [id, setError])

  const clone = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const d = new FormData(e.currentTarget)
    return run(async () => {
      await api.cloneCatalog(id, String(d.get('sku')).trim(), String(d.get('name')).trim())
      onChanged('Catalog cloned.')
    })
  }

  if (!catalog) return error ? feedback : <p className="muted">Loading...</p>

  return (
    <>
      <p className="meta"><code>{catalog.sku}</code><StatusBadges active={catalog.isActive} />{chain && <Badge kind="muted">{chain.isDefault ? 'Default approval chain' : `Chain: ${chain.name}`}</Badge>}</p>
      <CatalogForm key={JSON.stringify(catalog)} catalog={catalog} jobTypes={jobTypes} submitLabel="Save"
        onSubmit={(input, active) => run(async () => {
          const same = input.sku === catalog.sku && input.name === catalog.name && input.description === catalog.description && input.basePrice === catalog.basePrice
            && input.catalogDiscountPercent === catalog.catalogDiscountPercent && input.laborCost === catalog.laborCost && input.commissionPercent === catalog.commissionPercent
            && input.profitPercent === catalog.profitPercent && JSON.stringify(input.jobTypeIds) === JSON.stringify(catalog.steps.map((s) => s.jobTypeId))
          let next = same ? catalog : await api.updateCatalog(id, input)
          if (active !== next.isActive) {
            await (active ? api.activateCatalog(id) : api.deactivateCatalog(id))
            next = { ...next, isActive: active }
          }
          setCatalog(next)
          onSaved()
        }, 'Saved.')}
        extra={<>
          <button type="button" className="secondary" onClick={() => setCloning(true)}>Clone</button>
          <button type="button" className="secondary" onClick={onClose}>Close</button>
        </>}>
        {chain && <fieldset><legend>Approval stages</legend><Timeline items={chain.stages.map((s) => s.departmentName)} empty="No approval stages" /></fieldset>}
        {feedback}
      </CatalogForm>
      {cloning && (
        <Form onSubmit={clone}>
          <div className="row">
            <label>New SKU<input name="sku" required minLength={3} maxLength={50} autoFocus /></label>
            <label>New name<input name="name" required minLength={3} maxLength={150} /></label>
          </div>
          <div className="actions"><Submit>Create clone</Submit><button type="button" className="secondary" onClick={() => setCloning(false)}>Cancel</button></div>
        </Form>
      )}
    </>
  )
}
