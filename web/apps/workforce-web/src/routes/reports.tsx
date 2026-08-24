import React from 'react';
import { createRoute } from '@tanstack/react-router';
import { Route as rootRoute } from './__root';
import { ReportsWorkspace } from '@zainx/reports';

export const reportsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/reports',
  component: ReportsPage,
});

function ReportsPage() {
  return <ReportsWorkspace />;
}
