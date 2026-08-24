import React, { useState, useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';

export interface NotificationItem {
  id: string;
  category: string;
  titleEn: string;
  titleAr: string;
  bodyEn: string;
  bodyAr: string;
  deepLinkUrl?: string;
  isRead: boolean;
  createdAtUtc: string;
}

export function NotificationCenter() {
  const { i18n } = useTranslation();
  const isAr = i18n.language === 'ar';
  const [isOpen, setIsOpen] = useState(false);
  const [unreadOnly, setUnreadOnly] = useState(false);
  const [unreadCount, setUnreadCount] = useState<number>(0);
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [loading, setLoading] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  const fetchUnreadCount = async () => {
    try {
      const res = await fetch('/api/v1/notifications/unread-count');
      if (res.ok) {
        const data = await res.json();
        setUnreadCount(data.unreadCount ?? 0);
      }
    } catch {
      // Fallback
    }
  };

  const fetchNotifications = async () => {
    setLoading(true);
    try {
      const res = await fetch(`/api/v1/notifications?unreadOnly=${unreadOnly}&pageSize=20`);
      if (res.ok) {
        const data = await res.json();
        setNotifications(data.items || []);
        setUnreadCount(data.unreadCount ?? 0);
      }
    } catch {
      // Fallback sample data if local offline
      setNotifications([
        {
          id: 'n-1',
          category: 'Leave',
          titleEn: 'Leave Request Approved',
          titleAr: 'تمت الموافقة على طلب الإجازة',
          bodyEn: 'Your annual leave request for Sep 1 - Sep 5 was approved by HR.',
          bodyAr: 'تمت الموافقة على طلب إجازتك السنوية من 1 سبتمبر إلى 5 سبتمبر.',
          isRead: false,
          createdAtUtc: new Date().toISOString()
        },
        {
          id: 'n-2',
          category: 'Payroll',
          titleEn: 'Payroll Finalized',
          titleAr: 'تم اعتماد مسيرات الرواتب',
          bodyEn: 'August payroll run has been finalized and settlement file generated.',
          bodyAr: 'تم اعتماد مسير رواتب شهر أغسطس وإنشاء ملف التسوية البنكية.',
          isRead: true,
          createdAtUtc: new Date(Date.now() - 3600000).toISOString()
        }
      ]);
      setUnreadCount(1);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchUnreadCount();
    const interval = setInterval(fetchUnreadCount, 30000);
    return () => clearInterval(interval);
  }, []);

  useEffect(() => {
    if (isOpen) {
      fetchNotifications();
    }
  }, [isOpen, unreadOnly]);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    }
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const markAsRead = async (id: string) => {
    try {
      await fetch(`/api/v1/notifications/${id}/read`, { method: 'POST' });
      setNotifications(prev =>
        prev.map(n => (n.id === id ? { ...n, isRead: true } : n))
      );
      setUnreadCount(prev => Math.max(0, prev - 1));
    } catch {
      setNotifications(prev =>
        prev.map(n => (n.id === id ? { ...n, isRead: true } : n))
      );
      setUnreadCount(prev => Math.max(0, prev - 1));
    }
  };

  const markAllAsRead = async () => {
    try {
      await fetch('/api/v1/notifications/read-all', { method: 'POST' });
      setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
      setUnreadCount(0);
    } catch {
      setNotifications(prev => prev.map(n => ({ ...n, isRead: true })));
      setUnreadCount(0);
    }
  };

  return (
    <div className="relative inline-block text-left" ref={containerRef}>
      <button
        data-testid="notification-bell-btn"
        onClick={() => setIsOpen(!isOpen)}
        className="relative p-2 text-slate-600 hover:text-slate-900 hover:bg-slate-100 rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-indigo-500"
        aria-label={isAr ? 'الإشعارات' : 'Notifications'}
      >
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
        </svg>
        {unreadCount > 0 && (
          <span
            data-testid="notification-unread-badge"
            className="absolute top-1 right-1 inline-flex items-center justify-center px-1.5 py-0.5 text-xs font-bold leading-none text-white transform translate-x-1/4 -translate-y-1/4 bg-red-600 rounded-full"
          >
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <div
          data-testid="notification-dropdown"
          className="origin-top-right absolute right-0 mt-2 w-80 sm:w-96 rounded-xl shadow-2xl bg-white border border-slate-200 ring-1 ring-black ring-opacity-5 z-50 overflow-hidden"
        >
          {/* Header */}
          <div className="px-4 py-3 bg-slate-900 text-white flex items-center justify-between">
            <div className="flex items-center gap-2">
              <span className="font-semibold text-sm">
                {isAr ? 'مركز الإشعارات' : 'Notification Center'}
              </span>
              {unreadCount > 0 && (
                <span className="bg-indigo-600 text-white text-xs px-2 py-0.5 rounded-full font-medium">
                  {unreadCount} {isAr ? 'جديد' : 'new'}
                </span>
              )}
            </div>
            {unreadCount > 0 && (
              <button
                data-testid="mark-all-read-btn"
                onClick={markAllAsRead}
                className="text-xs text-indigo-200 hover:text-white underline font-medium transition-colors"
              >
                {isAr ? 'تحديد الكل كمقروء' : 'Mark all as read'}
              </button>
            )}
          </div>

          {/* Filter Bar */}
          <div className="flex border-b border-slate-100 bg-slate-50 text-xs px-3 py-2 justify-between items-center">
            <div className="flex gap-2">
              <button
                onClick={() => setUnreadOnly(false)}
                className={`px-2.5 py-1 rounded-md font-medium transition-colors ${!unreadOnly ? 'bg-white text-slate-900 shadow-sm border border-slate-200' : 'text-slate-600 hover:text-slate-900'}`}
              >
                {isAr ? 'الكل' : 'All'}
              </button>
              <button
                onClick={() => setUnreadOnly(true)}
                className={`px-2.5 py-1 rounded-md font-medium transition-colors ${unreadOnly ? 'bg-white text-slate-900 shadow-sm border border-slate-200' : 'text-slate-600 hover:text-slate-900'}`}
              >
                {isAr ? 'غير المقروءة' : 'Unread'}
              </button>
            </div>
            <span className="text-slate-400 text-[11px]">
              {isAr ? 'تحديث فوري' : 'Live updates'}
            </span>
          </div>

          {/* Notifications List */}
          <div className="max-h-80 overflow-y-auto divide-y divide-slate-100">
            {loading ? (
              <div className="py-8 text-center text-slate-400 text-sm">
                {isAr ? 'جاري تحميل الإشعارات...' : 'Loading notifications...'}
              </div>
            ) : notifications.length === 0 ? (
              <div className="py-8 text-center text-slate-400 text-sm">
                {isAr ? 'لا توجد إشعارات حالياً' : 'No notifications'}
              </div>
            ) : (
              notifications.map(item => (
                <div
                  key={item.id}
                  data-testid={`notification-item-${item.id}`}
                  onClick={() => !item.isRead && markAsRead(item.id)}
                  className={`p-3.5 transition-colors cursor-pointer hover:bg-slate-50 flex items-start gap-3 ${!item.isRead ? 'bg-indigo-50/40' : ''}`}
                >
                  <div className={`w-2 h-2 mt-1.5 rounded-full flex-shrink-0 ${!item.isRead ? 'bg-indigo-600 ring-4 ring-indigo-100' : 'bg-transparent'}`} />
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between gap-2 mb-0.5">
                      <span className="font-semibold text-xs text-slate-900 truncate">
                        {isAr ? item.titleAr : item.titleEn}
                      </span>
                      <span className="text-[10px] text-slate-400 font-mono flex-shrink-0">
                        {new Date(item.createdAtUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                      </span>
                    </div>
                    <p className="text-xs text-slate-600 line-clamp-2 leading-relaxed">
                      {isAr ? item.bodyAr : item.bodyEn}
                    </p>
                    <div className="mt-1.5 flex items-center gap-2">
                      <span className="text-[10px] uppercase tracking-wider font-semibold px-1.5 py-0.5 rounded bg-slate-200/70 text-slate-700">
                        {item.category}
                      </span>
                    </div>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
}
