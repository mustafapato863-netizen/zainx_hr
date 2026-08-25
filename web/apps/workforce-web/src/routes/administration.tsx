import React, { Suspense, lazy } from 'react';
import { createRoute } from '@tanstack/react-router';
import { Route as rootRoute } from './__root';

const AdministrationWorkspace = lazy(() =>
  import('@zainx/administration').then((m) => ({ default: m.AdministrationWorkspace }))
);

export const administrationRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/administration',
  component: AdministrationPage,
});

function AdministrationPage() {
  return (
    <Suspense
      fallback={
        <div className="mx-auto w-full max-w-[1440px] rounded-lg border border-border-default bg-surface p-8 text-sm text-text-secondary">
          Loading administration & governance…
        </div>
      }
    >
      <AdministrationWorkspace />
    </Suspense>
  );
}
