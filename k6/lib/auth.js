import http from 'k6/http';
import { BASE_URL } from './config.js';

export function login() {
  const { K6_LOGIN: user, K6_PASSWORD: password } = __ENV;
  if (!user || !password) {
    throw new Error('Set K6_LOGIN and K6_PASSWORD to the email or mobile and password of an existing user.');
  }
  const res = http.post(`${BASE_URL}/api/v1/auth/login`, JSON.stringify({ login: user, password }), { headers: { 'Content-Type': 'application/json' } });
  if (res.status !== 200) {
    throw new Error(`Sign in failed with status ${res.status}.`);
  }
  return res.json('accessToken');
}

const withToken = (token, params = {}) => ({ ...params, headers: { ...params.headers, Authorization: `Bearer ${token}` } });

export function client(token) {
  return {
    get: (url, params) => http.get(url, withToken(token, params)),
    post: (url, body, params) => http.post(url, body, withToken(token, params)),
    put: (url, body, params) => http.put(url, body, withToken(token, params)),
    del: (url, body, params) => http.del(url, body, withToken(token, params)),
  };
}
