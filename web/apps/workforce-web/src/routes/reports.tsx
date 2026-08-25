import React, { Suspense, lazy } from 'react';
import { createRoute } from '@tanstack/react-router';
import { Route as rootRoute } from './__root';

const ReportsWorkspace = lazy(() =>
  import('@zainx/reports').then((m) => ({ default: m.ReportsWorkspace }))
);

export const reportsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/reports',
  component: ReportsPage,
});

function ReportsPage() {
  return (
    <Suspense
      fallback={
        <div className="mx-auto w-full max-w-[1440px] rounded-lg border border-border-default bg-surface p-8 text-sm text-text-secondary">
          Loading reports & insights…
        </div>
      }
    >
      <ReportsWorkspace />
    </Suspense>
  );
}
