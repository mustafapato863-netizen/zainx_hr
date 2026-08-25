import React, { useState } from 'react';
import { Badge, Button } from '@zainx/design-system';
import { LeaveRequestDto } from '@zainx/contracts';

export interface LeaveCalendarProps {
  requests?: LeaveRequestDto[];
  isLoading?: boolean;
  onSelectDate?: (date: string) => void;
}

export const LeaveCalendar: React.FC<LeaveCalendarProps> = ({
  requests = [],
  isLoading = false,
  onSelectDate,
}) => {
  const [currentMonth, setCurrentMonth] = useState<Date>(new Date(2026, 7, 1)); // August 2026

  const monthName = currentMonth.toLocaleString('default', { month: 'long', year: 'numeric' });

  const prevMonth = () => {
    setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() - 1, 1));
  };

  const nextMonth = () => {
    setCurrentMonth(new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1, 1));
  };

  const daysInMonth = new Date(
    currentMonth.getFullYear(),
    currentMonth.getMonth() + 1,
    0,
  ).getDate();
  const firstDayIndex = new Date(currentMonth.getFullYear(), currentMonth.getMonth(), 1).getDay();

  const days = Array.from({ length: daysInMonth }, (_, i) => i + 1);
  const blankDays = Array.from({ length: firstDayIndex }, (_, i) => i);

  const getLeavesForDay = (day: number) => {
    const dateStr = `${currentMonth.getFullYear()}-${String(currentMonth.getMonth() + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
    return requests.filter((r) => {
      return r.startDate <= dateStr && r.endDate >= dateStr;
    });
  };

  return (
    <div className="space-y-4" data-testid="leave-calendar">
      {/* Calendar Header */}
      <div className="flex items-center justify-between">
        <h3 className="text-base font-bold text-text-primary">{monthName}</h3>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm" onClick={prevMonth} aria-label="Previous Month">
            ◀
          </Button>
          <Button variant="outline" size="sm" onClick={() => setCurrentMonth(new Date(2026, 7, 1))}>
            Today
          </Button>
          <Button variant="outline" size="sm" onClick={nextMonth} aria-label="Next Month">
            ▶
          </Button>
        </div>
      </div>

      {/* Grid */}
      <div className="grid grid-cols-7 gap-px rounded-xl border border-border-secondary bg-border-secondary overflow-hidden text-center">
        {['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'].map((d) => (
          <div
            key={d}
            className="bg-surface-secondary py-2 text-xs font-semibold text-text-secondary"
          >
            {d}
          </div>
        ))}

        {blankDays.map((b) => (
          <div key={`blank-${b}`} className="bg-surface-primary/40 min-h-[90px] p-1.5" />
        ))}

        {days.map((d) => {
          const dayLeaves = getLeavesForDay(d);
          const isToday = d === 24; // Mock today Aug 24
          const dateStr = `${currentMonth.getFullYear()}-${String(currentMonth.getMonth() + 1).padStart(2, '0')}-${String(d).padStart(2, '0')}`;

          return (
            <div
              key={`day-${d}`}
              onClick={() => onSelectDate?.(dateStr)}
              className={`bg-surface-primary min-h-[90px] p-2 text-left hover:bg-surface-secondary/40 transition-colors cursor-pointer flex flex-col justify-between ${
                isToday ? 'ring-2 ring-brand-primary ring-inset' : ''
              }`}
            >
              <div className="flex justify-between items-start">
                <span
                  className={`text-xs font-semibold ${
                    isToday
                      ? 'w-5 h-5 rounded-full bg-brand-primary text-text-inverse flex items-center justify-center'
                      : 'text-text-primary'
                  }`}
                >
                  {d}
                </span>
                {dayLeaves.length > 0 && <span className="w-2 h-2 rounded-full bg-brand-primary" />}
              </div>

              <div className="space-y-1 overflow-y-auto max-h-14">
                {dayLeaves.slice(0, 2).map((l) => (
                  <div
                    key={l.id}
                    className="text-[10px] truncate px-1.5 py-0.5 rounded bg-brand-primary/10 text-brand-primary font-medium border border-brand-primary/20"
                    title={`${(l as any).employeeNameEn || 'Employee'} (${l.leaveTypeNameEn})`}
                  >
                    {((l as any).employeeNameEn || 'Employee')?.split(' ')[0]}: {l.leaveTypeNameEn}
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
