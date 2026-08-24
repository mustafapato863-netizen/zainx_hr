import React from 'react';
import { createRoute } from '@tanstack/react-router';
import { Route as rootRoute } from './__root';
import { AdministrationWorkspace } from '@zainx/administration';

export const administrationRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/administration',
  component: AdministrationPage,
});

function AdministrationPage() {
  return <AdministrationWorkspace />;
}
