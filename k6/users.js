import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, buildOptions } from './lib/config.js';

// Each iteration creates one uniquely addressed user, so load/spike/soak runs leave rows behind in the target database.
export const options = buildOptions();

// The intentional 400/404/409 checks below are expected responses, not failures.
http.setResponseCallback(http.expectedStatuses({ min: 200, max: 299 }, 400, 404, 409));

const API = `${BASE_URL}/api/v1/users`;
const json = { headers: { 'Content-Type': 'application/json' } };

export default function () {
  const suffix = `${__VU}x${__ITER}x${Date.now().toString(36)}`;
  const body = { firstName: 'K6', lastName: `U${suffix}`.slice(0, 50), email: `k6.${suffix}@example.com`, mobile: '9876543210', departmentId: 4, roles: ['Employee'] };

  const list = http.get(API);
  check(list, {
    'list: 200': (r) => r.status === 200,
    'list: has seeded users': (r) => r.json().length >= 2,
  });

  const created = http.post(API, JSON.stringify(body), json);
  if (!check(created, { 'create: 201': (r) => r.status === 201 })) {
    return;
  }
  const id = created.json('id');

  check(http.get(`${API}/${id}`), { 'get: 200': (r) => r.status === 200 });
  check(http.put(`${API}/${id}`, JSON.stringify({ ...body, roles: ['Manager'], isActive: false }), json), {
    'update: 200': (r) => r.status === 200,
  });
  check(http.post(API, JSON.stringify(body), json), { 'duplicate email: 409': (r) => r.status === 409 });
  check(http.post(API, JSON.stringify({ ...body, email: `r.${body.email}`, roles: ['Wizard'] }), json), {
    'unknown role: 400': (r) => r.status === 400,
  });
  check(http.get(`${API}/999999`), { 'unknown id: 404': (r) => r.status === 404 });

  sleep(1);
}
