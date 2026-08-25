import React, { Suspense, lazy } from 'react';
import { createRoute } from '@tanstack/react-router';
import { Route as rootRoute } from './__root';

const AiWorkspace = lazy(() =>
  import('@zainx/ai').then((m) => ({ default: m.AiWorkspace }))
);

export const aiRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/ai',
  component: AiPage,
});

function AiPage() {
  return (
    <Suspense
      fallback={
        <div className="mx-auto w-full max-w-[1440px] rounded-lg border border-border-default bg-surface p-8 text-sm text-text-secondary">
          Loading Workforce AI…
        </div>
      }
    >
      <AiWorkspace />
    </Suspense>
  );
}
