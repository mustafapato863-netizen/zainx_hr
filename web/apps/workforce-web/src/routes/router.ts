import { createRouter } from '@tanstack/react-router';
import { Route as rootRoute } from './__root';
import { indexRoute } from './index';
import { peopleRoute } from './people';
import { organizationRoute } from './organization';
import { attendanceRoute } from './attendance';
import { leaveRoute } from './leave';
import { approvalsRoute } from './approvals';
import { payrollRoute } from './payroll';
import { recruitmentRoute } from './recruitment';
import { reportsRoute } from './reports';
import { administrationRoute } from './administration';
import { aiRoute } from './ai';
import { meRoute } from './me';

// The complete route tree
const routeTree = rootRoute.addChildren([
  indexRoute,
  peopleRoute,
  organizationRoute,
  attendanceRoute,
  leaveRoute,
  approvalsRoute,
  payrollRoute,
  recruitmentRoute,
  reportsRoute,
  administrationRoute,
  aiRoute,
  meRoute,
]);

export const router = createRouter({ routeTree });

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router;
  }
}
