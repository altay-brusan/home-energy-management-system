

import type { AppError } from '../../lib/http/error';

const DEMO_EMAIL = 'demo@hems.app';
const DEMO_PASSWORD = '123456';

const delay = (ms: number) => new Promise<void>((r) => setTimeout(r, ms));

export type LoginSuccess = {
  ok: true;
  data: {
    user: { id: string; email: string; name?: string; token?: string };
    token: string;
  };
};

export type LoginFail = {
  ok: false;
  error: string;
};

export type LoginResponse = LoginSuccess | LoginFail;

async function login(email: string, password: string): Promise<LoginResponse> {
  await delay(600);

  if (!email || !password) {
    const err: AppError = {
      code: 'VALIDATION',
      message: 'E-posta ve şifre zorunludur.',
      status: 422,
    };
    throw err;
  }

  if (email !== DEMO_EMAIL || password !== DEMO_PASSWORD) {
    const err: AppError = {
      code: 'UNAUTHORIZED',
      message: 'E-posta veya şifre hatalı.',
      status: 401,
    };
    throw err;
  }

  return {
    ok: true,
    data: {
      user: { id: '1', email, name: 'HEMS User', token: 'mock-token' },
      token: 'mock-token',
    },
  };
}

async function logout(): Promise<void> {
  await delay(200);
}

export const authService = { login, logout };
