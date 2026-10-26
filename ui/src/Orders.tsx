import { useAccess } from './access'
import { type FormEvent, useEffect, useState } from 'react'
import { type Col, useGrid } from './grid'
import { api, type Catalog, type Client, type Order, type OrderEvent, type OrderStatus, type Priority } from './api'
import { Badge, ChipPicker, Filters, Icon, matches, message, Modal, Page, Select, Timeline, locked, useList, useRunner, useToast, Form, Submit } from './ui'

type Panel = { mode: 'create' } | { mode: 'detail'; id: number; edit?: boolean } | null

const statuses: OrderStatus[] = ['PendingApproval', 'InProgress', 'OnHold', 'Completed', 'Cancelled']
const statusKind = { PendingApproval: 'warn', InProgress: 'info', OnHold: 'warn', Completed: 'ok', Cancelled: 'off' } as const
const label = (s: string) => s.replace(/([a-z])([A-Z])/g, '$1 $2')
const when = (iso: string) => new Date(iso).toLocaleString()
const money = (n: number) => n.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })
const isOpen = (o: Order) => o.status !== 'Cancelled' && o.status !== 'Completed'
const priorityKind = (rank: number) => (rank <= 1 ? 'muted' : rank === 2 ? 'info' : rank === 3 ? 'warn' : 'off')
const parseSpecs = (text: string) => text.split(/\r?\n/).map((l) => l.trim()).filter(Boolean).map((l) => {
  const i = l.indexOf(':')
  return i < 0 ? { name: l, value: '' } : { name: l.slice(0, i).trim(), value: l.slice(i + 1).trim() }
})
const today = new Date().toISOString().slice(0, 10)
const summary = (o: Order) => o.items.map((i) => `${i.catalogName} x${i.quantity}`).join(', ')

export function Orders() {
  const { items, error, load } = useList(api.orders)
  const priorityList = useList(api.priorities)
  const [panel, setPanel] = useState<Panel>(null)
  const [query, setQuery] = useState('')
  const [status, setStatus] = useState('all')
  const [priority, setPriority] = useState('all')
  const [toast, notify] = useToast()
  const access = useAccess()
  const canManage = access.can('orderManage')
  const open = (next: Panel) => { if (!locked(panel)) setPanel(next) }
  const priorities = priorityList.items ?? []

  const close = () => setPanel(null)
  const saved = (text: string) => {
    close()
    notify(text)
    void load()
  }

  const all: Col<Order>[] = [
    { key: 'id', label: 'Order', kind: 'number', locked: true, value: (o) => o.id, cell: (o) => <button className="link" onClick={() => open({ mode: 'detail', id: o.id })}>{o.orderNo}</button> },
    { key: 'client', label: 'Client', kind: 'text', value: (o) => o.clientCompanyName, cell: (o) => o.clientCompanyName },
    { key: 'items', label: 'Items', kind: 'text', value: summary, cell: (o) => <span className="muted">{summary(o)}</span> },
    { key: 'net', label: 'Net amount', kind: 'number', value: (o) => o.netAmount, cell: (o) => money(o.netAmount) },
    { key: 'deadline', label: 'Deadline', kind: 'text', value: (o) => o.commitmentDeadline ?? '', cell: (o) => o.commitmentDeadline ?? <span className="none" /> },
    { key: 'reference', label: 'Reference', kind: 'text', value: (o) => o.reference ?? '', cell: (o) => o.reference },
    { key: 'priority', label: 'Priority', kind: 'text', value: (o) => o.priorityName, cell: (o) => <Badge kind={priorityKind(priorities.find((p) => p.id === o.priorityId)?.rank ?? 1)}>{o.priorityName}</Badge> },
    { key: 'status', label: 'Status', kind: 'text', value: (o) => label(o.status), cell: (o) => <Badge kind={statusKind[o.status]}>{label(o.status)}</Badge> },
    { key: 'placed', label: 'Placed', kind: 'text', value: (o) => when(o.placedAt), cell: (o) => when(o.placedAt) },
  ]
  const grid = useGrid('grid:orders', all, ['reference'])

  const visible = items && grid.filter(items).filter((o) =>
    (status === 'all' || o.status === status) &&
    (priority === 'all' || String(o.priorityId) === priority) &&
    matches(query, o.orderNo, o.clientCompanyName, summary(o), o.reference))
  const dirty = query !== '' || status !== 'all' || priority !== 'all' || grid.active

  return (
    <Page title="Orders" subtitle="Orders placed by clients, from approval to delivery."
      action={<button onClick={() => setPanel({ mode: 'create' })}><Icon name="plus" />New order</button>}>
      {error && <p role="alert" className="error">{error}</p>}
      <Filters query={query} onQuery={setQuery} count={visible?.length ?? 0} total={items?.length ?? 0} dirty={dirty} placeholder="Search number, client, item or reference"
        onClear={() => { grid.clear(); setQuery(''); setStatus('all'); setPriority('all') }}>
        <Select label="Status" value={status} onChange={setStatus} options={[['all', 'All'], ...statuses.map((s): [string, string] => [s, label(s)])]} />
        <Select label="Priority" value={priority} onChange={setPriority} options={[['all', 'All'], ...priorities.map((p): [string, string] => [String(p.id), p.name])]} />
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
                <td data-label="" className="row-actions">{canManage && isOpen(o) && <button className="secondary small" aria-label={`Edit order ${o.orderNo}`} onClick={() => setPanel({ mode: 'detail', id: o.id, edit: true })}><Icon name="edit" />Edit</button>}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      {panel?.mode === 'create' && <Modal title="New order" onClose={close}><CreateForm priorities={priorities} onDone={saved} onCancel={close} /></Modal>}
      {panel?.mode === 'detail' && <Modal title={panel.edit ? 'Edit order' : 'Order'} readOnly={!panel.edit} onEdit={canManage ? () => setPanel({ ...panel, edit: true }) : undefined} changeKey={panel.id} onClose={close}><Detail id={panel.id} priorities={priorities} canManage={canManage} onChanged={() => void load()} onCancelled={saved} onClose={close} /></Modal>}
      {toast}
    </Page>
  )
}

