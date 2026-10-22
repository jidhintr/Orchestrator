import { useAccess } from './access'
import { type FormEvent, useEffect, useState } from 'react'
import { type Col, useGrid } from './grid'
import { api, type Client, type ClientInput, type ClientStatus, type ClientTier } from './api'
import { Badge, Filters, Icon, matches, message, Modal, Page, Select, locked, useList, useRunner, useToast, Form, Submit } from './ui'

type Panel = { mode: 'create' } | { mode: 'detail'; id: number; edit?: boolean } | null

const statuses: ClientStatus[] = ['Active', 'PaymentPending', 'Suspended', 'Inactive']
const tiers: ClientTier[] = ['Normal', 'Gold', 'Platinum']
const statusKind = { Active: 'ok', PaymentPending: 'warn', Suspended: 'off', Inactive: 'muted' } as const
const label = (s: string) => s.replace(/([a-z])([A-Z])/g, '$1 $2')

export const StatusBadge = ({ status }: { status: ClientStatus }) => <Badge kind={statusKind[status]}>{label(status)}</Badge>

export function Clients() {
  const { items, error, load } = useList(api.clients)
  const [panel, setPanel] = useState<Panel>(null)
  const [query, setQuery] = useState('')
  const [status, setStatus] = useState('all')
  const [tier, setTier] = useState('all')
  const [toast, notify] = useToast()
  const canEdit = useAccess().can('clients')
  const open = (next: Panel) => { if (!locked(panel)) setPanel(next) }

  const close = () => setPanel(null)
  const saved = (text: string) => {
    close()
    notify(text)
    void load()
  }

  const all: Col<Client>[] = [
    { key: 'company', label: 'Company', kind: 'text', locked: true, value: (c) => c.companyName, cell: (c) => <button className="link" onClick={() => open({ mode: 'detail', id: c.id })}>{c.companyName}</button> },
    { key: 'contactName', label: 'Contact', kind: 'text', value: (c) => c.contactName, cell: (c) => <span className="stack"><span>{c.contactName}</span><span className="muted">{c.email}</span></span> },
    { key: 'email', label: 'Email', kind: 'text', value: (c) => c.email, cell: (c) => c.email },
    { key: 'phone', label: 'Phone', kind: 'text', value: (c) => c.phone, cell: (c) => c.phone },
    { key: 'address', label: 'Address', kind: 'text', value: (c) => c.shippingAddress, cell: (c) => <span className="muted">{c.shippingAddress}</span> },
    { key: 'tier', label: 'Tier', kind: 'text', value: (c) => c.tier, cell: (c) => <Badge kind={c.tier === 'Normal' ? 'muted' : 'info'}>{c.tier}</Badge> },
    { key: 'discount', label: 'Discount', kind: 'number', value: (c) => c.tierDiscountPercent, cell: (c) => `${c.tierDiscountPercent}%` },
    { key: 'status', label: 'Status', kind: 'text', value: (c) => label(c.status), cell: (c) => <StatusBadge status={c.status} /> },
  ]
  const grid = useGrid('grid:clients', all, ['email','phone'])

  const visible = items && grid.filter(items).filter((c) =>
    (status === 'all' || c.status === status) &&
    (tier === 'all' || c.tier === tier) &&
    matches(query, c.companyName, c.contactName, c.email, c.phone))
  const dirty = query !== '' || status !== 'all' || tier !== 'all' || grid.active

  return (
    <Page title="Clients" subtitle="Companies that place orders."
      action={canEdit ? <button onClick={() => setPanel({ mode: 'create' })}><Icon name="plus" />New client</button> : null}>
      {error && <p role="alert" className="error">{error}</p>}
      <Filters query={query} onQuery={setQuery} count={visible?.length ?? 0} total={items?.length ?? 0} dirty={dirty} placeholder="Search company, contact, email or phone"
        onClear={() => { grid.clear(); setQuery(''); setStatus('all'); setTier('all') }}>
        <Select label="Status" value={status} onChange={setStatus} options={[['all', 'All'], ...statuses.map((s): [string, string] => [s, label(s)])]} />
        <Select label="Tier" value={tier} onChange={setTier} options={[['all', 'All'], ...tiers.map((t): [string, string] => [t, t])]} />
        {grid.tools}
      </Filters>
      {grid.panel}
      {items === null && !error && <p className="muted">Loading...</p>}
      {visible?.length === 0 && <p className="empty">{items?.length ? 'No clients match the filters.' : 'No clients yet.'}</p>}
      {visible && visible.length > 0 && (
        <table>
          <thead><tr>{grid.head}<th><span className="sr">Actions</span></th></tr></thead>
          <tbody>
            {visible.map((c) => (
              <tr key={c.id}>
                {grid.cells(c)}
                <td data-label="" className="row-actions">{canEdit && <button className="secondary small" aria-label={`Edit ${c.companyName}`} onClick={() => setPanel({ mode: 'detail', id: c.id, edit: true })}><Icon name="edit" />Edit</button>}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {panel?.mode === 'create' && <Modal title="New client" onClose={close}><CreateForm onDone={saved} onCancel={close} /></Modal>}
      {panel?.mode === 'detail' && <Modal title={panel.edit ? 'Edit client' : 'Client'} readOnly={!panel.edit} onEdit={canEdit ? () => setPanel({ ...panel, edit: true }) : undefined} changeKey={panel.id} onClose={close}><Detail id={panel.id} onChanged={() => void load()} onClose={close} /></Modal>}
      {toast}
    </Page>
  )
}

const read = (form: HTMLFormElement): ClientInput => {
  const d = new FormData(form)
  const text = (k: string) => String(d.get(k)).trim()
  return { companyName: text('companyName'), contactName: text('contactName'), email: text('email'), phone: text('phone'), shippingAddress: text('shippingAddress'), tier: text('tier') as ClientTier }
}

function Fields({ client }: { client?: Client }) {
  return (
    <>
      <label>Company name<input name="companyName" required minLength={2} maxLength={150} defaultValue={client?.companyName} autoFocus /></label>
      <div className="row">
        <label>Contact name<input name="contactName" required minLength={2} maxLength={100} defaultValue={client?.contactName} /></label>
        <label>Tier<select name="tier" required defaultValue={client?.tier ?? ''}><option value="" disabled>-- Select --</option>{tiers.map((t) => <option key={t}>{t}</option>)}</select></label>
      </div>
      <div className="row">
        <label>Email<input name="email" type="email" required minLength={5} maxLength={100} defaultValue={client?.email} /></label>
        <label>Phone<input name="phone" type="tel" required minLength={7} maxLength={30} defaultValue={client?.phone} /></label>
      </div>
      <label>Shipping address<textarea name="shippingAddress" required maxLength={500} defaultValue={client?.shippingAddress} /></label>
    </>
  )
}

function CreateForm({ onDone, onCancel }: { onDone: (text: string) => void; onCancel: () => void }) {
  const { run, feedback } = useRunner()
  const submit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const input = read(e.currentTarget)
    return run(async () => {
      await api.createClient(input)
      onDone('Client created.')
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

function Detail({ id, onChanged, onClose }: { id: number; onChanged: () => void; onClose: () => void }) {
  const [client, setClient] = useState<Client | null>(null)
  const { run, feedback, error, setError } = useRunner()

  useEffect(() => {
    api.client(id).then(setClient).catch((e) => setError(message(e)))
  }, [id, setError])

  const save = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const input = { ...read(e.currentTarget), userId: client?.userId }
    const status = String(new FormData(e.currentTarget).get('status')) as ClientStatus
    return run(async () => {
      let next = await api.updateClient(id, input)
      if (status !== next.status) next = await api.setClientStatus(id, status)
      setClient(next)
      onChanged()
    }, 'Saved.')
  }

  if (!client) return error ? feedback : <p className="muted">Loading...</p>

  return (
    <>
      <p className="meta"><strong>{client.companyName}</strong><StatusBadge status={client.status} /><Badge kind="muted">Discount {client.tierDiscountPercent}%</Badge></p>
      <Form onSubmit={save} requireChange className="ro-h" key={JSON.stringify(client)}>
        <Fields client={client} />
        <label>Account status<select name="status" defaultValue={client.status}>{statuses.map((s) => <option key={s} value={s}>{label(s)}</option>)}</select></label>
        {feedback}
        <div className="actions"><Submit>Save</Submit><button type="button" className="secondary" onClick={onClose}>Close</button></div>
      </Form>
    </>
  )
}
