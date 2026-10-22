import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, buildOptions } from './lib/config.js';

export const options = buildOptions();

export default function () {
  const res = http.get(`${BASE_URL}/api/health`);
  check(res, {
    'health: status 200': (r) => r.status === 200,
    'health: body Healthy': (r) => r.body === 'Healthy',
  });
  sleep(1);
}
