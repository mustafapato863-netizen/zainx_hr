import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Icon } from '@zainx/design-system/components/Icon/Icon';

interface AiQuickLauncherProps {
  onLaunch?: (initialPrompt: string) => void;
}

export const AiQuickLauncher: React.FC<AiQuickLauncherProps> = ({ onLaunch }) => {
  const { i18n } = useTranslation();
  const isRtl = i18n.language === 'ar' || document.documentElement.dir === 'rtl';
  const [isOpen, setIsOpen] = useState(false);
  const [query, setQuery] = useState('');

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    if (!query.trim()) return;
    if (onLaunch) {
      onLaunch(query.trim());
    } else {
      window.location.href = `/ai?prompt=${encodeURIComponent(query.trim())}`;
    }
  };

  return (
    <div className="relative inline-block" dir={isRtl ? 'rtl' : 'ltr'}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center gap-2 rounded-lg border border-primary bg-primary-subtle px-3 py-1.5 text-xs font-semibold text-primary transition shadow-xs"
        data-testid="ai-quick-launcher-trigger"
        aria-label="Ask ZainX AI"
      >
        <Icon name="sparkles" size="sm" aria-hidden="true" />
        <span>{isRtl ? 'اسأل المساعد الذكي' : 'Ask AI'}</span>
        <kbd className="hidden rounded border border-primary bg-surface px-1.5 py-0.5 font-mono text-[10px] text-text-muted sm:inline-block">
          ⌘K
        </kbd>
      </button>

      {isOpen && (
        <div className="absolute end-0 top-full z-50 mt-2 w-80 rounded-xl border border-border-default bg-surface p-3 shadow-xl sm:w-96">
          <form onSubmit={handleSearch} className="flex gap-2">
            <input
              type="text"
              value={query}
              onChange={e => setQuery(e.target.value)}
              placeholder={isRtl ? 'استفسر عن مسير الرواتب أو اللوائح...' : 'Ask about payroll, policies, headcount...'}
              className="flex-1 rounded-lg border border-border-default bg-surface-subtle px-3 py-1.5 text-xs focus:outline-hidden focus:ring-2 focus:ring-primary"
              autoFocus
              data-testid="ai-quick-launcher-input"
            />
            <button
              type="submit"
              className="px-3 py-1.5 bg-primary-subtle hover:bg-primary-subtle text-text-inverse font-semibold text-xs rounded-lg transition"
              data-testid="ai-quick-launcher-submit"
            >
              <Icon name="arrow-right" size="sm" aria-hidden="true" />
            </button>
          </form>
        </div>
      )}
    </div>
  );
};

