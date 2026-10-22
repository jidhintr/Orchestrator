import { Component, type ReactNode } from 'react'

const copy = {
  offline: { code: 'Offline', title: "We can't reach the service", text: 'Check your connection and try again. Nothing you saved is lost.' },
  server: { code: 'Error', title: 'Something went wrong on our side', text: 'This is not caused by anything you did. Try again in a moment.' },
  crash: { code: 'Error', title: 'This page stopped working', text: 'Reload to pick up where you left off.' },
} as const

export type ErrorKind = keyof typeof copy

export function ErrorPage({ kind, reference, onRetry }: { kind: ErrorKind; reference?: string; onRetry: () => void }) {
  const c = copy[kind]
  return (
    <main className="error-page" role="alert">
      <p className="misprint" aria-hidden="true"><span className="plate c">{c.code}</span><span className="plate m">{c.code}</span><span className="plate k">{c.code}</span></p>
      <h1>{c.title}</h1>
      <p>{c.text}</p>
      <div className="actions"><button onClick={onRetry}>Try again</button></div>
      {reference && <p className="reference">If it keeps happening, tell support this reference: <code>{reference}</code></p>}
    </main>
  )
}

export class ErrorBoundary extends Component<{ children: ReactNode }, { failed: boolean }> {
  state = { failed: false }
  static getDerivedStateFromError() {
    return { failed: true }
  }
  componentDidCatch(error: unknown) {
    console.error(error)
  }
  render() {
    return this.state.failed ? <ErrorPage kind="crash" onRetry={() => location.reload()} /> : this.props.children
  }
}
