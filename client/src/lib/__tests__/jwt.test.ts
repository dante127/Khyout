import { describe, expect, it } from 'vitest';
import { decodeJwtClaims } from '../jwt';

function base64Url(input: object): string {
  const bytes = new TextEncoder().encode(JSON.stringify(input));
  let binary = '';
  for (const byte of bytes) binary += String.fromCharCode(byte);
  return btoa(binary).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function makeToken(payload: Record<string, unknown>): string {
  return `${base64Url({ alg: 'HS256', typ: 'JWT' })}.${base64Url(payload)}.signature`;
}

describe('decodeJwtClaims', () => {
  it('decodes the user claims including an Arabic name', () => {
    const token = makeToken({ sub: 'u-1', name: 'أحمد الشامي', role: 'Supplier', companyId: 'c-9' });
    expect(decodeJwtClaims(token)).toEqual({
      userId: 'u-1',
      name: 'أحمد الشامي',
      role: 'Supplier',
      companyId: 'c-9',
    });
  });

  it('treats an empty companyId claim as null', () => {
    const token = makeToken({ sub: 'u-1', name: 'x', role: 'Buyer', companyId: '' });
    expect(decodeJwtClaims(token)?.companyId).toBeNull();
  });

  it('defaults unknown roles to Buyer', () => {
    const token = makeToken({ sub: 'u-1', name: 'x', role: 'Wizard', companyId: 'c-1' });
    expect(decodeJwtClaims(token)?.role).toBe('Buyer');
  });

  it('returns null for malformed tokens', () => {
    expect(decodeJwtClaims('')).toBeNull();
    expect(decodeJwtClaims('garbage')).toBeNull();
    expect(decodeJwtClaims('a.b')).toBeNull();
    expect(decodeJwtClaims('a.%%%.c')).toBeNull();
  });

  it('returns null when the payload has no subject', () => {
    const token = makeToken({ name: 'no-sub' });
    expect(decodeJwtClaims(token)).toBeNull();
  });
});
