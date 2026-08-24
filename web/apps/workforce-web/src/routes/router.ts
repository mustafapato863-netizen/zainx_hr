import { createRouter } from '@tanstack/react-router';
import { Route as rootRoute } from './__root';
import { indexRoute } from './index';
import { peopleRoute } from './people';

// The route tree
const routeTree = rootRoute.addChildren([indexRoute, peopleRoute]);

export const router = createRouter({ routeTree });

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router;
  }
}
