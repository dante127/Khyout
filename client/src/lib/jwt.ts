export type UserRole = 'Buyer' | 'Supplier' | 'Admin';

export interface JwtClaims {
  userId: string;
  name: string;
  role: UserRole;
  companyId: string | null;
}

function base64UrlDecode(input: string): string {
  const normalized = input.replace(/-/g, '+').replace(/_/g, '/');
  const padded = normalized + '='.repeat((4 - (normalized.length % 4)) % 4);
  const binary = atob(padded);
  const bytes = Uint8Array.from(binary, (character) => character.charCodeAt(0));
  return new TextDecoder().decode(bytes);
}

/** Decodes the JWT payload (no signature verification — server remains the authority). */
export function decodeJwtClaims(token: string): JwtClaims | null {
  try {
    const parts = token.split('.');
    if (parts.length < 2 || !parts[1]) return null;

    const parsed = JSON.parse(base64UrlDecode(parts[1])) as Record<string, unknown>;
    if (typeof parsed.sub !== 'string') return null;

    const role: UserRole =
      parsed.role === 'Supplier' || parsed.role === 'Admin' ? parsed.role : 'Buyer';

    return {
      userId: parsed.sub,
      name: typeof parsed.name === 'string' ? parsed.name : '',
      role,
      companyId:
        typeof parsed.companyId === 'string' && parsed.companyId.length > 0
          ? parsed.companyId
          : null,
    };
  } catch {
    return null;
  }
}
