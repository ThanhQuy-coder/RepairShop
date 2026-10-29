// utils/jwt.ts
export interface JwtPayload {
  sub?: string;
  email?: string;
  role?: string;
  exp?: number;
  // ASP.NET có thể dùng claim URI đầy đủ nếu MapInboundClaims = true
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'?: string;
}

export function decodeJwtPayload(token: string): JwtPayload | null {
  try {
    const payload = token.split('.')[1];
    const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
    const padding = '='.repeat((4 - (normalized.length % 4)) % 4);
    return JSON.parse(window.atob(normalized + padding));
  } catch {
    return null;
  }
}

export function getUserIdFromToken(token: string | null): string | null {
  if (!token) return null;
  const payload = decodeJwtPayload(token);
  return (
    payload?.sub ??
    payload?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] ??
    null
  );
}