const priorityOptions = (priorities: Priority[]) => priorities.filter((p) => p.isActive).map((p) => <option key={p.id} value={p.id}>{p.name}{p.cost ? ` (+${money(p.cost)})` : ''}</option>)

function CreateForm({ priorities, onDone, onCancel }: { priorities: Priority[]; onDone: (text: string) => void; onCancel: () => void }) {
  const { run, feedback, setError } = useRunner()
  const isClient = useAccess().user.departmentRoles.includes('Client')
  const [clients, setClients] = useState<Client[]>([])
  const [catalogs, setCatalogs] = useState<Catalog[]>([])
  const [chosen, setChosen] = useState<Set<string>>(new Set())

  useEffect(() => {
    Promise.all([isClient ? Promise.resolve([]) : api.clients(), api.catalogs()])
      .then(([c, k]) => {
        setClients(c.filter((x) => x.isActive && x.status !== 'Suspended'))
        setCatalogs(k.filter((x) => x.isActive && x.isApproved && (!x.availableFrom || x.availableFrom <= today) && (!x.availableTo || x.availableTo >= today)))
      })
      .catch((e) => setError(message(e)))
  }, [isClient, setError])

  const toggle = (key: string) => setChosen((cur) => {
    const next = new Set(cur)
    if (!next.delete(key)) next.add(key)
    return next
  })

  const submit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const d = new FormData(e.currentTarget)
    const input = {
      clientId: isClient ? undefined : Number(d.get('clientId')),
      items: [...chosen].map((id) => ({ catalogId: Number(id), quantity: Number(d.get(`qty-${id}`)), description: String(d.get(`desc-${id}`)).trim() || undefined, specs: parseSpecs(String(d.get(`specs-${id}`))) })),
      priorityId: Number(d.get('priorityId')),
      commitmentDeadline: String(d.get('commitmentDeadline')) || undefined,
      reference: String(d.get('reference')).trim() || undefined,
      notes: String(d.get('notes')).trim() || undefined,
    }
    return run(async () => {
      await api.placeOrder(input)
      onDone('Order placed.')
    })
  }

  return (
    <Form onSubmit={submit}>
      {!isClient && <label>Client<select name="clientId" required defaultValue=""><option value="" disabled>-- Select --</option>{clients.map((c) => <option key={c.id} value={c.id}>{c.companyName}</option>)}</select></label>}
      <fieldset>
        <legend>Items</legend>
        <ChipPicker addLabel="Add item" empty="No items yet" showChosen={false} options={catalogs.map((c) => ({ key: String(c.id), label: c.name, hint: c.sku }))} selected={chosen} onToggle={toggle} />
        {catalogs.filter((c) => chosen.has(String(c.id))).map((c) => (
          <div key={c.id}>
            <div className="row">
              <label>{c.name} ({c.sku}) - quantity<input name={`qty-${c.id}`} type="number" required min={1} max={1000000} step={1} defaultValue={1} /></label>
              <button type="button" className="secondary small" aria-label={`Remove ${c.name}`} onClick={() => toggle(String(c.id))}>Remove</button>
            </div>
            <label>Description<input name={`desc-${c.id}`} maxLength={500} placeholder="What is printed" /></label>
            <label>Specs (one per line, Name: value)<textarea name={`specs-${c.id}`} rows={3} placeholder="Size: A4, then GSM: 300 on the next line" /></label>
          </div>
        ))}
      </fieldset>
      <div className="row">
        <label>Priority<select name="priorityId" required defaultValue={priorities.find((p) => p.rank === 1)?.id ?? ''}>{priorityOptions(priorities)}</select></label>
        <label>Commitment deadline<input name="commitmentDeadline" type="date" min={new Date().toISOString().slice(0, 10)} /></label>
      </div>
      <label>Reference / PO number<input name="reference" maxLength={100} /></label>
      <label>Notes<textarea name="notes" maxLength={1000} /></label>
      {feedback}
      <div className="actions"><Submit disabled={chosen.size === 0}>Place order</Submit><button type="button" className="secondary" onClick={onCancel}>Cancel</button></div>
    </Form>
  )
}

