// Phase 1A: Safe Browser Storage Policy Implementation

// List of allowed keys that can be persisted in local storage
const ALLOWED_STORAGE_KEYS = [
  'zainx_theme',
  'zainx_sidebar_state',
  'zainx_language_preference'
] as const;

type AllowedKey = typeof ALLOWED_STORAGE_KEYS[number];

export const SafeStorage = {
  get: (key: AllowedKey): string | null => {
    if (!ALLOWED_STORAGE_KEYS.includes(key)) {
      console.error(`Security Warning: Attempted to read unapproved storage key: ${key}`);
      return null;
    }
    return localStorage.getItem(key);
  },
  
  set: (key: AllowedKey, value: string): void => {
    if (!ALLOWED_STORAGE_KEYS.includes(key)) {
      console.error(`Security Violation: Attempted to persist unapproved storage key: ${key}`);
      return;
    }
    localStorage.setItem(key, value);
  },
  
  remove: (key: AllowedKey): void => {
    if (!ALLOWED_STORAGE_KEYS.includes(key)) return;
    localStorage.removeItem(key);
  }
};
