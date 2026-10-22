import { type ReactNode, useCallback, useEffect, useRef, useState } from 'react'

type Health = { state: 'checking' } | { state: 'ok' } | { state: 'down'; cause: 'api' | 'database' }
type Toast = { id: number; kind: 'down' | 'still' | 'back' }

const PROBE_TIMEOUT_MS = 5000
const OK_EVERY_MS = 15000
const DOWN_EVERY_MS = 5000
const TOAST_MS = 12000
const BACK_MS = 3500

// Exact body match so a static host that answers every path with index.html never looks healthy.
async function probe(path: string): Promise<boolean> {
  const controller = new AbortController()
  const timer = setTimeout(() => controller.abort(), PROBE_TIMEOUT_MS)
  try {
    const response = await fetch(path, { signal: controller.signal, cache: 'no-store' })
    return response.ok && (await response.text()).trim() === 'Healthy'
  } catch {
    return false
  } finally {
    clearTimeout(timer)
  }
}

async function checkHealth(): Promise<Health> {
  if (!(await probe('/api/health/live'))) return { state: 'down', cause: 'api' }
  return (await probe('/api/health/ready')) ? { state: 'ok' } : { state: 'down', cause: 'database' }
}

export function HealthProvider({ children }: { children: ReactNode }) {
  const [health, setHealth] = useState<Health>({ state: 'checking' })
  const [checking, setChecking] = useState(false)
  const [toast, setToast] = useState<Toast | null>(null)
  const previous = useRef<Health['state']>('checking')
  const inFlight = useRef(false)
  const toastId = useRef(0)

  const show = useCallback((kind: Toast['kind']) => setToast({ id: ++toastId.current, kind }), [])

  const check = useCallback(async (manual: boolean) => {
    if (inFlight.current) return
    inFlight.current = true
    setChecking(true)
    const next = await checkHealth()
    inFlight.current = false
    setChecking(false)
    setHealth(next)
    const before = previous.current
    previous.current = next.state
    if (next.state === 'down' && (before !== 'down' || manual)) show(before === 'down' ? 'still' : 'down')
    if (next.state === 'ok' && before === 'down') {
      show('back')
      dispatchEvent(new Event('api:recovered'))
    }
  }, [show])

  useEffect(() => {
    let cancelled = false
    let timer: ReturnType<typeof setTimeout>
    const tick = async () => {
      await check(false)
      if (!cancelled) timer = setTimeout(() => void tick(), previous.current === 'down' ? DOWN_EVERY_MS : OK_EVERY_MS)
    }
    void tick()
    const now = () => void check(false)
    const onFailure = () => {
      if (previous.current === 'down') show('down')
      void check(false)
    }
    const onVisible = () => document.visibilityState === 'visible' && now()
    addEventListener('online', now)
    addEventListener('offline', now)
    addEventListener('api:failure', onFailure)
    document.addEventListener('visibilitychange', onVisible)
    return () => {
      cancelled = true
      clearTimeout(timer)
      removeEventListener('online', now)
      removeEventListener('offline', now)
      removeEventListener('api:failure', onFailure)
      document.removeEventListener('visibilitychange', onVisible)
    }
  }, [check, show])

  const down = health.state === 'down'
  const banner = useRef<HTMLDivElement>(null)
  useEffect(() => {
    const root = document.documentElement
    root.classList.toggle('has-banner', down)
    const el = banner.current
    if (!down || !el) return
    const size = new ResizeObserver(() => root.style.setProperty('--banner-h', `${el.offsetHeight}px`))
    size.observe(el)
    return () => size.disconnect()
  }, [down])

  useEffect(() => {
    if (!toast) return
    const timer = setTimeout(() => setToast(null), toast.kind === 'back' ? BACK_MS : TOAST_MS)
    return () => clearTimeout(timer)
  }, [toast])

  return (
    <>
      {children}
      {health.state === 'down' && (
        <div className="health-banner" role="alert" ref={banner}>
          <strong>Service unavailable</strong>
          <span>
            {health.cause === 'database'
              ? 'The API is running but cannot reach its database, so nothing can be loaded or saved.'
              : 'The API cannot be reached, so nothing can be loaded or saved.'}{' '}
            Retrying automatically.
          </span>
        </div>
      )}
      {toast && (
        <div className={`health-toast ${toast.kind === 'back' ? 'good' : 'bad'}`} role="status" key={toast.id}>
          <p>
            {toast.kind === 'back' && 'The service is back online.'}
            {toast.kind === 'down' && `${health.state === 'down' && health.cause === 'database' ? 'The database is unavailable.' : 'The service is unavailable.'} We keep checking and will tell you when it is back.`}
            {toast.kind === 'still' && 'Still unavailable. We will keep checking.'}
          </p>
          <div className="actions">
            {toast.kind !== 'back' && <button type="button" className="small" disabled={checking} onClick={() => void check(true)}>{checking ? 'Checking...' : 'Check now'}</button>}
            <button type="button" className="secondary small" onClick={() => setToast(null)}>{toast.kind === 'back' ? 'Dismiss' : 'Cancel'}</button>
          </div>
          <span className="timer" style={{ animationDuration: `${toast.kind === 'back' ? BACK_MS : TOAST_MS}ms` }} aria-hidden="true" />
        </div>
      )}
    </>
  )
}