function Detail({ id, priorities, canManage, onChanged, onCancelled, onClose }: { id: number; priorities: Priority[]; canManage: boolean; onChanged: () => void; onCancelled: (text: string) => void; onClose: () => void }) {
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
    const priorityId = Number(d.get('priorityId'))
    const total = Number(d.get('total'))
    return run(async () => {
      if (d.has('total') && total !== order?.netAmount) {
        if (reason.length < 3) throw new Error('Give a reason (at least 3 characters) for the quote change.')
        setOrder(await api.adjustOrderQuote(id, total, reason))
      }
      if (priorityId !== order?.priorityId) await api.requestOrderPriority(id, priorityId, reason || undefined)
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
      <p className="meta"><strong>Order {order.orderNo}</strong><Badge kind={statusKind[order.status]}>{label(order.status)}</Badge><Badge kind={priorityKind(priorities.find((p) => p.id === order.priorityId)?.rank ?? 1)}>{order.priorityName}</Badge></p>
      <Form onSubmit={save} requireChange className="ro-h" key={events.length}>
        <label>Client<input readOnly value={order.clientCompanyName} /></label>
        <label>Placed<input readOnly value={when(order.placedAt)} /></label>
        {order.commitmentDeadline && <label>Commitment deadline<input readOnly value={order.commitmentDeadline} /></label>}
        {order.reference && <label>Reference<input readOnly value={order.reference} /></label>}
        {order.notes && <label>Notes<input readOnly value={order.notes} /></label>}
        <fieldset>
          <legend>Items</legend>
          <table>
            <thead><tr><th>Item</th><th className="num">Qty</th><th className="num">Unit price</th><th className="num">Discounts</th><th className="num">Line net</th></tr></thead>
            <tbody>
              {order.items.map((i) => (
                <tr key={i.id}>
                  <td data-label="Item"><span className="stack"><span>{i.catalogName}</span><span className="muted">{i.catalogSku}</span>{i.description && <span className="muted">{i.description}</span>}{i.specs.map((x) => <span key={x.name} className="muted">{x.name}: {x.value}</span>)}</span></td>
                  <td data-label="Qty" className="num">{i.quantity}</td>
                  <td data-label="Unit price" className="num">{money(i.unitPrice)}</td>
                  <td data-label="Discounts" className="num">{i.catalogDiscountPercent}% + {i.clientDiscountPercent}%</td>
                  <td data-label="Line net" className="num">{money(i.lineNet)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </fieldset>
        <label>Subtotal<input readOnly value={money(order.subTotal)} /></label>
        <label>Discounts<input readOnly value={`-${money(order.discountTotal)}`} /></label>
        <label>Priority cost<input readOnly value={money(order.priorityCost)} /></label>
        {canQuote && order.status === 'PendingApproval'
          ? <label>Net amount<input name="total" type="number" required min={0} step="0.01" defaultValue={order.netAmount} /></label>
          : <label>Net amount<input readOnly value={money(order.netAmount)} /></label>}
        {canManage && isOpen(order) && (
          <>
            <label>Request priority change<select name="priorityId" defaultValue={order.priorityId}>{priorityOptions(priorities)}</select></label>
            <label className="edit-only">Reason<input name="reason" maxLength={500} /></label>
          </>
        )}
        <fieldset>
          <legend>Job steps</legend>
          {order.items.map((i) => (
            <div key={i.id}>
              <strong>{i.catalogName}</strong>
              <Timeline items={i.steps.map((s) => `${s.sortOrder}. ${s.jobTypeName} - ${label(s.status)}`)} />
            </div>
          ))}
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
