import { createContext, type FormEvent, type ReactNode, useCallback, useContext, useEffect, useRef, useState } from 'react'

export const message = (e: unknown) => (e instanceof Error ? e.message : 'Something went wrong.')

export function useList<T>(fetcher: () => Promise<T[]>) {
  const [items, setItems] = useState<T[] | null>(null)
  const [error, setError] = useState('')
  const load = useCallback(async () => {
    try {
      setItems(await fetcher())
      setError('')
    } catch (e) {
      setError(message(e))
    }
  }, [fetcher])
  useEffect(() => {
    void load()
  }, [load])
  return { items, error, load }
}

export function useRunner() {
  const [error, setError] = useState('')
  const [note, setNote] = useState('')
  const [busy, setBusy] = useState(false)
  async function run(action: () => Promise<void>, done?: string) {
    setError('')
    setNote('')
    setBusy(true)
    try {
      await action()
      if (done) setNote(done)
    } catch (e) {
      setError(message(e))
    } finally {
      setBusy(false)
    }
  }
  const feedback = (
    <>
      {error && <p role="alert" className="error">{error}</p>}
      {note && <p role="status" className="note">{note}</p>}
    </>
  )
  return { run, feedback, error, setError, busy }
}

const FormReady = createContext(true)

const snapshot = (form: HTMLFormElement) => JSON.stringify([...new FormData(form).entries()].map(([k, v]) => [k, typeof v === 'string' ? v : v.name]))

export function Form({ onSubmit, requireChange = false, changed = false, className, children }: { onSubmit: (e: FormEvent<HTMLFormElement>) => unknown; requireChange?: boolean; changed?: boolean; className?: string; children: ReactNode }) {
  const ref = useRef<HTMLFormElement>(null)
  const initial = useRef('')
  const [valid, setValid] = useState(false)
  const [edited, setEdited] = useState(false)
  const [busy, setBusy] = useState(false)

  const check = () => {
    const form = ref.current
    if (!form) return
    setValid(form.checkValidity())
    setEdited(snapshot(form) !== initial.current)
  }
  useEffect(() => {
    if (ref.current) initial.current = snapshot(ref.current)
    check()
  }, [])

  const submit = async (e: FormEvent<HTMLFormElement>) => {
    setBusy(true)
    try {
      await onSubmit(e)
    } finally {
      setBusy(false)
    }
  }

  return (
    <FormReady.Provider value={valid && !busy && (!requireChange || edited || changed)}>
      <form ref={ref} className={className} onSubmit={(e) => void submit(e)} onInput={check} onChange={check}>{children}</form>
    </FormReady.Provider>
  )
}

export function Submit({ children, disabled = false }: { children: ReactNode; disabled?: boolean }) {
  const ready = useContext(FormReady)
  return <button type="submit" disabled={!ready || disabled}>{children}</button>
}

export function useToast(): [ReactNode, (text: string) => void] {
  const [text, setText] = useState('')
  useEffect(() => {
    if (!text) return
    const t = setTimeout(() => setText(''), 3000)
    return () => clearTimeout(t)
  }, [text])
  return [text && <p role="status" className="toast">{text}</p>, setText]
}

const icons: Record<string, string> = {
  departments: 'M3 21V8l9-5 9 5v13h-6v-6H9v6zM8 10h2v2H8zm6 0h2v2h-2z',
  users: 'M16 11a4 4 0 1 0-8 0 4 4 0 0 0 8 0zm-4 6c-4 0-8 2-8 5h16c0-3-4-5-8-5z',
  roles: 'M12 2l8 3v6c0 5-3.5 9-8 11-4.5-2-8-6-8-11V5zm-1 12l6-6-1.4-1.4L11 11.2 8.4 8.6 7 10z',
  dashboard: 'M3 3h8v10H3zm10 0h8v6h-8zM3 15h8v6H3zm10-4h8v10h-8z',
  jobtypes: 'M4 5h16v2H4zm0 6h10v2H4zm0 6h16v2H4z',
  profile: 'M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8zm-8 9c0-4 4-6 8-6s8 2 8 6z',
  plus: 'M11 5h2v6h6v2h-6v6h-2v-6H5v-2h6z',
  search: 'M10 4a6 6 0 1 0 3.5 10.9l5 5 1.4-1.4-5-5A6 6 0 0 0 10 4zm0 2a4 4 0 1 1 0 8 4 4 0 0 1 0-8z',
  clients: 'M9 3h6v2h5v4H4V5h5zM4 11h16v9H4z',
  catalogs: 'M4 4h16v4H4zm0 6h16v10H4zm5 2v2h6v-2z',
  chains: 'M3 3h7v7H3zm11 0h7v7h-7zM8.5 14h7v7h-7zM6 10h2v3h8v-3h2v5H6z',
  edit: 'M3 17.25V21h3.75L18.8 8.94l-3.75-3.75zM20.7 7.04a1 1 0 0 0 0-1.41l-2.34-2.34a1 1 0 0 0-1.41 0l-1.83 1.83 3.75 3.75z',
  close: 'M6.4 5 12 10.6 17.6 5 19 6.4 13.4 12 19 17.6 17.6 19 12 13.4 6.4 19 5 17.6 10.6 12 5 6.4z',
}

