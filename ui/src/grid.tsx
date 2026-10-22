import { type ReactNode, useEffect, useRef, useState } from 'react'
import { Icon } from './ui'

export type Col<T> = { key: string; label: string; kind: 'text' | 'number'; value: (row: T) => string | number; cell: (row: T) => ReactNode; locked?: boolean }
export type Rule = { id: number; col: string; op: string; value: string }

const ops = {
  text: [['contains', 'contains'], ['equals', 'equals'], ['starts', 'starts with'], ['not', 'does not contain']],
  number: [['eq', '='], ['gt', '>'], ['lt', '<'], ['gte', '>='], ['lte', '<=']],
} as const

type Saved = { order: string[]; hidden: string[] }

function load(key: string, all: string[], defaultHidden: string[]): Saved {
  try {
    const saved = JSON.parse(localStorage.getItem(key) ?? 'null') as Saved | null
    if (saved) {
      const order = saved.order.filter((k) => all.includes(k))
      return { order: [...order, ...all.filter((k) => !order.includes(k))], hidden: saved.hidden.filter((k) => all.includes(k)) }
    }
  } catch { /* storage unavailable: use defaults */ }
  return { order: all, hidden: defaultHidden }
}

function useColumns<T>(storeKey: string, all: Col<T>[], defaultHidden: string[]) {
  const keys = all.map((c) => c.key)
  const [state, setState] = useState(() => load(storeKey, keys, defaultHidden))
  const update = (next: Saved) => {
    setState(next)
    try { localStorage.setItem(storeKey, JSON.stringify(next)) } catch { /* storage unavailable: keep in memory */ }
  }
  const ordered = state.order.map((k) => all.find((c) => c.key === k)!)
  return {
    ordered,
    visible: ordered.filter((c) => c.locked || !state.hidden.includes(c.key)),
    hidden: state.hidden,
    toggle: (k: string) => update({ ...state, hidden: state.hidden.includes(k) ? state.hidden.filter((h) => h !== k) : [...state.hidden, k] }),
    move: (from: number, to: number) => {
      const order = [...state.order]
      order.splice(to, 0, order.splice(from, 1)[0])
      update({ ...state, order })
    },
    reset: () => update({ order: keys, hidden: defaultHidden }),
  }
}

function ColumnChooser<T>({ columns }: { columns: ReturnType<typeof useColumns<T>> }) {
  const [open, setOpen] = useState(false)
  const [drag, setDrag] = useState<number | null>(null)
  const box = useRef<HTMLSpanElement>(null)

  useEffect(() => {
    if (!open) return
    const onDown = (e: MouseEvent) => { if (!box.current?.contains(e.target as Node)) setOpen(false) }
    const onKey = (e: KeyboardEvent) => { if (e.key === 'Escape') { e.stopPropagation(); setOpen(false) } }
    document.addEventListener('mousedown', onDown)
    document.addEventListener('keydown', onKey, true)
    return () => {
      document.removeEventListener('mousedown', onDown)
      document.removeEventListener('keydown', onKey, true)
    }
  }, [open])

  return (
    <span className="chip-add-wrap" ref={box}>
      <button type="button" className="secondary small" aria-haspopup="true" aria-expanded={open} onClick={() => setOpen((o) => !o)}>Columns</button>
      {open && (
        <div className="menu col-menu" role="group" aria-label="Columns">
          <p className="muted">Tick to show, drag to reorder.</p>
          {columns.ordered.map((c, i) => (
            <label key={c.key} draggable className={drag === i ? 'dragging' : undefined}
              onDragStart={() => setDrag(i)} onDragOver={(e) => e.preventDefault()}
              onDrop={() => { if (drag !== null) columns.move(drag, i); setDrag(null) }} onDragEnd={() => setDrag(null)}>
              <span className="grip" aria-hidden="true">⋮⋮</span>
              <input type="checkbox" checked={c.locked || !columns.hidden.includes(c.key)} disabled={c.locked} onChange={() => columns.toggle(c.key)} />
              {c.label}
            </label>
          ))}
          <button type="button" className="link" onClick={columns.reset}>Reset columns</button>
        </div>
      )}
    </span>
  )
}

