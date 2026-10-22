export const BASE_URL = __ENV.BASE_URL || 'http://localhost:5176';

// Shared pass/fail gates. Provisional: no SLOs are defined yet.
const thresholds = {
  http_req_failed: ['rate<0.01'],
  http_req_duration: ['p(95)<500'],
  checks: ['rate>0.99'],
};

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

export function buildOptions() {
  const name = __ENV.PROFILE || 'smoke';
  if (!profiles[name]) {
    throw new Error(`Unknown PROFILE '${name}'. Use: ${Object.keys(profiles).join(', ')}`);
  }
  return { ...profiles[name], thresholds };
}
