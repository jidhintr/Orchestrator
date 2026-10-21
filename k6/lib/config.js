export const BASE_URL = __ENV.BASE_URL || 'http://localhost:5176';

// Shared pass/fail gates. Tighten once real SLOs are defined (see docs/testing/test-strategy.md).
export const defaultThresholds = {
  http_req_failed: ['rate<0.01'],
  http_req_duration: ['p(95)<500'],
  checks: ['rate>0.99'],
};