export function Icon({ name }: { name: keyof typeof icons }) {
  return <svg className="icon" viewBox="0 0 24 24" aria-hidden="true"><path d={icons[name]} /></svg>
}

export function Page({ title, subtitle, action, children }: { title: string; subtitle: string; action: ReactNode; children: ReactNode }) {
  return (
    <>
      <header className="page-head">
        <div><h1>{title}</h1><p>{subtitle}</p></div>
        {action}
      </header>
      {children}
    </>
  )
}

export const locked = (p: { mode: string; edit?: boolean } | null) => p?.mode === 'create' || p?.edit === true

export function Modal({ title, onClose, readOnly = false, onEdit, changeKey, children }: { title: string; onClose: () => void; readOnly?: boolean; onEdit?: () => void; changeKey?: number; children: ReactNode }) {
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose()
    document.addEventListener('keydown', onKey)
    return () => document.removeEventListener('keydown', onKey)
  }, [onClose])
  return (
    <div className="overlay">
      <aside role="dialog" aria-label={title}>
        <div className="modal-head">
          <h2>{title}</h2>
          {readOnly && onEdit && <button className="secondary small" onClick={onEdit}><Icon name="edit" />Edit</button>}
          <button className="icon-btn" aria-label="Close" onClick={onClose}><Icon name="close" /></button>
          <span key={changeKey} className="switch-bar" />
        </div>
        <fieldset key={changeKey} className="pane" disabled={readOnly}>{children}</fieldset>
      </aside>
    </div>
  )
}

export function Badge({ kind = 'info', children }: { kind?: 'ok' | 'off' | 'warn' | 'info' | 'muted'; children: ReactNode }) {
  return <span className={`badge ${kind}`}>{children}</span>
}

export function StatusBadges({ active, system, activeLabel = 'Active', inactiveLabel = 'Inactive' }: { active: boolean; system?: boolean; activeLabel?: string; inactiveLabel?: string }) {
  return (
    <>
      <Badge kind={active ? 'ok' : 'off'}>{active ? activeLabel : inactiveLabel}</Badge>
      {system && <Badge>System</Badge>}
    </>
  )
}

export function Chips({ items, empty = '-' }: { items: string[]; empty?: string }) {
  return items.length ? <span className="chips">{items.map((i) => <Badge key={i} kind="muted">{i}</Badge>)}</span> : <span className="muted">{empty}</span>
}

export function Avatar({ name, large }: { name: string; large?: boolean }) {
  const initials = name.split(' ').map((p) => p[0]).slice(0, 2).join('').toUpperCase()
  return <span className={large ? "avatar xl" : "avatar"} style={{ background: `hsl(${[...name].reduce((h, c) => h + c.charCodeAt(0), 0) % 360} 55% 42%)` }}>{initials}</span>
}

export function ChipPicker({ options, selected, onToggle, addLabel = 'Add', empty = 'None selected', showChosen = true }: { options: { key: string; label: string; hint?: string }[]; selected: Set<string>; onToggle: (key: string) => void; addLabel?: string; empty?: string; showChosen?: boolean }) {
  const [open, setOpen] = useState(false)
  const [filter, setFilter] = useState('')
  const box = useRef<HTMLSpanElement>(null)
  const chosen = options.filter((o) => selected.has(o.key))
  const rest = options.filter((o) => !selected.has(o.key))
  const shown = rest.filter((o) => matches(filter, o.label, o.hint))

  useEffect(() => {
    if (!open) return
    const onDown = (e: MouseEvent) => { if (!box.current?.contains(e.target as Node)) setOpen(false) }
    const onKey = (e: KeyboardEvent) => {
      if (e.key !== 'Escape') return
      e.stopPropagation()
      setOpen(false)
    }
    document.addEventListener('mousedown', onDown)
    document.addEventListener('keydown', onKey, true)
    return () => {
      document.removeEventListener('mousedown', onDown)
      document.removeEventListener('keydown', onKey, true)
    }
  }, [open])

  useEffect(() => {
    if (rest.length === 0) setOpen(false)
  }, [rest.length])

  return (
    <div className="picker-chips">
      {options.length === 0 && <p className="muted">Nothing to choose from.</p>}
      {showChosen && options.length > 0 && chosen.length === 0 && <span className="muted">{empty}</span>}
      {showChosen && chosen.map((o) => (
        <span key={o.key} className="chip" title={o.hint}>
          {o.label}
          <button type="button" className="chip-x" aria-label={`Remove ${o.label}`} onClick={() => onToggle(o.key)}><Icon name="close" /></button>
        </span>
      ))}
      {rest.length > 0 && (
        <span className="chip-add-wrap" ref={box}>
          <button type="button" className="chip-add" aria-haspopup="listbox" aria-expanded={open} onClick={() => { setOpen((o) => !o); setFilter('') }}><Icon name="plus" />{addLabel}</button>
          {open && (
            <div className="menu" role="listbox" aria-label={addLabel}>
              {rest.length > 8 && <input type="search" aria-label="Filter" placeholder="Filter..." value={filter} onChange={(e) => setFilter(e.target.value)} autoFocus />}
              {shown.length === 0 && <p className="muted">No matches.</p>}
              {shown.map((o) => (
                <button type="button" role="option" aria-selected={false} key={o.key} onClick={() => onToggle(o.key)}>
                  {o.label}{o.hint && <small>{o.hint}</small>}
                </button>
              ))}
            </div>
          )}
        </span>
      )}
    </div>
  )
}

