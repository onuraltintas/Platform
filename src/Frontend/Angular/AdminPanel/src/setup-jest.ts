import 'zone.js';
import 'zone.js/testing';
import { getTestBed } from '@angular/core/testing';
import {
  BrowserDynamicTestingModule,
  platformBrowserDynamicTesting,
} from '@angular/platform-browser-dynamic/testing';

// Initialize Angular testing environment
getTestBed().initTestEnvironment(
  BrowserDynamicTestingModule,
  platformBrowserDynamicTesting()
);

// Add TextEncoder/TextDecoder polyfills for Node.js environment
import { TextEncoder, TextDecoder } from 'util';
if (typeof global.TextEncoder === 'undefined') {
  global.TextEncoder = TextEncoder;
  global.TextDecoder = TextDecoder;
}

// Jasmine globals shim for Jest
// Some legacy specs use jasmine.* helpers
// eslint-disable-next-line @typescript-eslint/no-explicit-any
(global as any).jasmine = {
  any: (type: any) => expect.any(type),
  objectContaining: (obj: any) => expect.objectContaining(obj),
  createSpyObj: (baseName: string, methodNames: string[]) => {
    const obj: Record<string, any> = {};
    for (const m of methodNames) {
      const spy = jest.fn();
      // Provide jasmine-like chainable API
      (spy as any).and = {
        returnValue: (val: any) => { spy.mockReturnValue(val); },
        callFake: (fn: (...args: any[]) => any) => { spy.mockImplementation(fn); },
        resolveTo: (val: any) => { spy.mockResolvedValue(val); },
        rejectWith: (err: any) => { spy.mockRejectedValue(err); }
      };
      obj[m] = spy;
    }
    return obj;
  }
};

// Spy helpers shim
// eslint-disable-next-line @typescript-eslint/no-explicit-any
(global as any).spyOn = (obj: any, method: string) => jest.spyOn(obj, method as any);

// expectAsync shim
// eslint-disable-next-line @typescript-eslint/no-explicit-any
(global as any).expectAsync = (promise: Promise<any>) => ({
  toBeResolved: () => expect(promise).resolves.not.toThrow(),
  toBeRejected: () => expect(promise).rejects.toBeDefined()
});

// Web Crypto API polyfill for tests
if (!(global as any).crypto) {
  // minimal polyfill to avoid runtime errors in specs that instantiate services
  (global as any).crypto = {
    subtle: {
      generateKey: jest.fn().mockResolvedValue({}),
      importKey: jest.fn().mockResolvedValue({}),
      exportKey: jest.fn().mockResolvedValue(new ArrayBuffer(0)),
      encrypt: jest.fn().mockResolvedValue(new ArrayBuffer(0)),
      decrypt: jest.fn().mockResolvedValue(new ArrayBuffer(0)),
      deriveKey: jest.fn().mockResolvedValue({}),
      deriveBits: jest.fn().mockResolvedValue(new ArrayBuffer(0))
    },
    getRandomValues: (arr: Uint8Array) => arr.fill(4) // deterministic
  } as unknown as Crypto;
}

// Ensure window.crypto references the same polyfill
if (!(window as any).crypto && (global as any).crypto) {
  Object.defineProperty(window, 'crypto', { value: (global as any).crypto });
}

// Spy some globals used in specs when not explicitly mocked
if (!jest.isMockFunction(console.warn)) {
  jest.spyOn(console, 'warn').mockImplementation(() => {});
}
if (!jest.isMockFunction(console.error)) {
  jest.spyOn(console, 'error').mockImplementation(() => {});
}

// Some specs assert createElement calls
if (!jest.isMockFunction(document.createElement)) {
  jest.spyOn(document, 'createElement');
}

// Mock localStorage
const localStorageMock = {
  getItem: jest.fn(),
  setItem: jest.fn(),
  removeItem: jest.fn(),
  clear: jest.fn(),
  length: 0,
  key: jest.fn(),
};
Object.defineProperty(window, 'localStorage', {
  value: localStorageMock
});

// Mock sessionStorage
const sessionStorageMock = {
  getItem: jest.fn(),
  setItem: jest.fn(),
  removeItem: jest.fn(),
  clear: jest.fn(),
  length: 0,
  key: jest.fn(),
};
Object.defineProperty(window, 'sessionStorage', {
  value: sessionStorageMock
});

// Mock window.matchMedia
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: jest.fn().mockImplementation(query => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: jest.fn(),
    removeListener: jest.fn(),
    addEventListener: jest.fn(),
    removeEventListener: jest.fn(),
    dispatchEvent: jest.fn(),
  })),
});

// Mock IntersectionObserver
global.IntersectionObserver = class IntersectionObserver {
  constructor() {}
  observe() { return null; }
  unobserve() { return null; }
  disconnect() { return null; }
  takeRecords() { return []; }
  root = null;
  rootMargin = '';
  thresholds = [];
};

// Mock ResizeObserver
global.ResizeObserver = class ResizeObserver {
  constructor() {}
  observe() { return null; }
  unobserve() { return null; }
  disconnect() { return null; }
};

// Mock URL.createObjectURL and revokeObjectURL for file download tests
Object.defineProperty(window.URL, 'createObjectURL', {
  value: jest.fn(() => 'mocked-url'),
});
Object.defineProperty(window.URL, 'revokeObjectURL', {
  value: jest.fn(),
});

// Mock Blob constructor
global.Blob = jest.fn().mockImplementation((content, options) => ({
  content,
  options,
  size: content ? content.join('').length : 0,
  type: options?.type || '',
}));

// Add custom matchers
expect.extend({
  toHaveBeenCalledWithMatch(received: jest.Mock, ...args: unknown[]) {
    const calls = received.mock.calls;
    const pass = calls.some(call =>
      args.every((arg, index) => {
        if (typeof arg === 'function') {
          return arg(call[index]);
        }
        return call[index] === arg;
      })
    );

    return {
      pass,
      message: () =>
        pass
          ? `Expected not to have been called with matching arguments`
          : `Expected to have been called with matching arguments`
    };
  }
});

// Clean up after each test
afterEach(() => {
  jest.clearAllMocks();
  // Clear localStorage and sessionStorage mocks
  localStorageMock.getItem.mockClear();
  localStorageMock.setItem.mockClear();
  localStorageMock.removeItem.mockClear();
  localStorageMock.clear.mockClear();
  sessionStorageMock.getItem.mockClear();
  sessionStorageMock.setItem.mockClear();
  sessionStorageMock.removeItem.mockClear();
  sessionStorageMock.clear.mockClear();
});