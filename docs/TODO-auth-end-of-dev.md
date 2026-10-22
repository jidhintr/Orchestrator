# Auth: do before leaving the dev phase

Dev-phase auth (ADR-0005) is password sign-in only, with one seeded Super Admin. Finish these before any real deployment.

- [ ] Remove the seeded Super Admin (user id 1) from `UserConfiguration` with a migration, and the Development-only `SeedDevSuperAdminAsync` in `Program.cs` plus `Auth:DevSeedPassword`.
- [ ] Bootstrap the first Super Admin properly (one-off command or deployment step, not a seed row or committed password).
- [ ] OTP for mobile users and a verified password-reset channel (SMS or WhatsApp: owner to decide, with a per-message budget).
- [ ] Require a second factor for Admin and Super Admin (they can change roles).
- [ ] Force a password change on first sign in after an admin sets or resets it.
- [ ] Rotate `Auth:JwtKey` without signing everyone out (key ids), keep it in a real secret store.
- [ ] Audit log of sign-ins, failures, lockouts and password changes; alert on repeated lockouts.
- [ ] Breached-password check; per-login rate limit in addition to the per-address limit.
- [ ] Normalise mobile numbers (spaces, +country code) so login and uniqueness match what people type.
- [ ] Scope reads: Client-role users see only their own orders; employees only their own data where required.
- [ ] Session list and "sign out everywhere" for users.
- [ ] Write the deferred auth tests: Gherkin scenarios, unit and integration tests for login, refresh rotation and reuse, logout, lockout, rate limit, change and reset password, deactivated user.
- [ ] HTTPS everywhere (HSTS is on outside Development), review cookie `Secure`/`SameSite` once the real host names are known.
