import axios from 'axios';

const BASE_URL = 'https://localhost:7217/api';
const TOKEN_KEY = 'sjt_token';
const USER_KEY  = 'sjt_user';

const authApi = axios.create({ baseURL: BASE_URL });

// ── Storage helpers ───────────────────────────────────────────────────────────

export const getToken = () => localStorage.getItem(TOKEN_KEY);

export const getUser = () => {
  try {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? JSON.parse(raw) : null;
  } catch {
    return null;
  }
};

export const isAuthenticated = () => {
  const token = getToken();
  if (!token) return false;
  try {
    // Decode payload without a library — just check expiry
    const payload = JSON.parse(atob(token.split('.')[1]));
    return payload.exp * 1000 > Date.now();
  } catch {
    return false;
  }
};

const saveSession = (response) => {
  localStorage.setItem(TOKEN_KEY, response.token);
  localStorage.setItem(USER_KEY, JSON.stringify({
    userId:   response.userId,
    fullName: response.fullName,
    email:    response.email,
    expiresAt: response.expiresAt,
  }));
};

export const clearSession = () => {
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(USER_KEY);
};

// ── API calls ─────────────────────────────────────────────────────────────────

export const register = async ({ fullName, email, password }) => {
  const { data } = await authApi.post('/auth/register', { fullName, email, password });
  saveSession(data);
  return data;
};

export const login = async ({ email, password }) => {
  const { data } = await authApi.post('/auth/login', { email, password });
  saveSession(data);
  return data;
};

export const logout = () => {
  clearSession();
  window.location.href = '/login';
};