export function toggled(set: Set<string>, key: string) {
  const next = new Set(set)
  if (!next.delete(key)) next.add(key)
  return next
}

export function Filters({ query, onQuery, count, total, onClear, dirty, placeholder, children }: { query: string; onQuery: (q: string) => void; count: number; total: number; onClear: () => void; dirty: boolean; placeholder: string; children?: ReactNode }) {
  return (
    <section className="filters" aria-label="Filters">
      <label className="search"><Icon name="search" /><input type="search" placeholder={placeholder} aria-label="Search" value={query} onChange={(e) => onQuery(e.target.value)} /></label>
      {children}
      <span className="count" role="status">{count} of {total}</span>
      {dirty && <button className="link" onClick={onClear}>Clear filters</button>}
    </section>
  )
}

export function Select({ label, value, onChange, options }: { label: string; value: string; onChange: (v: string) => void; options: [string, string][] }) {
  return (
    <label className="select" data-active={value !== "all" || undefined}><span>{label}</span>
      <select aria-label={label} value={value} onChange={(e) => onChange(e.target.value)}><option value="all">-- Select --</option>{options.filter(([v]) => v !== "all").map(([v, t]) => <option key={v} value={v}>{t}</option>)}</select>
    </label>
  )
}

export const matches = (q: string, ...parts: (string | null | undefined)[]) => !q.trim() || parts.join(' ').toLowerCase().includes(q.trim().toLowerCase())

export function OrderedPicker({ label, options, value, onChange, empty = 'Nothing added yet.' }: { label: string; options: { id: number; label: string }[]; value: number[]; onChange: (ids: number[]) => void; empty?: string }) {
  const name = (id: number) => options.find((o) => o.id === id)?.label ?? `#${id}`
  const [drag, setDrag] = useState<{ from: number; over: number } | null>(null)
  const [was, setWas] = useState<Record<number, number>>({})
  const move = (from: number, to: number) => {
    if (from === to) return
    const next = [...value]
    next.splice(to, 0, next.splice(from, 1)[0])
    const id = value[from]
    setWas((w) => ({ ...w, [id]: w[id] ?? from + 1 }))
    onChange(next)
  }
  return (
    <div className="picker">
      {value.length === 0 && <p className="muted">{empty}</p>}
      <ol>
        {value.map((id, i) => (
          <li key={`${id}-${i}`} draggable className={drag?.from === i ? 'dragging' : drag?.over === i ? 'over' : undefined}
            onDragStart={(e) => {
              if (e.currentTarget.closest('fieldset:disabled')) return e.preventDefault()
              e.dataTransfer.effectAllowed = 'move'
              setDrag({ from: i, over: i })
            }}
            onDragOver={(e) => { if (drag) { e.preventDefault(); if (drag.over !== i) setDrag({ ...drag, over: i }) } }}
            onDrop={(e) => { e.preventDefault(); if (drag) move(drag.from, i); setDrag(null) }}
            onDragEnd={() => setDrag(null)}>
            <span className="step-no">{i + 1}</span><span className="step-name">{name(id)}</span>
            {was[id] !== undefined && was[id] !== i + 1 && <span className="moved-tag" title={`Moved from position ${was[id]}`}>was {was[id]}</span>}
            <button type="button" className="icon-btn" aria-label={`Move ${name(id)} up`} disabled={i === 0} onClick={() => move(i, i - 1)}>↑</button>
            <button type="button" className="icon-btn" aria-label={`Move ${name(id)} down`} disabled={i === value.length - 1} onClick={() => move(i, i + 1)}>↓</button>
            <button type="button" className="icon-btn" aria-label={`Remove ${name(id)}`} onClick={() => { setWas(({ [id]: _, ...rest }) => rest); onChange(value.filter((_, j) => j !== i)) }}>✕</button>
          </li>
        ))}
      </ol>
      <div className="add"><ChipPicker showChosen={false} addLabel={label} options={options.map((o) => ({ key: String(o.id), label: o.label }))} selected={new Set()} onToggle={(k) => onChange([...value, Number(k)])} /></div>
    </div>
  )
}

export const sameSet = (a: Set<string>, b: Set<string>) => a.size === b.size && [...a].every((k) => b.has(k))

export function Timeline({ items, empty = '-' }: { items: string[]; empty?: string }) {
  return items.length ? <ol className="tl">{items.map((t, i) => <li key={i}><span>{t}</span></li>)}</ol> : <span className="muted">{empty}</span>
}
