export const environment = {
  production: false,
  apiUrl: "http://localhost:57508/api",
  // Client-side AES-256 encryption key (32 chars).
  // In production, derive this from the user's master password via PBKDF2.
  clientEncryptionKey: "CHANGE_ME_CLIENT_AES_KEY_32CHARS",
  tokenKey: "pv_access_token",
  refreshTokenKey: "pv_refresh_token",
  userKey: "pv_user",
  passwordRevealTimeout: 8000, // ms before password auto-hides
};
