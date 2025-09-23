import { Injectable } from '@angular/core';
import { EncryptionResult } from '../interfaces/storage-security.interface';

/**
 * Enterprise-grade encryption service using Web Crypto API
 * Implements AES-GCM encryption for maximum security
 */
@Injectable({
  providedIn: 'root'
})
export class EncryptionService {
  private readonly algorithm = 'AES-GCM';
  private readonly keyLength = 256;
  private readonly ivLength = 12; // 96 bits for GCM
  private readonly tagLength = 128; // 128 bits authentication tag

  private masterKey: CryptoKey | null = null;
  private keyGenerated = false;

  constructor() {
    this.initializeMasterKey();
  }

  /**
   * Initialize or retrieve master encryption key
   */
  private async initializeMasterKey(): Promise<void> {
    try {
      // Try to derive key from browser fingerprinting + session data
      const keyMaterial = await this.generateKeyMaterial();
      this.masterKey = await window.crypto.subtle.importKey(
        'raw',
        keyMaterial,
        { name: this.algorithm },
        false, // not extractable
        ['encrypt', 'decrypt']
      );
      this.keyGenerated = true;
    } catch (error) {
      console.error('Failed to initialize master key:', error);
      // Fallback to session-based key
      await this.generateSessionKey();
    }
  }

  /**
   * Generate key material from browser characteristics and session data
   */
  private async generateKeyMaterial(): Promise<ArrayBuffer> {
    const encoder = new TextEncoder();

    // Collect browser fingerprinting data (non-sensitive)
    const fingerprint = [
      navigator.userAgent,
      navigator.language,
      screen.width + 'x' + screen.height,
      new Date().getTimezoneOffset().toString(),
      sessionStorage.length.toString(),
      Math.random().toString(36) // Session-specific entropy
    ].join('|');

    // Hash the fingerprint to create key material
    const fingerprintBuffer = encoder.encode(fingerprint);
    const hashBuffer = await window.crypto.subtle.digest('SHA-256', fingerprintBuffer);

    return hashBuffer;
  }

  /**
   * Generate a session-only encryption key
   */
  private async generateSessionKey(): Promise<void> {
    this.masterKey = await window.crypto.subtle.generateKey(
      {
        name: this.algorithm,
        length: this.keyLength
      },
      false, // not extractable
      ['encrypt', 'decrypt']
    );
    this.keyGenerated = true;
  }

  /**
   * Encrypt data using AES-GCM
   */
  async encrypt(data: string): Promise<EncryptionResult> {
    await this.ensureKeyReady();

    if (!this.masterKey) {
      throw new Error('Encryption key not available');
    }

    const encoder = new TextEncoder();
    const dataBuffer = encoder.encode(data);

    // Generate random IV
    const iv = window.crypto.getRandomValues(new Uint8Array(this.ivLength));

    try {
      const encryptedBuffer = await window.crypto.subtle.encrypt(
        {
          name: this.algorithm,
          iv: iv,
          tagLength: this.tagLength
        },
        this.masterKey,
        dataBuffer
      );

      // Split encrypted data and authentication tag
      const encryptedArray = new Uint8Array(encryptedBuffer);
      const tagStart = encryptedArray.length - (this.tagLength / 8);

      const encryptedData = encryptedArray.slice(0, tagStart);
      const tag = encryptedArray.slice(tagStart);

      return {
        data: this.arrayBufferToBase64(encryptedData),
        iv: this.arrayBufferToBase64(iv),
        tag: this.arrayBufferToBase64(tag)
      };
    } catch (error: any) {
      throw new Error(`Encryption failed: ${error?.message || 'Unknown error'}`);
    }
  }

  /**
   * Decrypt data using AES-GCM
   */
  async decrypt(encryptionResult: EncryptionResult): Promise<string> {
    await this.ensureKeyReady();

    if (!this.masterKey) {
      throw new Error('Decryption key not available');
    }

    try {
      const encryptedData = this.base64ToArrayBuffer(encryptionResult.data);
      const iv = this.base64ToArrayBuffer(encryptionResult.iv);
      const tag = this.base64ToArrayBuffer(encryptionResult.tag);

      // Combine encrypted data with authentication tag
      const combinedBuffer = new Uint8Array(encryptedData.byteLength + tag.byteLength);
      combinedBuffer.set(new Uint8Array(encryptedData), 0);
      combinedBuffer.set(new Uint8Array(tag), encryptedData.byteLength);

      const decryptedBuffer = await window.crypto.subtle.decrypt(
        {
          name: this.algorithm,
          iv: iv,
          tagLength: this.tagLength
        },
        this.masterKey,
        combinedBuffer
      );

      const decoder = new TextDecoder();
      return decoder.decode(decryptedBuffer);
    } catch (error: any) {
      throw new Error(`Decryption failed: ${error?.message || 'Unknown error'}`);
    }
  }

  /**
   * Generate integrity hash for data validation
   */
  async generateIntegrityHash(data: string): Promise<string> {
    const encoder = new TextEncoder();
    const dataBuffer = encoder.encode(data);
    const hashBuffer = await window.crypto.subtle.digest('SHA-256', dataBuffer);
    return this.arrayBufferToBase64(hashBuffer);
  }

  /**
   * Verify data integrity
   */
  async verifyIntegrity(data: string, expectedHash: string): Promise<boolean> {
    try {
      const actualHash = await this.generateIntegrityHash(data);
      return actualHash === expectedHash;
    } catch (error) {
      console.error('Integrity verification failed:', error);
      return false;
    }
  }

  /**
   * Check if encryption is available and ready
   */
  isReady(): boolean {
    return this.keyGenerated && this.masterKey !== null;
  }

  /**
   * Reset encryption service (for security cleanup)
   */
  async reset(): Promise<void> {
    this.masterKey = null;
    this.keyGenerated = false;
    await this.initializeMasterKey();
  }

  /**
   * Ensure encryption key is ready
   */
  private async ensureKeyReady(): Promise<void> {
    if (!this.keyGenerated || !this.masterKey) {
      await this.initializeMasterKey();
    }

    if (!this.masterKey) {
      throw new Error('Unable to initialize encryption key');
    }
  }

  /**
   * Convert ArrayBuffer to base64 string
   */
  private arrayBufferToBase64(buffer: ArrayBuffer | Uint8Array): string {
    const bytes = new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.byteLength; i++) {
      binary += String.fromCharCode(bytes[i]);
    }
    return btoa(binary);
  }

  /**
   * Convert base64 string to ArrayBuffer
   */
  private base64ToArrayBuffer(base64: string): ArrayBuffer {
    const binary = atob(base64);
    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
      bytes[i] = binary.charCodeAt(i);
    }
    return bytes.buffer;
  }

  /**
   * Generate secure random string for additional entropy
   */
  generateSecureRandom(length: number = 32): string {
    const array = new Uint8Array(length);
    window.crypto.getRandomValues(array);
    return Array.from(array, byte => byte.toString(16).padStart(2, '0')).join('');
  }

  /**
   * Check if Web Crypto API is available
   */
  static isSupported(): boolean {
    return typeof window !== 'undefined' &&
           'crypto' in window &&
           'subtle' in window.crypto;
  }
}