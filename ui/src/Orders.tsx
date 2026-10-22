import { useAccess } from './access'
import { type FormEvent, useEffect, useState } from 'react'
import { type Col, useGrid } from './grid'
import { api, type Catalog, type Client, type Order, type OrderEvent, type OrderPriority, type OrderStatus } from './api'
import { Badge, Filters, Icon, matches, message, Modal, Page, Select, Timeline, locked, useList, useRunner, useToast, Form, Submit } from './ui'

type Panel = { mode: 'create' } | { mode: 'detail'; id: number; edit?: boolean } | null

const statuses: OrderStatus[] = ['PendingApproval', 'InProgress', 'OnHold', 'Completed', 'Cancelled']
const priorities: OrderPriority[] = ['Normal', 'High', 'Premium', 'UltraHigh']
const statusKind = { PendingApproval: 'warn', InProgress: 'info', OnHold: 'warn', Completed: 'ok', Cancelled: 'off' } as const
const priorityKind = { Normal: 'muted', High: 'info', Premium: 'warn', UltraHigh: 'off' } as const
const label = (s: string) => s.replace(/([a-z])([A-Z])/g, '$1 $2')
const when = (iso: string) => new Date(iso).toLocaleString()
const money = (n: number) => n.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
const isOpen = (o: Order) => o.status !== 'Cancelled' && o.status !== 'Completed'

