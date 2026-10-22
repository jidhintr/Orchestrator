import { useAccess } from './access'
import { type FormEvent, useEffect, useState } from 'react'
import { api, type Catalog, type JobType, type JobTypeInput } from './api'
import { Chips, Filters, Icon, matches, message, Modal, Page, Select, StatusBadges, locked, useList, useRunner, useToast, Form, Submit } from './ui'

type Panel = { mode: 'create' } | { mode: 'detail'; id: number; edit?: boolean } | null

const percent = (v: number | null) => (v === null ? '-' : `${v}%`)

export function JobTypes() {
  const { items, error, load } = useList(api.jobTypes)
  const [panel, setPanel] = useState<Panel>(null)
  const [query, setQuery] = useState('')
  const [status, setStatus] = useState('all')
  const [toast, notify] = useToast()
  const canEdit = useAccess().can('jobTypes')
  const open = (next: Panel) => { if (!locked(panel)) setPanel(next) }

  const close = () => setPanel(null)
  const saved = (text: string) => {
    close()
    notify(text)
    void load()
  }

  const visible = items?.filter((j) => (status === 'all' || (status === 'active') === j.isActive) && matches(query, j.name, j.description))
  const dirty = query !== '' || status !== 'all'

  return (
    <Page title="Job types" subtitle="The steps a catalog item goes through, and what each costs."
      action={canEdit ? <button onClick={() => setPanel({ mode: 'create' })}><Icon name="plus" />New job type</button> : null}>
      {error && <p role="alert" className="error">{error}</p>}
      <Filters query={query} onQuery={setQuery} count={visible?.length ?? 0} total={items?.length ?? 0} dirty={dirty} placeholder="Search name or description"
        onClear={() => { setQuery(''); setStatus('all') }}>
        <Select label="Status" value={status} onChange={setStatus} options={[['active', 'Active'], ['inactive', 'Inactive'], ['all', 'All']]} />
      </Filters>
      {items === null && !error && <p className="muted">Loading...</p>}
      {visible?.length === 0 && <p className="empty">{items?.length ? 'No job types match the filters.' : 'No job types yet.'}</p>}
      {visible && visible.length > 0 && (
        <table>
          <thead><tr><th>Name</th><th>Labor cost</th><th>Commission</th><th>Profit</th><th>Status</th><th><span className="sr">Actions</span></th></tr></thead>
          <tbody>
            {visible.map((j) => (
              <tr key={j.id}>
                <td data-label="Name"><span className="stack"><button className="link" onClick={() => open({ mode: 'detail', id: j.id })}>{j.name}</button>{j.description && <span className="muted">{j.description}</span>}</span></td>
                <td data-label="Labor cost">{j.laborCost ?? '-'}</td>
                <td data-label="Commission">{percent(j.commissionPercent)}</td>
                <td data-label="Profit">{percent(j.profitPercent)}</td>
                <td data-label="Status"><StatusBadges active={j.isActive} /></td>
                <td data-label="" className="row-actions">{canEdit && <button className="secondary small" aria-label={`Edit ${j.name}`} onClick={() => setPanel({ mode: 'detail', id: j.id, edit: true })}><Icon name="edit" />Edit</button>}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {panel?.mode === 'create' && <Modal title="New job type" onClose={close}><CreateForm onDone={saved} onCancel={close} /></Modal>}
      {panel?.mode === 'detail' && <Modal title={panel.edit ? 'Edit job type' : 'Job type'} readOnly={!panel.edit} onEdit={canEdit ? () => setPanel({ ...panel, edit: true }) : undefined} changeKey={panel.id} onClose={close}><Detail id={panel.id} onChanged={saved} onSaved={() => void load()} onClose={close} /></Modal>}
      {toast}
    </Page>
  )
}

const number = (v: FormDataEntryValue | null) => (String(v).trim() === '' ? null : Number(v))

const read = (form: HTMLFormElement): JobTypeInput => {
  const d = new FormData(form)
  return {
    name: String(d.get('name')).trim(),
    description: String(d.get('description')).trim() || null,
    laborCost: number(d.get('laborCost')),
    commissionPercent: number(d.get('commissionPercent')),
    profitPercent: number(d.get('profitPercent')),
  }
}

function Fields({ job }: { job?: JobType }) {
  return (
    <>
      <label>Name<input name="name" required minLength={2} maxLength={100} defaultValue={job?.name} autoFocus /></label>
      <label>Description<textarea name="description" maxLength={500} defaultValue={job?.description ?? ''} /></label>
      <div className="row three">
        <label>Labor cost<input name="laborCost" type="number" min={0} max={1000000000} step="0.01" defaultValue={job?.laborCost ?? ''} /></label>
        <label>Commission %<input name="commissionPercent" type="number" min={0} max={100} step="0.01" defaultValue={job?.commissionPercent ?? ''} /></label>
        <label>Profit %<input name="profitPercent" type="number" min={0} max={100} step="0.01" defaultValue={job?.profitPercent ?? ''} /></label>
      </div>
    </>
  )
}

function CreateForm({ onDone, onCancel }: { onDone: (text: string) => void; onCancel: () => void }) {
  const { run, feedback } = useRunner()
  const submit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const input = read(e.currentTarget)
    return run(async () => {
      await api.createJobType(input)
      onDone('Job type created.')
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

function Detail({ id, onChanged, onSaved, onClose }: { onSaved: () => void; id: number; onChanged: (text: string) => void; onClose: () => void }) {
  const [job, setJob] = useState<JobType | null>(null)
  const [catalogs, setCatalogs] = useState<Catalog[]>([])
  const { run, feedback, error, setError } = useRunner()

  useEffect(() => {
    Promise.all([api.jobType(id), api.jobTypeCatalogs(id)]).then(([j, c]) => {
      setJob(j)
      setCatalogs(c)
    }).catch((e) => setError(message(e)))
  }, [id, setError])

  const save = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const input = read(e.currentTarget)
    return run(async () => { setJob(await api.updateJobType(id, input)); onSaved() }, 'Saved.')
  }

  const deactivate = () => {
    if (job && window.confirm(`Deactivate ${job.name}?`)) {
      return run(async () => {
        await api.deactivateJobType(id)
        onChanged('Job type deactivated.')
      })
    }
  }

  if (!job) return error ? feedback : <p className="muted">Loading...</p>

  return (
    <>
      <p className="meta"><StatusBadges active={job.isActive} /></p>
      <Form onSubmit={save} requireChange key={JSON.stringify(job)}>
        <Fields job={job} />
        <div className="actions"><Submit>Save details</Submit></div>
      </Form>
      <fieldset><legend>Used in catalogs</legend><Chips items={catalogs.map((c) => c.name)} empty="No catalog uses this job type." /></fieldset>
      {feedback}
      <div className="actions">
        <button className="danger" disabled={!job.isActive} onClick={deactivate}>Deactivate</button>
        <button className="secondary" onClick={onClose}>Close</button>
      </div>
    </>
  )
}
