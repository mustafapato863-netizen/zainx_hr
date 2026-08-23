import { createRouter } from '@tanstack/react-router';
import { Route as rootRoute } from './__root';
import { indexRoute } from './index';

// The route tree
const routeTree = rootRoute.addChildren([indexRoute]);

export const router = createRouter({ routeTree });

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router;
  }
}
