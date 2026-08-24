import React from 'react';
import { createRootRoute, Outlet, Link } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';

export const Route = createRootRoute({
  component: RootComponent,
});

function RootComponent() {
  const { i18n } = useTranslation();

  return (
    <div className="flex h-screen w-full bg-slate-50 text-slate-900" dir={i18n.dir()}>
      <aside className="w-64 bg-slate-900 text-white flex-shrink-0 p-4">
        <h1 className="font-bold text-xl mb-8">ZainX Platform</h1>
        <nav className="flex flex-col gap-1.5">
          <Link 
            to="/" 
            className="py-2 px-3 hover:bg-slate-800 rounded cursor-pointer transition-colors block text-slate-200 hover:text-white"
          >
            Dashboard
          </Link>
          <Link 
            to="/people" 
            data-testid="nav-people-link"
            className="py-2 px-3 hover:bg-slate-800 rounded cursor-pointer transition-colors block text-slate-200 hover:text-white"
          >
            People
          </Link>
          <Link 
            to="/attendance" 
            data-testid="nav-attendance-link"
            className="py-2 px-3 hover:bg-slate-800 rounded cursor-pointer transition-colors block text-slate-200 hover:text-white"
          >
            Attendance
          </Link>
          <Link 
            to="/leave" 
            data-testid="nav-leave-link"
            className="py-2 px-3 hover:bg-slate-800 rounded cursor-pointer transition-colors block text-slate-200 hover:text-white"
          >
            Leave Management
          </Link>
          <Link 
            to="/approvals" 
            data-testid="nav-approvals-link"
            className="py-2 px-3 hover:bg-slate-800 rounded cursor-pointer transition-colors block text-slate-200 hover:text-white"
          >
            Universal Approvals
          </Link>
          <Link 
            to="/payroll" 
            data-testid="nav-payroll-link"
            className="py-2 px-3 hover:bg-slate-800 rounded cursor-pointer transition-colors block text-slate-200 hover:text-white"
          >
            Payroll & Settlement
          </Link>
          <div className="py-2 px-3 text-slate-500 rounded cursor-not-allowed">
            Administration
          </div>
        </nav>
      </aside>
      <main className="flex-1 flex flex-col min-w-0">
        <header className="h-16 bg-white border-b border-slate-200 flex items-center justify-between px-6">
          <div className="font-semibold text-slate-700">ZainX Workforce • Enterprise</div>
          <div className="flex gap-4 items-center">
            <button 
              data-testid="lang-switch-btn"
              onClick={() => i18n.changeLanguage(i18n.language === 'en' ? 'ar' : 'en')}
              className="text-sm font-medium px-3 py-1 bg-slate-100 hover:bg-slate-200 rounded border border-slate-200"
            >
              {i18n.language === 'en' ? 'العربية' : 'English'}
            </button>
            <div className="w-8 h-8 rounded-full bg-indigo-600 text-white flex items-center justify-center font-bold">
              U
            </div>
          </div>
        </header>
        <div className="flex-1 overflow-auto p-6">
          <Outlet />
        </div>
      </main>
    </div>
  );
}