function AdvancedFilter<T>({ columns, rules, onChange }: { columns: Col<T>[]; rules: Rule[]; onChange: (rules: Rule[]) => void }) {
  const patch = (id: number, change: Partial<Rule>) => onChange(rules.map((r) => (r.id === id ? { ...r, ...change } : r)))
  const kindOf = (key: string) => columns.find((c) => c.key === key)?.kind ?? 'text'
  const add = () => onChange([...rules, { id: Date.now(), col: columns[0].key, op: 'contains', value: '' }])

  return (
    <section className="adv" aria-label="Advanced filter">
      {rules.length === 0 && <p className="muted">No rules. All rules must match.</p>}
      {rules.map((r) => (
        <div className="adv-rule" key={r.id}>
          <select aria-label="Column" value={r.col} onChange={(e) => patch(r.id, { col: e.target.value, op: ops[kindOf(e.target.value)][0][0] })}>
            {columns.map((c) => <option key={c.key} value={c.key}>{c.label}</option>)}
          </select>
          <select aria-label="Operator" value={r.op} onChange={(e) => patch(r.id, { op: e.target.value })}>
            {ops[kindOf(r.col)].map(([v, t]) => <option key={v} value={v}>{t}</option>)}
          </select>
          <input aria-label="Value" type={kindOf(r.col) === 'number' ? 'number' : 'text'} step="any" placeholder="Value" value={r.value} onChange={(e) => patch(r.id, { value: e.target.value })} />
          <button type="button" className="icon-btn" aria-label="Remove rule" onClick={() => onChange(rules.filter((x) => x.id !== r.id))}><Icon name="close" /></button>
        </div>
      ))}
      <div className="actions">
        <button type="button" className="secondary small" onClick={add}><Icon name="plus" />Add rule</button>
        {rules.length > 0 && <button type="button" className="link" onClick={() => onChange([])}>Clear rules</button>}
      </div>
    </section>
  )
}

function applyRules<T>(rows: T[], rules: Rule[], columns: Col<T>[]) {
  const active = rules.filter((r) => r.value.trim() !== '')
  if (active.length === 0) return rows
  return rows.filter((row) => active.every((r) => {
    const col = columns.find((c) => c.key === r.col)
    if (!col) return true
    const v = col.value(row)
    if (col.kind === 'number') {
      const n = Number(v)
      const x = Number(r.value)
      return { eq: n === x, gt: n > x, lt: n < x, gte: n >= x, lte: n <= x }[r.op] ?? true
    }
    const t = String(v).toLowerCase()
    const x = r.value.trim().toLowerCase()
    return { contains: t.includes(x), equals: t === x, starts: t.startsWith(x), not: !t.includes(x) }[r.op] ?? true
  }))
}

export function useGrid<T>(storeKey: string, all: Col<T>[], defaultHidden: string[] = []) {
  const columns = useColumns(storeKey, all, defaultHidden)
  const [rules, setRules] = useState<Rule[]>([])
  const [open, setOpen] = useState(false)
  const num = (c: Col<T>) => (c.kind === 'number' ? 'num' : undefined)
  return {
    active: rules.length > 0,
    clear: () => setRules([]),
    filter: (rows: T[]) => applyRules(rows, rules, all),
    tools: (
      <>
        <ColumnChooser columns={columns} />
        <button type="button" className={open || rules.length ? 'small' : 'secondary small'} aria-expanded={open} onClick={() => setOpen((o) => !o)}>Advanced{rules.length ? ` (${rules.length})` : ''}</button>
      </>
    ),
    panel: open ? <AdvancedFilter columns={all} rules={rules} onChange={setRules} /> : null,
    head: columns.visible.map((c) => <th key={c.key} className={num(c)}>{c.label}</th>),
    cells: (row: T) => columns.visible.map((c) => <td key={c.key} data-label={c.label} className={num(c)}>{c.cell(row)}</td>),
  }
}
