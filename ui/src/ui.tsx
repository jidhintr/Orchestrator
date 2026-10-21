import { type ReactNode, useCallback, useEffect, useState } from 'react'

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
  async function run(action: () => Promise<void>, done?: string) {
    setError('')
    setNote('')
    try {
      await action()
      if (done) setNote(done)
    } catch (e) {
      setError(message(e))
    }
  }
  const feedback = (
    <>
      {error && <p role="alert" className="error">{error}</p>}
      {note && <p role="status" className="note">{note}</p>}
    </>
  )
  return { run, feedback, error, setError }
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
  plus: 'M11 5h2v6h6v2h-6v6h-2v-6H5v-2h6z',
  search: 'M10 4a6 6 0 1 0 3.5 10.9l5 5 1.4-1.4-5-5A6 6 0 0 0 10 4zm0 2a4 4 0 1 1 0 8 4 4 0 0 1 0-8z',
  clients: 'M9 3h6v2h5v4H4V5h5zM4 11h16v9H4z',
  catalogs: 'M4 4h16v4H4zm0 6h16v10H4zm5 2v2h6v-2z',
  chains: 'M3 3h7v7H3zm11 0h7v7h-7zM8.5 14h7v7h-7zM6 10h2v3h8v-3h2v5H6z',
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

export function Modal({ title, onClose, children }: { title: string; onClose: () => void; children: ReactNode }) {
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && onClose()
    document.addEventListener('keydown', onKey)
    return () => document.removeEventListener('keydown', onKey)
  }, [onClose])
  return (
    <div className="overlay" onClick={onClose}>
      <aside role="dialog" aria-modal="true" aria-label={title} onClick={(e) => e.stopPropagation()}>
        <div className="modal-head"><h2>{title}</h2><button className="icon-btn" aria-label="Close" onClick={onClose}><Icon name="close" /></button></div>
        {children}
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

export function Avatar({ name }: { name: string }) {
  const initials = name.split(' ').map((p) => p[0]).slice(0, 2).join('').toUpperCase()
  return <span className="avatar" style={{ background: `hsl(${[...name].reduce((h, c) => h + c.charCodeAt(0), 0) % 360} 55% 42%)` }}>{initials}</span>
}

export function CheckList({ options, selected, onToggle, empty = 'Nothing to choose from.' }: { options: { key: string; label: string; hint?: string }[]; selected: Set<string>; onToggle: (key: string) => void; empty?: string }) {
  return (
    <div className="checklist">
      {options.length === 0 && <p className="muted">{empty}</p>}
      {options.map((o) => (
        <label key={o.key} className="check">
          <input type="checkbox" aria-label={o.label} checked={selected.has(o.key)} onChange={() => onToggle(o.key)} />
          <span>{o.label}{o.hint && <small>{o.hint}</small>}</span>
        </label>
      ))}
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
      {dirty && <button className="secondary" onClick={onClear}>Clear filters</button>}
    </section>
  )
}

export function Select({ label, value, onChange, options }: { label: string; value: string; onChange: (v: string) => void; options: [string, string][] }) {
  return (
    <label className="select"><span>{label}</span>
      <select aria-label={label} value={value} onChange={(e) => onChange(e.target.value)}>{options.map(([v, t]) => <option key={v} value={v}>{t}</option>)}</select>
    </label>
  )
}

export const matches = (q: string, ...parts: (string | null | undefined)[]) => !q.trim() || parts.join(' ').toLowerCase().includes(q.trim().toLowerCase())

export function OrderedPicker({ label, options, value, onChange, empty = 'Nothing added yet.' }: { label: string; options: { id: number; label: string }[]; value: number[]; onChange: (ids: number[]) => void; empty?: string }) {
  const [pick, setPick] = useState('')
  const name = (id: number) => options.find((o) => o.id === id)?.label ?? `#${id}`
  const move = (from: number, to: number) => {
    const next = [...value]
    next.splice(to, 0, next.splice(from, 1)[0])
    onChange(next)
  }
  return (
    <div className="picker">
      {value.length === 0 && <p className="muted">{empty}</p>}
      <ol>
        {value.map((id, i) => (
          <li key={`${id}-${i}`}>
            <span className="step-no">{i + 1}</span><span className="step-name">{name(id)}</span>
            <button type="button" className="icon-btn" aria-label={`Move ${name(id)} up`} disabled={i === 0} onClick={() => move(i, i - 1)}>↑</button>
            <button type="button" className="icon-btn" aria-label={`Move ${name(id)} down`} disabled={i === value.length - 1} onClick={() => move(i, i + 1)}>↓</button>
            <button type="button" className="icon-btn" aria-label={`Remove ${name(id)}`} onClick={() => onChange(value.filter((_, j) => j !== i))}>✕</button>
          </li>
        ))}
      </ol>
      <div className="add">
        <select aria-label={label} value={pick} onChange={(e) => setPick(e.target.value)}>
          <option value="">{label}</option>
          {options.map((o) => <option key={o.id} value={o.id}>{o.label}</option>)}
        </select>
        <button type="button" className="secondary" disabled={!pick} onClick={() => { onChange([...value, Number(pick)]); setPick('') }}>Add</button>
      </div>
    </div>
  )
}
