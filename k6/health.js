import http from 'k6/http';
import { check, sleep } from 'k6';
import { BASE_URL, defaultThresholds } from './lib/config.js';

// Profiles: smoke (default), load, spike, soak.  Run: k6 run -e PROFILE=load k6/health.js
const profiles = {
  smoke: { vus: 1, duration: '10s' },
  load: {
    stages: [
      { duration: '30s', target: 20 },
      { duration: '1m', target: 20 },
      { duration: '15s', target: 0 },
    ],
  },
  spike: {
    stages: [
      { duration: '10s', target: 5 },
      { duration: '10s', target: 100 },
      { duration: '30s', target: 100 },
      { duration: '10s', target: 5 },
      { duration: '20s', target: 0 },
    ],
  },
  soak: { vus: 10, duration: '10m' },
};

const profile = __ENV.PROFILE || 'smoke';
if (!profiles[profile]) {
  throw new Error(`Unknown PROFILE '${profile}'. Use: ${Object.keys(profiles).join(', ')}`);
}

export const options = { ...profiles[profile], thresholds: defaultThresholds };

export default function () {
  const res = http.get(`${BASE_URL}/api/health`);
  check(res, {
    'health: status 200': (r) => r.status === 200,
    'health: body Healthy': (r) => r.json('status') === 'Healthy',
  });
  sleep(1);
}
