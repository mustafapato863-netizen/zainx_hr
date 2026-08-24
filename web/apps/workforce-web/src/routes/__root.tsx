import React from 'react';
import { createRootRoute, Outlet, Link } from '@tanstack/react-router';
import { useTranslation } from 'react-i18next';
import { NotificationCenter } from '@zainx/platform';

export const Route = createRootRoute({
  component: RootComponent,
});

function RootComponent() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === 'ar';

  return (
    <div className="flex h-screen w-full bg-slate-50 text-slate-900" dir={i18n.dir()}>
      <aside className="w-64 bg-slate-900 text-white flex-shrink-0 p-4 flex flex-col justify-between">
        <div>
          <div className="flex items-center gap-2 mb-8 px-2">
            <div className="w-7 h-7 rounded-lg bg-indigo-600 flex items-center justify-center font-bold text-white text-sm">
              Z
            </div>
            <h1 className="font-bold text-lg tracking-tight">ZainX Workforce</h1>
          </div>
          <nav className="flex flex-col gap-1.5">
            <Link 
              to="/" 
              className="py-2 px-3 hover:bg-slate-800 rounded-xl cursor-pointer transition-colors block text-slate-300 hover:text-white text-xs font-medium"
            >
              {isAr ? 'لوحة التحكم' : 'Dashboard'}
            </Link>
            <Link 
              to="/people" 
              data-testid="nav-people-link"
              className="py-2 px-3 hover:bg-slate-800 rounded-xl cursor-pointer transition-colors block text-slate-300 hover:text-white text-xs font-medium"
            >
              {isAr ? 'شؤون الموظفين' : 'People'}
            </Link>
            <Link 
              to="/attendance" 
              data-testid="nav-attendance-link"
              className="py-2 px-3 hover:bg-slate-800 rounded-xl cursor-pointer transition-colors block text-slate-300 hover:text-white text-xs font-medium"
            >
              {isAr ? 'الحضور والانصراف' : 'Attendance'}
            </Link>
            <Link 
              to="/leave" 
              data-testid="nav-leave-link"
              className="py-2 px-3 hover:bg-slate-800 rounded-xl cursor-pointer transition-colors block text-slate-300 hover:text-white text-xs font-medium"
            >
              {isAr ? 'إدارة الإجازات' : 'Leave Management'}
            </Link>
            <Link 
              to="/approvals" 
              data-testid="nav-approvals-link"
              className="py-2 px-3 hover:bg-slate-800 rounded-xl cursor-pointer transition-colors block text-slate-300 hover:text-white text-xs font-medium"
            >
              {isAr ? 'الموافقات الشاملة' : 'Universal Approvals'}
            </Link>
            <Link 
              to="/payroll" 
              data-testid="nav-payroll-link"
              className="py-2 px-3 hover:bg-slate-800 rounded-xl cursor-pointer transition-colors block text-slate-300 hover:text-white text-xs font-medium"
            >
              {isAr ? 'الرواتب والتسويات' : 'Payroll & Settlement'}
            </Link>
            <Link 
              to="/recruitment" 
              data-testid="nav-recruitment-link"
              className="py-2 px-3 hover:bg-slate-800 rounded-xl cursor-pointer transition-colors block text-slate-300 hover:text-white text-xs font-medium"
            >
              {isAr ? 'التوظيف والاستقطاب' : 'Recruitment (ATS)'}
            </Link>
            <Link 
              to="/reports" 
              data-testid="nav-reports-link"
              className="py-2 px-3 hover:bg-slate-800 rounded-xl cursor-pointer transition-colors block text-slate-300 hover:text-white text-xs font-medium"
            >
              {isAr ? 'التقارير والرؤى' : 'Reports & Insights'}
            </Link>
            <Link 
              to="/administration" 
              data-testid="nav-administration-link"
              className="py-2 px-3 hover:bg-slate-800 rounded-xl cursor-pointer transition-colors block text-slate-300 hover:text-white text-xs font-medium"
            >
              {isAr ? 'الإدارة والتحكم' : 'Administration & Governance'}
            </Link>
          </nav>
        </div>

        <div className="pt-4 border-t border-slate-800 text-[11px] text-slate-500 px-2 flex justify-between items-center">
          <span>v1.0.0-phase6</span>
          <span className="font-mono text-emerald-500">Live</span>
        </div>
      </aside>
      <main className="flex-1 flex flex-col min-w-0">
        <header className="h-16 bg-white border-b border-slate-200 flex items-center justify-between px-6">
          <div className="font-semibold text-slate-800 text-sm">
            {isAr ? 'منظومة زين إكس المؤسسية • الحوكمة والتحكم' : 'ZainX Workforce • Enterprise Platform'}
          </div>
          <div className="flex gap-4 items-center">
            {/* Live In-App Notification Center */}
            <NotificationCenter />

            <button 
              data-testid="lang-switch-btn"
              onClick={() => i18n.changeLanguage(i18n.language === 'en' ? 'ar' : 'en')}
              className="text-xs font-semibold px-3 py-1.5 bg-slate-100 hover:bg-slate-200 rounded-xl border border-slate-200 transition-colors text-slate-700"
            >
              {i18n.language === 'en' ? 'العربية' : 'English'}
            </button>
            <div className="w-8 h-8 rounded-full bg-indigo-600 text-white flex items-center justify-center font-bold text-xs shadow-sm">
              AD
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
