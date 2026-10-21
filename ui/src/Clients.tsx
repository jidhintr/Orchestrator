import { type FormEvent, useEffect, useState } from 'react'
import { api, type Client, type ClientInput, type ClientStatus, type ClientTier } from './api'
import { Badge, Filters, Icon, matches, message, Modal, Page, Select, useList, useRunner, useToast } from './ui'

type Panel = { mode: 'create' } | { mode: 'detail'; id: number } | null

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

  const close = () => setPanel(null)
  const saved = (text: string) => {
    close()
    notify(text)
    void load()
  }

  const visible = items?.filter((c) =>
    (status === 'all' || c.status === status) &&
    (tier === 'all' || c.tier === tier) &&
    matches(query, c.companyName, c.contactName, c.email, c.phone))
  const dirty = query !== '' || status !== 'all' || tier !== 'all'

  return (
    <Page title="Clients" subtitle="Companies that place orders."
      action={<button onClick={() => setPanel({ mode: 'create' })}><Icon name="plus" />New client</button>}>
      {error && <p role="alert" className="error">{error}</p>}
      <Filters query={query} onQuery={setQuery} count={visible?.length ?? 0} total={items?.length ?? 0} dirty={dirty} placeholder="Search company, contact, email or phone"
        onClear={() => { setQuery(''); setStatus('all'); setTier('all') }}>
        <Select label="Status" value={status} onChange={setStatus} options={[['all', 'All'], ...statuses.map((s): [string, string] => [s, label(s)])]} />
        <Select label="Tier" value={tier} onChange={setTier} options={[['all', 'All'], ...tiers.map((t): [string, string] => [t, t])]} />
      </Filters>
      {items === null && !error && <p className="muted">Loading...</p>}
      {visible?.length === 0 && <p className="empty">{items?.length ? 'No clients match the filters.' : 'No clients yet.'}</p>}
      {visible && visible.length > 0 && (
        <table>
          <thead><tr><th>Company</th><th>Contact</th><th>Tier</th><th>Discount</th><th>Status</th></tr></thead>
          <tbody>
            {visible.map((c) => (
              <tr key={c.id}>
                <td data-label="Company"><button className="link" onClick={() => setPanel({ mode: 'detail', id: c.id })}>{c.companyName}</button></td>
                <td data-label="Contact"><span className="stack"><span>{c.contactName}</span><span className="muted">{c.email}</span></span></td>
                <td data-label="Tier"><Badge kind={c.tier === 'Normal' ? 'muted' : 'info'}>{c.tier}</Badge></td>
                <td data-label="Discount">{c.tierDiscountPercent}%</td>
                <td data-label="Status"><StatusBadge status={c.status} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {panel?.mode === 'create' && <Modal title="New client" onClose={close}><CreateForm onDone={saved} onCancel={close} /></Modal>}
      {panel?.mode === 'detail' && <Modal title="Client" onClose={close}><Detail id={panel.id} onChanged={() => void load()} onClose={close} /></Modal>}
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
        <label>Tier<select name="tier" defaultValue={client?.tier ?? 'Normal'}>{tiers.map((t) => <option key={t}>{t}</option>)}</select></label>
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
    void run(async () => {
      await api.createClient(input)
      onDone('Client created.')
    })
  }
  return (
    <form onSubmit={submit}>
      <Fields />
      {feedback}
      <div className="actions"><button type="submit">Create</button><button type="button" className="secondary" onClick={onCancel}>Cancel</button></div>
    </form>
  )
}

function Detail({ id, onChanged, onClose }: { id: number; onChanged: () => void; onClose: () => void }) {
  const [client, setClient] = useState<Client | null>(null)
  const [status, setStatus] = useState<ClientStatus>('Active')
  const { run, feedback, error, setError } = useRunner()

  const apply = (c: Client) => {
    setClient(c)
    setStatus(c.status)
    onChanged()
  }

  useEffect(() => {
    api.client(id).then((c) => {
      setClient(c)
      setStatus(c.status)
    }).catch((e) => setError(message(e)))
  }, [id, setError])

  const save = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const input = read(e.currentTarget)
    return run(async () => apply(await api.updateClient(id, input)), 'Saved.')
  }

  if (!client) return error ? feedback : <p className="muted">Loading...</p>

  return (
    <>
      <p className="meta"><strong>{client.companyName}</strong><StatusBadge status={client.status} /><Badge kind="muted">Discount {client.tierDiscountPercent}%</Badge></p>
      <form onSubmit={save} key={JSON.stringify(client)}>
        <Fields client={client} />
        <div className="actions"><button type="submit">Save details</button></div>
      </form>
      <fieldset>
        <legend>Account status</legend>
        <label>Status<select aria-label="Status" value={status} onChange={(e) => setStatus(e.target.value as ClientStatus)}>{statuses.map((s) => <option key={s} value={s}>{label(s)}</option>)}</select></label>
        <div className="actions"><button disabled={status === client.status} onClick={() => run(async () => apply(await api.setClientStatus(id, status)), 'Status saved.')}>Save status</button></div>
      </fieldset>
      {feedback}
      <div className="actions"><button className="secondary" onClick={onClose}>Close</button></div>
    </>
  )
}
