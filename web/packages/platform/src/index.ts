export const PLATFORM_VERSION = '1.0.0';
export interface SessionContext {
  tenantId: string;
  userId: string;
  roles: string[];
}

export * from './components/NotificationCenter';
