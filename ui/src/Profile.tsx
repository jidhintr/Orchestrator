import { useAccess } from './access'
import { type FormEvent, useEffect, useState } from 'react'
import { api, currentUserId, type User } from './api'
import { Avatar, Chips, message, Page, StatusBadges, useRunner, Form, Submit } from './ui'

export function Profile() {
  const [user, setUser] = useState<User | null>(null)
  const { run, feedback, error, setError } = useRunner()
  const id = currentUserId() ?? 0
  const canEditSelf = useAccess().rank === 4

  useEffect(() => {
    api.user(id).then(setUser).catch((e) => setError(message(e)))
  }, [id, setError])

  const save = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const d = new FormData(e.currentTarget)
    return run(async () => {
      setUser(await api.updateUser(id, {
        firstName: String(d.get('firstName')).trim(),
        lastName: String(d.get('lastName')).trim(),
        email: String(d.get('email')).trim() || null,
        mobile: String(d.get('mobile')).trim(),
        isActive: user?.isActive ?? true,
      }))
    }, 'Profile saved.')
  }

  if (!user) return <Page title="Profile" subtitle="Your details and access." action={null}>{error ? feedback : <p className="muted">Loading...</p>}</Page>

  const name = `${user.firstName} ${user.lastName}`
  return (
    <Page title="Profile" subtitle="Your details and access." action={null}>
      <section className="profile-head">
        <Avatar name={name} large />
        <div className="stack">
          <h2>{name}</h2>
          <span className="muted">{user.email ?? user.mobile}</span>
          <span><StatusBadges active={user.isActive} /></span>
        </div>
      </section>
      <div className="profile-grid">
        <fieldset className="pane" disabled={!canEditSelf}>
        <Form className="panel" onSubmit={save} requireChange key={JSON.stringify(user)}>
          <h2>Contact details</h2>
          <div className="row">
            <label>First name<input name="firstName" required minLength={2} maxLength={50} defaultValue={user.firstName} /></label>
            <label>Last name<input name="lastName" required minLength={2} maxLength={50} defaultValue={user.lastName} /></label>
          </div>
          <label>Email<input name="email" type="email" maxLength={254} defaultValue={user.email ?? ''} /></label>
          <label>Mobile<input name="mobile" type="tel" required minLength={5} maxLength={20} pattern="\+?[0-9]+" title="Digits, optional leading +" defaultValue={user.mobile} /></label>
          {feedback}
          <div className="actions"><Submit>Save changes</Submit></div>
        </Form>
          {!canEditSelf && <p className="muted">Your contact details are changed by an admin.</p>}
        </fieldset>
        <PasswordForm />
        <section className="panel">
          <h2>Access</h2>
          <div className="stack"><span className="muted">Departments</span><Chips items={user.departments.map((d) => d.name)} empty="None" /></div>
          <div className="stack"><span className="muted">Roles from departments</span><Chips items={user.departmentRoles} empty="None" /></div>
          <div className="stack"><span className="muted">Extra roles</span><Chips items={user.roles} empty="None" /></div>
          <p className="muted">Departments and roles are managed by an admin on the Users page.</p>
        </section>
      </div>
    </Page>
  )
}

function PasswordForm() {
  const { run, feedback } = useRunner()

  const submit = (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault()
    const d = new FormData(e.currentTarget)
    return run(async () => {
      if (d.get('next') !== d.get('confirm')) throw new Error('The new passwords do not match.')
      await api.changePassword(String(d.get('current')), String(d.get('next')))
    })
  }

  return (
    <Form className="panel" onSubmit={submit}>
      <h2>Change password</h2>
      <label>Current password<input name="current" type="password" required maxLength={128} autoComplete="current-password" /></label>
      <label>New password<input name="next" type="password" required minLength={10} maxLength={128} autoComplete="new-password" /></label>
      <label>Repeat new password<input name="confirm" type="password" required minLength={10} maxLength={128} autoComplete="new-password" /></label>
      <p className="muted">At least 10 characters. You will be signed out and sign in again with the new one.</p>
      {feedback}
      <div className="actions"><Submit>Change password</Submit></div>
    </Form>
  )
}
