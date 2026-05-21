import { Injectable } from "@angular/core";
import { environment } from "../../../environments/environment";

/**
 * Client-side AES-256-CBC encryption service using the Web Crypto API.
 * Passwords are encrypted HERE before being sent to the server.
 * The server never receives plaintext passwords.
 */
@Injectable({ providedIn: "root" })
export class EncryptionService {
  private keyPromise: Promise<CryptoKey> | null = null;

  private getKey(): Promise<CryptoKey> {
    if (!this.keyPromise) {
      const keyBytes = new TextEncoder().encode(
        environment.clientEncryptionKey.padEnd(32, "0").slice(0, 32),
      );
      this.keyPromise = crypto.subtle.importKey(
        "raw",
        keyBytes,
        { name: "AES-CBC" },
        false,
        ["encrypt", "decrypt"],
      );
    }
    return this.keyPromise;
  }

  /**
   * Encrypts plaintext using AES-256-CBC.
   * Returns { cipherText, iv } both Base64 encoded.
   */
  async encrypt(
    plainText: string,
  ): Promise<{ cipherText: string; iv: string }> {
    const key = await this.getKey();
    const iv = crypto.getRandomValues(new Uint8Array(16));
    const encoded = new TextEncoder().encode(plainText);

    const cipherBuf = await crypto.subtle.encrypt(
      { name: "AES-CBC", iv },
      key,
      encoded,
    );

    return {
      cipherText: this.bufToBase64(cipherBuf),
      iv: this.bufToBase64(iv.buffer as ArrayBuffer),
    };
  }

  /**
   * Decrypts AES-256-CBC ciphertext.
   * Both cipherText and iv must be Base64 encoded.
   */
  async decrypt(cipherText: string, iv: string): Promise<string> {
    const key = await this.getKey();
    const ivBuf = this.base64ToBuf(iv);
    const cipherBuf = this.base64ToBuf(cipherText);

    const plainBuf = await crypto.subtle.decrypt(
      { name: "AES-CBC", iv: ivBuf },
      key,
      cipherBuf,
    );
    return new TextDecoder().decode(plainBuf);
  }

  // ─── Helpers ─────────────────────────────────────────────────────────────

  private bufToBase64(buf: ArrayBuffer): string {
    return btoa(String.fromCharCode(...new Uint8Array(buf)));
  }

  private base64ToBuf(b64: string): Uint8Array {
    const binary = atob(b64);
    const buf = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) buf[i] = binary.charCodeAt(i);
    return buf;
  }
}
