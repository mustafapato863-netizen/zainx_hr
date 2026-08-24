import { createRouter } from '@tanstack/react-router';
import { Route as rootRoute } from './__root';
import { indexRoute } from './index';
import { peopleRoute } from './people';
import { attendanceRoute } from './attendance';
import { leaveRoute } from './leave';
import { approvalsRoute } from './approvals';
import { payrollRoute } from './payroll';

// The complete route tree
const routeTree = rootRoute.addChildren([
  indexRoute,
  peopleRoute,
  attendanceRoute,
  leaveRoute,
  approvalsRoute,
  payrollRoute
]);

export const router = createRouter({ routeTree });

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router;
  }
}
