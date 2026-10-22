import { type FormEvent, useState } from 'react'
import { signIn } from './api'
import { Form, Submit, useRunner } from './ui'

export function Login({ onDone }: { onDone: () => void }) {
  const { run, feedback } = useRunner()
  const [show, setShow] = useState(false)

  const submit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const d = new FormData(e.currentTarget)
    return run(async () => {
      await signIn(String(d.get('login')).trim(), String(d.get('password')))
      onDone()
    })
  }

  return (
    <div className="login">
      <div className="login-art" aria-hidden="true">
        <svg viewBox="0 0 240 240" fill="none">
          <circle className="ink-c" cx="100" cy="100" r="70" />
          <circle className="ink-m" cx="140" cy="100" r="70" />
          <circle className="ink-y" cx="120" cy="138" r="70" />
          <path className="reg" d="M120 4v232M4 120h232" />
          <circle className="reg" cx="120" cy="120" r="22" />
        </svg>
        <p className="wordmark">Orchestrator</p>
        <p>Every job, stage by stage.</p>
      </div>
      <div className="login-form">
        <Form onSubmit={submit}>
          <h1>Sign in</h1>
          <label>Email or mobile<input name="login" required minLength={3} maxLength={254} autoComplete="username" autoFocus /></label>
          <label>Password
            <span className="reveal">
              <input name="password" type={show ? 'text' : 'password'} required maxLength={128} autoComplete="current-password" />
              <button type="button" className="secondary small" aria-pressed={show} onClick={() => setShow((s) => !s)}>{show ? 'Hide' : 'Show'}</button>
            </span>
          </label>
          {feedback}
          <div className="actions"><Submit>Sign in</Submit></div>
        </Form>
      </div>
    </div>
  )
}
