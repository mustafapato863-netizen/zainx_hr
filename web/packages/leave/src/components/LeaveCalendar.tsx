import React, { useState } from 'react';
import { Badge, Button } from '@zainx/design-system';
import { LeaveRequestDto, LeaveRequestStatus } from '@zainx/contracts';

export interface LeaveCalendarProps {
  requests?: LeaveRequestDto[];
  isLoading?: boolean;
  onSelectDate?: (date: string) => void;
}

export const LeaveCalendar: React.FC<LeaveCalendarProps> = ({
  requests = [],
  isLoading = false,
  onSelectDate
}) => {
  const [currentMonth, setCurrentMonth] = useState<Date>(new Date(2026, 7, 1)); // August 2026

  const monthName = currentMonth.toLocaleString('default', { month: 'long', year: 'numeric' });

  const prevMonth = () => {
    setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() - 1, 1));
  };

  const nextMonth = () => {
    setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1, 1));
  };

  const year = currentMonth.getFullYear();
  const month = currentMonth.getMonth();
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  const firstDayOfWeek = new Date(year, month, 1).getDay(); // 0 = Sun

  const daysArray = Array.from({ length: daysInMonth }, (_, i) => i + 1);
  const paddingDays = Array.from({ length: firstDayOfWeek }, (_, i) => i);

  return (
    <div className="p-6 bg-surface-primary rounded-xl border border-border-primary shadow-sm space-y-4" data-testid="leave-calendar">
      <div className="flex items-center justify-between border-b border-border-primary pb-4">
        <div>
          <h2 className="text-lg font-bold text-text-primary">Leave Schedule Calendar</h2>
          <p className="text-xs text-text-muted mt-0.5">
            Team leave visualization and overlap transparency
          </p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={prevMonth} ariaLabel="Previous month">
            ←
          </Button>
          <span className="text-sm font-bold text-text-primary min-w-32 text-center">
            {monthName}
          </span>
          <Button variant="outline" size="sm" onClick={nextMonth} ariaLabel="Next month">
            →
          </Button>
        </div>
      </div>

      {/* Weekday headers */}
      <div className="grid grid-cols-7 gap-2 text-center text-xs font-semibold text-text-secondary uppercase">
        <span>Sun</span>
        <span>Mon</span>
        <span>Tue</span>
        <span>Wed</span>
        <span>Thu</span>
        <span>Fri</span>
        <span>Sat</span>
      </div>

      {/* Calendar Grid */}
      <div className="grid grid-cols-7 gap-2">
        {paddingDays.map((d) => (
          <div key={`pad-${d}`} className="h-24 rounded-lg bg-surface-secondary/20 border border-transparent" />
        ))}

        {daysArray.map((dayNum) => {
          const dateStr = `${year}-${String(month + 1).padStart(2, '0')}-${String(dayNum).padStart(2, '0')}`;
          const dayLeaves = requests.filter(
            (r) =>
              r.status !== LeaveRequestStatus.Rejected &&
              r.status !== LeaveRequestStatus.Cancelled &&
              r.startDate <= dateStr &&
              r.endDate >= dateStr
          );

          return (
            <div
              key={dateStr}
              onClick={() => onSelectDate?.(dateStr)}
              className="h-24 p-2 rounded-lg border border-border-secondary bg-surface-secondary/40 hover:border-brand-primary/60 transition-all flex flex-col justify-between cursor-pointer"
              data-testid={`calendar-cell-${dateStr}`}
            >
              <div className="flex items-center justify-between">
                <span className="text-xs font-bold text-text-primary font-mono">{dayNum}</span>
                {dayLeaves.length > 0 && (
                  <span className="w-2 h-2 rounded-full bg-brand-primary" />
                )}
              </div>

              <div className="space-y-1 overflow-y-auto max-h-14">
                {dayLeaves.slice(0, 2).map((l) => (
                  <div
                    key={l.id}
                    className="text-[10px] truncate px-1.5 py-0.5 rounded bg-brand-primary/10 text-brand-primary font-medium border border-brand-primary/20"
                    title={`${l.employeeNameEn} (${l.leaveTypeNameEn})`}
                  >
                    {l.employeeNameEn?.split(' ')[0]}: {l.leaveTypeNameEn}
                  </div>
                ))}
                {dayLeaves.length > 2 && (
                  <span className="text-[9px] text-text-muted font-semibold block text-center">
                    +{dayLeaves.length - 2} more
                  </span>
                )}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
