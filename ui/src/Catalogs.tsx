import { type FormEvent, useEffect, useState } from 'react'
import { api, type ApprovalChain, type Catalog, type CatalogInput, type JobType } from './api'
import { Badge, Chips, Filters, Icon, matches, message, Modal, OrderedPicker, Page, Select, StatusBadges, useList, useRunner, useToast } from './ui'

type Panel = { mode: 'create' } | { mode: 'detail'; id: number } | null

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
  const [status, setStatus] = useState('active')
  const [jobType, setJobType] = useState('all')
  const [toast, notify] = useToast()

  const close = () => setPanel(null)
  const saved = (text: string) => {
    close()
    notify(text)
    void load()
  }

  const visible = items?.filter((c) =>
    (status === 'all' || (status === 'active') === c.isActive) &&
    (jobType === 'all' || c.steps.some((s) => String(s.jobTypeId) === jobType)) &&
    matches(query, c.sku, c.name, c.description))
  const dirty = query !== '' || status !== 'active' || jobType !== 'all'

  return (
    <Page title="Catalogs" subtitle="Products, pricing and the job steps each one goes through."
      action={<button onClick={() => setPanel({ mode: 'create' })}><Icon name="plus" />New catalog</button>}>
      {error && <p role="alert" className="error">{error}</p>}
      <Filters query={query} onQuery={setQuery} count={visible?.length ?? 0} total={items?.length ?? 0} dirty={dirty} placeholder="Search SKU, name or description"
        onClear={() => { setQuery(''); setStatus('active'); setJobType('all') }}>
        <Select label="Status" value={status} onChange={setStatus} options={[['active', 'Active'], ['inactive', 'Inactive'], ['all', 'All']]} />
        <Select label="Job type" value={jobType} onChange={setJobType} options={[['all', 'All'], ...jobTypes.map((j): [string, string] => [String(j.id), j.name])]} />
      </Filters>
      {items === null && !error && <p className="muted">Loading...</p>}
      {visible?.length === 0 && <p className="empty">{items?.length ? 'No catalogs match the filters.' : 'No catalogs yet.'}</p>}
      {visible && visible.length > 0 && (
        <table>
          <thead><tr><th>Catalog</th><th>SKU</th><th>Base price</th><th>Steps</th><th>Status</th></tr></thead>
          <tbody>
            {visible.map((c) => (
              <tr key={c.id}>
                <td data-label="Catalog"><button className="link" onClick={() => setPanel({ mode: 'detail', id: c.id })}>{c.name}</button></td>
                <td data-label="SKU"><code>{c.sku}</code></td>
                <td data-label="Base price">{money(c.basePrice)}</td>
                <td data-label="Steps"><Chips items={c.steps.map((s) => `${s.sequence}. ${s.jobTypeName}`)} /></td>
                <td data-label="Status"><StatusBadges active={c.isActive} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {panel?.mode === 'create' && <Modal title="New catalog" onClose={close}><CreateForm jobTypes={jobTypes} onDone={saved} onCancel={close} /></Modal>}
      {panel?.mode === 'detail' && <Modal title="Catalog" onClose={close}><Detail id={panel.id} jobTypes={jobTypes} onChanged={saved} onClose={close} /></Modal>}
      {toast}
    </Page>
  )
}

function CatalogForm({ catalog, jobTypes, submitLabel, onSubmit, children }: { catalog?: Catalog; jobTypes: JobType[]; submitLabel: string; onSubmit: (input: CatalogInput) => void; children?: React.ReactNode }) {
  const [steps, setSteps] = useState<number[]>(catalog?.steps.map((s) => s.jobTypeId) ?? [])

  const submit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const d = new FormData(e.currentTarget)
    onSubmit({
      sku: String(d.get('sku')).trim(),
      name: String(d.get('name')).trim(),
      description: String(d.get('description')).trim() || null,
      basePrice: num(d.get('basePrice')),
      catalogDiscountPercent: num(d.get('catalogDiscountPercent')),
      laborCost: num(d.get('laborCost')),
      commissionPercent: num(d.get('commissionPercent')),
      profitPercent: num(d.get('profitPercent')),
      jobTypeIds: steps,
    })
  }

  const field = (name: keyof Catalog, text: string, max: number) => (
    <label>{text}<input name={name} type="number" min={0} max={max} step="0.01" required defaultValue={catalog ? String(catalog[name]) : '0'} /></label>
  )

  return (
    <form onSubmit={submit}>
      <div className="row">
        <label>SKU<input name="sku" required minLength={3} maxLength={50} defaultValue={catalog?.sku} autoFocus /></label>
        <label>Name<input name="name" required minLength={3} maxLength={150} defaultValue={catalog?.name} /></label>
      </div>
      <label>Description<textarea name="description" maxLength={1000} defaultValue={catalog?.description ?? ''} /></label>
      <div className="row">{field('basePrice', 'Base price', 1000000000)}{field('catalogDiscountPercent', 'Discount %', 100)}</div>
      <div className="row">{field('laborCost', 'Labor cost', 1000000000)}{field('commissionPercent', 'Commission %', 100)}</div>
      <div className="row">{field('profitPercent', 'Profit %', 100)}</div>
      <fieldset>
        <legend>Job steps in order</legend>
        <OrderedPicker label="Add a job type" options={jobTypes.filter((j) => j.isActive).map((j) => ({ id: j.id, label: j.name }))} value={steps} onChange={setSteps} empty="Add at least one job type." />
      </fieldset>
      {children}
      <div className="actions"><button type="submit" disabled={steps.length === 0}>{submitLabel}</button></div>
    </form>
  )
}

function CreateForm({ jobTypes, onDone, onCancel }: { jobTypes: JobType[]; onDone: (text: string) => void; onCancel: () => void }) {
  const { run, feedback } = useRunner()
  return (
    <CatalogForm jobTypes={jobTypes} submitLabel="Create" onSubmit={(input) => void run(async () => {
      await api.createCatalog(input)
      onDone('Catalog created.')
    })}>
      {feedback}
      <div className="actions"><button type="button" className="secondary" onClick={onCancel}>Cancel</button></div>
    </CatalogForm>
  )
}

function Detail({ id, jobTypes, onChanged, onClose }: { id: number; jobTypes: JobType[]; onChanged: (text: string) => void; onClose: () => void }) {
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

  const deactivate = () => {
    if (catalog && window.confirm(`Deactivate ${catalog.name}?`)) {
      void run(async () => {
        await api.deactivateCatalog(id)
        onChanged('Catalog deactivated.')
      })
    }
  }

  const clone = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const d = new FormData(e.currentTarget)
    void run(async () => {
      await api.cloneCatalog(id, String(d.get('sku')).trim(), String(d.get('name')).trim())
      onChanged('Catalog cloned.')
    })
  }

  if (!catalog) return error ? feedback : <p className="muted">Loading...</p>

  return (
    <>
      <p className="meta"><code>{catalog.sku}</code><StatusBadges active={catalog.isActive} />{chain && <Badge kind="muted">{chain.isDefault ? 'Default approval chain' : `Chain: ${chain.name}`}</Badge>}</p>
      <CatalogForm key={JSON.stringify(catalog)} catalog={catalog} jobTypes={jobTypes} submitLabel="Save catalog"
        onSubmit={(input) => void run(async () => setCatalog(await api.updateCatalog(id, input)), 'Saved.')} />
      {chain && <fieldset><legend>Approval stages</legend><Chips items={chain.stages.map((s) => `${s.sequence}. ${s.departmentName}`)} empty="No approval stages" /></fieldset>}
      {cloning && (
        <form onSubmit={clone}>
          <div className="row">
            <label>New SKU<input name="sku" required minLength={3} maxLength={50} autoFocus /></label>
            <label>New name<input name="name" required minLength={3} maxLength={150} /></label>
          </div>
          <div className="actions"><button type="submit">Create clone</button><button type="button" className="secondary" onClick={() => setCloning(false)}>Cancel</button></div>
        </form>
      )}
      {feedback}
      <div className="actions">
        <button className="secondary" onClick={() => setCloning(true)}>Clone</button>
        <button className="danger" disabled={!catalog.isActive} onClick={deactivate}>Deactivate</button>
        <button className="secondary" onClick={onClose}>Close</button>
      </div>
    </>
  )
}
