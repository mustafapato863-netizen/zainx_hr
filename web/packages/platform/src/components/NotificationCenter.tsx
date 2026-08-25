import React, { useState, useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { Icon } from '@zainx/design-system/components/Icon/Icon';

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
  const [isUnavailable, setIsUnavailable] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  const fetchUnreadCount = async () => {
    try {
      const res = await fetch('/api/v1/notifications/unread-count');
      if (res.ok) {
        const data = await res.json();
        setUnreadCount(data.unreadCount ?? 0);
        setIsUnavailable(false);
      }
    } catch {
      setUnreadCount(0);
      setIsUnavailable(true);
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
        setIsUnavailable(false);
      }
    } catch {
      setNotifications([]);
      setUnreadCount(0);
      setIsUnavailable(true);
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
      setNotifications((prev) => prev.map((n) => (n.id === id ? { ...n, isRead: true } : n)));
      setUnreadCount((prev) => Math.max(0, prev - 1));
    } catch {
      setNotifications((prev) => prev.map((n) => (n.id === id ? { ...n, isRead: true } : n)));
      setUnreadCount((prev) => Math.max(0, prev - 1));
    }
  };

  const markAllAsRead = async () => {
    try {
      await fetch('/api/v1/notifications/read-all', { method: 'POST' });
      setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
      setUnreadCount(0);
    } catch {
      setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
      setUnreadCount(0);
    }
  };

  return (
    <div className="relative inline-block text-start" ref={containerRef}>
      <button
        data-testid="notification-bell-btn"
        onClick={() => setIsOpen(!isOpen)}
        className="relative p-2 text-text-secondary hover:text-text-primary hover:bg-surface-subtle rounded-lg transition-colors focus-visible:outline-hidden focus-visible:ring-2 focus-visible:ring-primary"
        aria-label={isAr ? 'الإشعارات' : 'Notifications'}
      >
        <Icon name="bell" size="sm" />
        {unreadCount > 0 && (
          <span
            data-testid="notification-unread-badge"
            className="absolute top-1 end-1 inline-flex items-center justify-center px-1.5 py-0.5 text-[10px] font-bold leading-none text-text-inverse transform translate-x-1/4 -translate-y-1/4 bg-danger rounded-full"
          >
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {isOpen && (
        <div
          data-testid="notification-dropdown"
          className="origin-top-end absolute end-0 mt-2 w-80 sm:w-96 rounded-xl shadow-overlay bg-surface border border-border-default ring-1 ring-black/5 z-50 overflow-hidden"
        >
          {/* Header */}
          <div className="px-4 py-3 bg-surface-subtle border-b border-border-default flex items-center justify-between">
            <div className="flex items-center gap-2">
              <span className="font-semibold text-sm text-text-primary">
                {isAr ? 'مركز الإشعارات' : 'Notification Center'}
              </span>
              {unreadCount > 0 && (
                <span className="bg-primary text-primary-foreground text-xs px-2 py-0.5 rounded-full font-medium">
                  {unreadCount} {isAr ? 'جديد' : 'new'}
                </span>
              )}
            </div>
            {unreadCount > 0 && (
              <button
                data-testid="mark-all-read-btn"
                onClick={markAllAsRead}
                className="text-xs text-primary hover:underline font-medium transition-colors"
              >
                {isAr ? 'تحديد الكل كمقروء' : 'Mark all as read'}
              </button>
            )}
          </div>

          {/* Filter Bar */}
          <div className="flex border-b border-border-subtle bg-surface-subtle/50 text-xs px-3 py-2 justify-between items-center">
            <div className="flex gap-2">
              <button
                onClick={() => setUnreadOnly(false)}
                className={`px-2.5 py-1 rounded-md font-medium transition-colors ${
                  !unreadOnly
                    ? 'bg-surface text-text-primary shadow-xs border border-border-default'
                    : 'text-text-secondary hover:text-text-primary'
                }`}
              >
                {isAr ? 'الكل' : 'All'}
              </button>
              <button
                onClick={() => setUnreadOnly(true)}
                className={`px-2.5 py-1 rounded-md font-medium transition-colors ${
                  unreadOnly
                    ? 'bg-surface text-text-primary shadow-xs border border-border-default'
                    : 'text-text-secondary hover:text-text-primary'
                }`}
              >
                {isAr ? 'غير المقروءة' : 'Unread'}
              </button>
            </div>
            <span className="text-text-muted text-[11px]">
              {isAr ? 'تحديث فوري' : 'Live updates'}
            </span>
          </div>

          {/* Notifications List */}
          <div className="max-h-80 overflow-y-auto divide-y divide-border-subtle">
            {loading ? (
              <div className="py-8 text-center text-text-muted text-sm">
                {isAr ? 'جاري تحميل الإشعارات...' : 'Loading notifications...'}
              </div>
            ) : isUnavailable ? (
              <div className="px-4 py-8 text-center text-sm text-text-secondary">
                {isAr ? 'مركز الإشعارات غير متاح حالياً' : 'Notification service is unavailable'}
                <p className="mt-1 text-xs text-text-muted">
                  {isAr
                    ? 'لن يتم عرض إشعارات افتراضية.'
                    : 'No placeholder notifications are shown.'}
                </p>
              </div>
            ) : notifications.length === 0 ? (
              <div className="py-8 text-center text-text-muted text-sm">
                {isAr ? 'لا توجد إشعارات حالياً' : 'No notifications'}
              </div>
            ) : (
              notifications.map((item) => (
                <div
                  key={item.id}
                  data-testid={`notification-item-${item.id}`}
                  onClick={() => !item.isRead && markAsRead(item.id)}
                  className={`p-3.5 transition-colors cursor-pointer hover:bg-surface-subtle flex items-start gap-3 ${
                    !item.isRead ? 'bg-primary/5' : ''
                  }`}
                >
                  <div
                    className={`w-2 h-2 mt-1.5 rounded-full flex-shrink-0 ${
                      !item.isRead ? 'bg-primary ring-4 ring-primary/20' : 'bg-transparent'
                    }`}
                  />
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between gap-2 mb-0.5">
                      <span className="font-semibold text-xs text-text-primary truncate">
                        {isAr ? item.titleAr : item.titleEn}
                      </span>
                      <span className="text-[10px] text-text-muted font-mono flex-shrink-0">
                        {new Date(item.createdAtUtc).toLocaleTimeString([], {
                          hour: '2-digit',
                          minute: '2-digit',
                        })}
                      </span>
                    </div>
                    <p className="text-xs text-text-secondary line-clamp-2 leading-relaxed">
                      {isAr ? item.bodyAr : item.bodyEn}
                    </p>
                    <div className="mt-1.5 flex items-center gap-2">
                      <span className="text-[10px] uppercase tracking-wider font-semibold px-1.5 py-0.5 rounded bg-surface-subtle text-text-secondary border border-border-subtle">
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
