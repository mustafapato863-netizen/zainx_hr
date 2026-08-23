import { RouterProvider as TanStackRouterProvider } from '@tanstack/react-router';
import { router } from '../routes/router';

export function RouterProvider() {
  return <TanStackRouterProvider router={router} />;
}