export function Orders() {
  const { items, error, load } = useList(api.orders)
  const [panel, setPanel] = useState<Panel>(null)
  const [query, setQuery] = useState('')
  const [status, setStatus] = useState('all')
  const [priority, setPriority] = useState('all')
  const [toast, notify] = useToast()
  const access = useAccess()
  const canManage = access.can('orderManage')
  const open = (next: Panel) => { if (!locked(panel)) setPanel(next) }

  const close = () => setPanel(null)
  const saved = (text: string) => {
    close()
    notify(text)
    void load()
  }

  const all: Col<Order>[] = [
    { key: 'id', label: 'Order', kind: 'number', locked: true, value: (o) => o.id, cell: (o) => <button className="link" onClick={() => open({ mode: 'detail', id: o.id })}>#{o.id}</button> },
    { key: 'client', label: 'Client', kind: 'text', value: (o) => o.clientCompanyName, cell: (o) => o.clientCompanyName },
    { key: 'catalog', label: 'Catalog', kind: 'text', value: (o) => o.catalogName, cell: (o) => <span className="stack"><span>{o.catalogName}</span><span className="muted">{o.catalogSku}</span></span> },
    { key: 'quantity', label: 'Qty', kind: 'number', value: (o) => o.quantity, cell: (o) => o.quantity },
    { key: 'total', label: 'Quote', kind: 'number', value: (o) => o.quotedTotal, cell: (o) => money(o.quotedTotal) },
    { key: 'due', label: 'Due', kind: 'text', value: (o) => o.dueDate ?? '', cell: (o) => o.dueDate ?? <span className="none" /> },
    { key: 'reference', label: 'Reference', kind: 'text', value: (o) => o.reference ?? '', cell: (o) => o.reference },
    { key: 'priority', label: 'Priority', kind: 'text', value: (o) => label(o.priority), cell: (o) => <Badge kind={priorityKind[o.priority]}>{label(o.priority)}</Badge> },
    { key: 'status', label: 'Status', kind: 'text', value: (o) => label(o.status), cell: (o) => <Badge kind={statusKind[o.status]}>{label(o.status)}</Badge> },
    { key: 'placed', label: 'Placed', kind: 'text', value: (o) => when(o.placedAt), cell: (o) => when(o.placedAt) },
  ]
  const grid = useGrid('grid:orders', all, ['reference'])

  const visible = items && grid.filter(items).filter((o) =>
    (status === 'all' || o.status === status) &&
    (priority === 'all' || o.priority === priority) &&
    matches(query, String(o.id), o.clientCompanyName, o.catalogName, o.catalogSku, o.reference))
  const dirty = query !== '' || status !== 'all' || priority !== 'all' || grid.active

  return (
    <Page title="Orders" subtitle="Orders placed by clients, from approval to delivery."
      action={<button onClick={() => setPanel({ mode: 'create' })}><Icon name="plus" />New order</button>}>
      {error && <p role="alert" className="error">{error}</p>}
      <Filters query={query} onQuery={setQuery} count={visible?.length ?? 0} total={items?.length ?? 0} dirty={dirty} placeholder="Search number, client, catalog or reference"
        onClear={() => { grid.clear(); setQuery(''); setStatus('all'); setPriority('all') }}>
        <Select label="Status" value={status} onChange={setStatus} options={[['all', 'All'], ...statuses.map((s): [string, string] => [s, label(s)])]} />
        <Select label="Priority" value={priority} onChange={setPriority} options={[['all', 'All'], ...priorities.map((p): [string, string] => [p, label(p)])]} />
        {grid.tools}
      </Filters>
      {grid.panel}
      {items === null && !error && <p className="muted">Loading...</p>}
      {visible?.length === 0 && <p className="empty">{items?.length ? 'No orders match the filters.' : 'No orders yet.'}</p>}
      {visible && visible.length > 0 && (
        <table>
          <thead><tr>{grid.head}<th><span className="sr">Actions</span></th></tr></thead>
          <tbody>
            {visible.map((o) => (
              <tr key={o.id}>
                {grid.cells(o)}
                <td data-label="" className="row-actions">{canManage && isOpen(o) && <button className="secondary small" aria-label={`Edit order ${o.id}`} onClick={() => setPanel({ mode: 'detail', id: o.id, edit: true })}><Icon name="edit" />Edit</button>}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {panel?.mode === 'create' && <Modal title="New order" onClose={close}><CreateForm onDone={saved} onCancel={close} /></Modal>}
      {panel?.mode === 'detail' && <Modal title={panel.edit ? 'Edit order' : 'Order'} readOnly={!panel.edit} onEdit={canManage ? () => setPanel({ ...panel, edit: true }) : undefined} changeKey={panel.id} onClose={close}><Detail id={panel.id} canManage={canManage} onChanged={() => void load()} onCancelled={saved} onClose={close} /></Modal>}
      {toast}
    </Page>
  )
}

const priorityField = (order?: Order) => (
  <label>Priority<select name="priority" required defaultValue={order?.priority ?? 'Normal'}>{priorities.map((p) => <option key={p} value={p}>{label(p)}</option>)}</select></label>
)

function CreateForm({ onDone, onCancel }: { onDone: (text: string) => void; onCancel: () => void }) {
  const { run, feedback, setError } = useRunner()
  const isClient = useAccess().user.departmentRoles.includes('Client')
  const [clients, setClients] = useState<Client[]>([])
  const [catalogs, setCatalogs] = useState<Catalog[]>([])
  const [catalogId, setCatalogId] = useState(0)
  const picked = catalogs.find((c) => c.id === catalogId)

  useEffect(() => {
    Promise.all([isClient ? Promise.resolve([]) : api.clients(), api.catalogs()])
      .then(([c, k]) => {
        setClients(c.filter((x) => x.isActive && x.status !== 'Suspended'))
        setCatalogs(k.filter((x) => x.isActive))
      })
      .catch((e) => setError(message(e)))
  }, [isClient, setError])

  const submit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const d = new FormData(e.currentTarget)
    const input = { clientId: isClient ? undefined : Number(d.get('clientId')), catalogId: Number(d.get('catalogId')), quantity: Number(d.get('quantity')), dueDate: String(d.get('dueDate')) || undefined, reference: String(d.get('reference')).trim() || undefined, notes: String(d.get('notes')).trim() || undefined, priority: String(d.get('priority')) as OrderPriority }
    return run(async () => {
      await api.placeOrder(input)
      onDone('Order placed.')
    })
  }
  return (
    <Form onSubmit={submit}>
      {!isClient && <label>Client<select name="clientId" required defaultValue=""><option value="" disabled>-- Select --</option>{clients.map((c) => <option key={c.id} value={c.id}>{c.companyName}</option>)}</select></label>}
      <label>Catalog<select name="catalogId" required defaultValue="" onChange={(e) => setCatalogId(Number(e.target.value))}><option value="" disabled>-- Select --</option>{catalogs.map((c) => <option key={c.id} value={c.id}>{c.name} ({c.sku})</option>)}</select></label>
      {picked && (
        <fieldset>
          <legend>Job steps of this catalog</legend>
          <Timeline items={[...picked.steps].sort((a, b) => a.sequence - b.sequence).map((s) => `${s.sequence}. ${s.jobTypeName}`)} empty="This catalog has no job steps." />
        </fieldset>
      )}
      <div className="row">
        <label>Quantity<input name="quantity" type="number" required min={1} max={1000000} step={1} /></label>
        <label>Due date<input name="dueDate" type="date" min={new Date().toISOString().slice(0, 10)} /></label>
      </div>
      <label>Reference / PO number<input name="reference" maxLength={100} /></label>
      <label>Notes<textarea name="notes" maxLength={1000} /></label>
      {priorityField()}
      {feedback}
      <div className="actions"><Submit>Place order</Submit><button type="button" className="secondary" onClick={onCancel}>Cancel</button></div>
    </Form>
  )
}

function Detail({ id, canManage, onChanged, onCancelled, onClose }: { id: number; canManage: boolean; onChanged: () => void; onCancelled: (text: string) => void; onClose: () => void }) {
  const [order, setOrder] = useState<Order | null>(null)
  const [events, setEvents] = useState<OrderEvent[]>([])
  const { run, feedback, error, setError } = useRunner()
  const access = useAccess()
  const isClient = access.user.departmentRoles.includes('Client')
  const canQuote = access.rank >= 3 || (access.rank >= 2 && access.user.departments.some((d) => d.id === 8))

  useEffect(() => {
    Promise.all([api.order(id), api.orderEvents(id)])
      .then(([o, ev]) => {
        setOrder(o)
        setEvents(ev)
      })
      .catch((e) => setError(message(e)))
  }, [id, setError])

  const save = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const d = new FormData(e.currentTarget)
    const reason = String(d.get('reason')).trim()
    const priority = String(d.get('priority')) as OrderPriority
    const total = Number(d.get('total'))
    return run(async () => {
      const quoteChanged = d.has('total') && total !== order?.quotedTotal
      if (quoteChanged) {
        if (reason.length < 3) throw new Error('Give a reason (at least 3 characters) for the quote change.')
        setOrder(await api.adjustOrderQuote(id, total, reason))
      }
      if (priority !== order?.priority) await api.requestOrderPriority(id, priority, reason || undefined)
      setEvents(await api.orderEvents(id))
      onChanged()
    }, 'Saved. A priority change is only a request: the order keeps its priority until Estimation approves.')
  }

  const cancel = () => {
    const reason = window.prompt('Reason for cancelling this order (min 3 characters)')?.trim()
    if (!reason) return
    return run(async () => {
      await api.cancelOrder(id, reason)
      onCancelled('Order cancelled and returned to Estimation.')
    })
  }

  if (!order) return error ? feedback : <p className="muted">Loading...</p>

  return (
    <>
      <p className="meta"><strong>Order #{order.id}</strong><Badge kind={statusKind[order.status]}>{label(order.status)}</Badge><Badge kind={priorityKind[order.priority]}>{label(order.priority)}</Badge></p>
      <Form onSubmit={save} requireChange className="ro-h" key={events.length}>
        <label>Client<input readOnly value={order.clientCompanyName} /></label>
        <label>Catalog<input readOnly value={`${order.catalogName} (${order.catalogSku})`} /></label>
        <label>Placed<input readOnly value={when(order.placedAt)} /></label>
        <label>Quantity<input readOnly value={order.quantity} /></label>
        <label>Unit price<input readOnly value={money(order.unitPrice)} /></label>
        <label>Discounts<input readOnly value={`Catalog ${order.catalogDiscountPercent}%, client ${order.clientDiscountPercent}%`} /></label>
        {canQuote && order.status === 'PendingApproval'
          ? <label>Quote total<input name="total" type="number" required min={0} step="0.01" defaultValue={order.quotedTotal} /></label>
          : <label>Quote total<input readOnly value={money(order.quotedTotal)} /></label>}
        {order.dueDate && <label>Due date<input readOnly value={order.dueDate} /></label>}
        {order.reference && <label>Reference<input readOnly value={order.reference} /></label>}
        {order.notes && <label>Notes<input readOnly value={order.notes} /></label>}
        {canManage && isOpen(order) && (
          <>
            <label>Request priority change<select name="priority" defaultValue={order.priority}>{priorities.map((p) => <option key={p} value={p}>{label(p)}</option>)}</select></label>
            <label className="edit-only">Reason<input name="reason" maxLength={500} /></label>
          </>
        )}
        <fieldset>
          <legend>Stages</legend>
          <Timeline items={order.stages.map((s) => `${s.sequence}. ${s.jobTypeName}${s.exitedAt ? ` - done ${when(s.exitedAt)}` : s.enteredAt ? ` - started ${when(s.enteredAt)}` : ' - waiting'}`)} />
        </fieldset>
        <fieldset>
          <legend>History</legend>
          <Timeline items={events.map((ev) => `${label(ev.type)}${ev.fromValue || ev.toValue ? `: ${ev.fromValue ? `${label(ev.fromValue)} to ` : ''}${label(ev.toValue ?? '')}` : ''} - ${when(ev.at)}${isClient ? '' : ` - user #${ev.actorUserId}`}${ev.reason ? ` - ${ev.reason}` : ''}`)} />
        </fieldset>
        {feedback}
        <div className="actions">
          {canManage && isOpen(order) && <Submit>Save</Submit>}
          {canManage && isOpen(order) && <button type="button" className="danger" onClick={cancel}>Cancel order</button>}
          <button type="button" className="secondary" onClick={onClose}>Close</button>
        </div>
      </Form>
    </>
  )
}
