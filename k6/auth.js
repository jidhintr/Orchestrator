import http from 'k6/http';
import { check, sleep } from 'k6';
import { client, login } from './lib/auth.js';
import { BASE_URL, buildOptions } from './lib/config.js';

// Sign in is rate limited to 10 per minute per address, so keep this to the smoke profile.
export const options = buildOptions();

http.setResponseCallback(http.expectedStatuses({ min: 200, max: 299 }, 400, 401));

const API = `${BASE_URL}/api/v1`;
const json = { headers: { 'Content-Type': 'application/json' } };

export function setup() {
  return login();
}

export default function (token) {
  const api = client(token);

  check(api.get(`${API}/roles`), { 'token works: 200': (r) => r.status === 200 });
  check(http.get(`${API}/roles`), { 'no token: 401': (r) => r.status === 401 });
  check(http.post(`${API}/auth/login`, JSON.stringify({ login: 'nobody@example.test', password: 'whatever-password' }), json), { 'unknown login: 401': (r) => r.status === 401 });
  check(http.post(`${API}/auth/login`, JSON.stringify({ login: '', password: '' }), json), { 'invalid body: 400': (r) => r.status === 400 });
  check(http.post(`${API}/auth/refresh`, null, json), { 'refresh without cookie: 401': (r) => r.status === 401 });
  check(http.get(`${BASE_URL}/api/health/live`), { 'live: 200': (r) => r.status === 200 });

  sleep(7);
}
