import { createRoute } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';
import { Route as rootRoute } from './__root';

function Index() {
  const { t } = useTranslation();
  return (
    <div className="p-2">
      <h3 className="text-2xl font-bold mb-4">{t('welcome')}</h3>
      <p className="text-slate-600">
        This is the Phase 1A Platform Kernel shell. Business modules will be loaded here.
      </p>
    </div>
  );
}

export const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  component: Index,
});
