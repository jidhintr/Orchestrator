import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, buildOptions } from './lib/config.js';

// Each iteration creates one uniquely addressed client, so load/spike/soak runs leave rows behind in the target database.
export const options = buildOptions();

// The intentional 404 check below is an expected response, not a failure.
http.setResponseCallback(http.expectedStatuses({ min: 200, max: 299 }, 404));

const API = `${BASE_URL}/api/v1/clients`;
const json = { headers: { 'Content-Type': 'application/json' } };

export default function () {
  const suffix = `${__VU}x${__ITER}x${Date.now().toString(36)}`;
  const client = {
    companyName: `K6 ${suffix}`,
    contactName: 'K6 Contact',
    email: `k6${suffix}@example.com`,
    phone: '+15551234567',
    shippingAddress: '1 Main St',
  };

  check(http.get(API), { 'list: 200': (r) => r.status === 200 });

  const created = http.post(API, JSON.stringify(client), json);
  if (!check(created, { 'create: 201': (r) => r.status === 201 })) {
    return;
  }
  const id = created.json('id');

  check(http.get(`${API}/${id}`), { 'get: 200': (r) => r.status === 200 });
  check(http.put(`${API}/${id}`, JSON.stringify({ ...client, tier: 'Gold' }), json), {
    'update: 200': (r) => r.status === 200,
    'update: gold discount': (r) => r.json('tierDiscountPercent') === 5,
  });
  check(http.put(`${API}/${id}/status`, JSON.stringify({ status: 'Inactive' }), json), { 'status: 200': (r) => r.status === 200 });
  check(http.get(`${API}/999999`), { 'unknown id: 404': (r) => r.status === 404 });

  sleep(1);
}
